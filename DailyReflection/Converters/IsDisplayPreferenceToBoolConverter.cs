using DailyReflection.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that returns Visibility.Visible if the value matches a specific SoberTimeDisplayPreference.
/// Migrated from MAUI IValueConverter to WinUI IValueConverter.
/// In WinUI, Visibility binding requires Visibility type, not bool.
/// </summary>
public class IsDisplayPreferenceToBoolConverter : IValueConverter
{
    /// <summary>
    /// The display preference to match against. Set this in code-behind.
    /// </summary>
    public SoberTimeDisplayPreference DisplayPreference { get; set; }

    /// <summary>
    /// String property for XAML initialization. Gets parsed to DisplayPreference enum.
    /// </summary>
    public string DisplayPreferenceString
    {
        get => DisplayPreference.ToString();
        set
        {
            if (Enum.TryParse<SoberTimeDisplayPreference>(value, out var preference))
            {
                DisplayPreference = preference;
            }
        }
    }

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is SoberTimeDisplayPreference @enum)
        {
            return @enum == DisplayPreference ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
