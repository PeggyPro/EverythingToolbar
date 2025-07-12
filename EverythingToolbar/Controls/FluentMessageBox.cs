using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using TextBlock = System.Windows.Controls.TextBlock;

namespace EverythingToolbar.Controls
{
    public static class FluentMessageBox
    {
        private static MessageBox CreateBase(string title)
        {
            var messageBox = new MessageBox()
            {
                Title = title,
                IsCloseButtonEnabled = true,
                MinWidth = 300
            };

            // We need to apply resources before setting the content on the base message box
            ApplicationThemeManager.Apply(SystemThemeManager.GetCachedSystemTheme() == SystemTheme.Light
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark);
            ApplicationThemeManager.Apply(messageBox);

            return messageBox;
        }

        private static TextBlock CreateTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public static MessageBox CreateRegular(string content, string title)
        {
            var messageBox = CreateBase(title);

            messageBox.Content = CreateTextBlock(content);

            return messageBox;
        }

        public static MessageBox CreateYesNo(string content, string title)
        {
            var messageBox = CreateRegular(content, title);

            messageBox.PrimaryButtonText = "Yes";
            messageBox.SecondaryButtonText = "No";
            messageBox.IsCloseButtonEnabled = false;

            return messageBox;
        }

        public static MessageBox CreateError(string content, string title)
        {
            var messageBox = CreateBase(title);

            messageBox.IsPrimaryButtonEnabled = false;
            messageBox.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 0),
                Children =
                {
                    new SymbolIcon
                    {
                        Symbol = SymbolRegular.Warning28,
                        FontSize = 32,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 12, 0)
                    },
                    CreateTextBlock(content)
                }
            };
            return messageBox;
        }
    }
}