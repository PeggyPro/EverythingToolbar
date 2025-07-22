using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class WindowsVersionToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Version currentVersion)
                return Visibility.Visible;

            if (parameter == null)
                return Visibility.Visible;

            var parameterString = parameter.ToString();
            if (string.IsNullOrEmpty(parameterString))
                return Visibility.Visible;

            var comparison = ParseVersionParameter(parameterString);
            if (comparison == null)
                return Visibility.Visible;

            var result = CompareVersions(currentVersion, comparison.Value.targetVersion, comparison.Value.operation);
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private (Version targetVersion, ComparisonOperation operation)? ParseVersionParameter(string parameter)
        {
            ComparisonOperation operation = ComparisonOperation.Equal;
            string versionString = parameter;

            if (parameter.StartsWith(">="))
            {
                operation = ComparisonOperation.GreaterThanOrEqual;
                versionString = parameter[2..];
            }
            else if (parameter.StartsWith("<="))
            {
                operation = ComparisonOperation.LessThanOrEqual;
                versionString = parameter[2..];
            }
            else if (parameter.StartsWith(">"))
            {
                operation = ComparisonOperation.GreaterThan;
                versionString = parameter[1..];
            }
            else if (parameter.StartsWith("<"))
            {
                operation = ComparisonOperation.LessThan;
                versionString = parameter[1..];
            }

            if (int.TryParse(versionString.Trim(), out int buildNumber))
            {
                var targetVersion = new Version(10, 0, buildNumber);
                return (targetVersion, operation);
            }

            return null;
        }

        private bool CompareVersions(Version currentVersion, Version targetVersion, ComparisonOperation operation)
        {
            var compareResult = currentVersion.CompareTo(targetVersion);

            return operation switch
            {
                ComparisonOperation.Equal => compareResult == 0,
                ComparisonOperation.GreaterThan => compareResult > 0,
                ComparisonOperation.LessThan => compareResult < 0,
                ComparisonOperation.GreaterThanOrEqual => compareResult >= 0,
                ComparisonOperation.LessThanOrEqual => compareResult <= 0,
                _ => true,
            };
        }

        private enum ComparisonOperation
        {
            Equal,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
