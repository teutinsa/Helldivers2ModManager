using System.Globalization;
using System.Windows.Data;
using Helldivers2ModManager.Services;

namespace Helldivers2ModManager.Converters;

internal sealed class LocalizationConverter : IValueConverter
{
	public LocalizationService? LocalizationService { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (parameter is string key && LocalizationService != null)
		{
			return LocalizationService[key];
		}

		return $"[{parameter}]";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
