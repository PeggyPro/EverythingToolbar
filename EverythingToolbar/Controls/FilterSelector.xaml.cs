using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace EverythingToolbar.Controls
{
    public partial class FilterSelector
    {
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register(
                nameof(SelectedFilter),
                typeof(Filter),
                typeof(FilterSelector),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFilterChanged));

        private static void OnSelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FilterSelector)d;
            control.UpdateSelectedItems();
        }

        public Filter? SelectedFilter
        {
            get => (Filter)GetValue(SelectedFilterProperty);
            set => SetValue(SelectedFilterProperty, value);
        }

        public FilterSelector()
        {
            InitializeComponent();

            Loaded += (_, _) => UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            if (SelectedFilter == null) return;

            TabControl.SelectionChanged -= OnTabItemSelected;
            ComboBox.SelectionChanged -= OnComboBoxItemSelected;

            int filterIndex = FilterLoader.Instance.Filters.IndexOf(SelectedFilter);
            int maxTabItems = ToolbarSettings.User.MaxTabItems;

            TabControl.SelectedIndex = filterIndex < maxTabItems ? filterIndex : -1;
            ComboBox.SelectedIndex = filterIndex >= maxTabItems ? filterIndex - maxTabItems : -1;

            TabControl.SelectionChanged += OnTabItemSelected;
            ComboBox.SelectionChanged += OnComboBoxItemSelected;
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0) return;

            if (!TabControl.IsFocused && !TabControl.IsMouseOver)
            {
                TabControl.SelectedIndex = -1;
                return;
            }

            if (TabControl.SelectedItem is Filter selectedFilter)
                SelectedFilter = selectedFilter;
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0) return;

            if (!ComboBox.IsFocused && !ComboBox.IsMouseOver)
            {
                ComboBox.SelectedIndex = -1;
                return;
            }

            if (ComboBox.SelectedItem is Filter selectedFilter)
                SelectedFilter = selectedFilter;
        }
    }
}