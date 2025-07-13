using EverythingToolbar.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar.Helpers
{
    internal class FilterLoader : INotifyPropertyChanged
    {
        public ObservableCollection<Filter> Filters
        {
            get
            {
                if (ToolbarSettings.User.IsRegExEnabled)
                    return new ObservableCollection<Filter>([DefaultFilterLoader.AllFilter]);

                if (ToolbarSettings.User.IsImportFilters && EverythingFilterLoader.Instance.Filters != null)
                    return EverythingFilterLoader.Instance.Filters;

                return DefaultFilterLoader.Instance.Filters;
            }
        }

        public static readonly FilterLoader Instance = new ();

        private FilterLoader()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
            EverythingFilterLoader.Instance.PropertyChanged += OnEverythingFiltersChanged;
            DefaultFilterLoader.Instance.PropertyChanged += OnDefaultFiltersChanged;
        }

        private void OnDefaultFiltersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DefaultFilterLoader.Instance.Filters))
            {
                NotifyPropertyChanged(nameof(Filters));
            }
        }

        private void OnEverythingFiltersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EverythingFilterLoader.Instance.Filters))
            {
                NotifyPropertyChanged(nameof(Filters));
            }
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ToolbarSettings.User.IsRegExEnabled):
                case nameof(ToolbarSettings.User.IsImportFilters):
                    NotifyPropertyChanged(nameof(Filters));
                    break;
            }
        }

        public Filter GetInitialFilter()
        {
            if (ToolbarSettings.User.IsRememberFilter)
            {
                foreach (var filter in Filters)
                {
                    if (filter.Name == ToolbarSettings.User.LastFilter)
                        return filter;
                }
            }

            return Filters[0];
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}