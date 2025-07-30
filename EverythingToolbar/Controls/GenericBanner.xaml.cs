using System;
using System.Windows;
using System.Windows.Media;

namespace EverythingToolbar.Controls
{
    public partial class GenericBanner
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(GenericBanner),
            new PropertyMetadata(string.Empty)
        );

        public static readonly DependencyProperty HeadlineProperty = DependencyProperty.Register(
            nameof(Headline),
            typeof(string),
            typeof(GenericBanner),
            new PropertyMetadata(string.Empty)
        );

        public static readonly DependencyProperty PrimaryButtonTextProperty = DependencyProperty.Register(
            nameof(PrimaryButtonText),
            typeof(string),
            typeof(GenericBanner),
            new PropertyMetadata(string.Empty)
        );

        public static readonly DependencyProperty SecondaryButtonTextProperty = DependencyProperty.Register(
            nameof(SecondaryButtonText),
            typeof(string),
            typeof(GenericBanner),
            new PropertyMetadata(string.Empty)
        );

        public static readonly DependencyProperty BannerColorProperty = DependencyProperty.Register(
            nameof(BannerColor),
            typeof(Brush),
            typeof(GenericBanner),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 158, 51)))
        );

        public static readonly DependencyProperty ButtonBorderBrushProperty = DependencyProperty.Register(
            nameof(ButtonBorderBrush),
            typeof(Brush),
            typeof(GenericBanner),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)))
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Headline
        {
            get => (string)GetValue(HeadlineProperty);
            set => SetValue(HeadlineProperty, value);
        }

        public string PrimaryButtonText
        {
            get => (string)GetValue(PrimaryButtonTextProperty);
            set => SetValue(PrimaryButtonTextProperty, value);
        }

        public string SecondaryButtonText
        {
            get => (string)GetValue(SecondaryButtonTextProperty);
            set => SetValue(SecondaryButtonTextProperty, value);
        }

        public Brush BannerColor
        {
            get => (Brush)GetValue(BannerColorProperty);
            set => SetValue(BannerColorProperty, value);
        }

        public Brush ButtonBorderBrush
        {
            get => (Brush)GetValue(ButtonBorderBrushProperty);
            set => SetValue(ButtonBorderBrushProperty, value);
        }

        public event EventHandler? PrimaryButtonClicked;
        public event EventHandler? SecondaryButtonClicked;

        public GenericBanner()
        {
            InitializeComponent();
        }

        private void OnPrimaryButtonClicked(object sender, RoutedEventArgs e)
        {
            PrimaryButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnSecondaryButtonClicked(object sender, RoutedEventArgs e)
        {
            SecondaryButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
