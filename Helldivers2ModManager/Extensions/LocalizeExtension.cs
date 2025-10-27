using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Helldivers2ModManager.Services;

namespace Helldivers2ModManager.Extensions;

[MarkupExtensionReturnType(typeof(BindingExpression))]
internal sealed class LocalizeExtension : MarkupExtension
{
	public string Key { get; set; }

	public LocalizeExtension()
	{
		Key = string.Empty;
	}

	public LocalizeExtension(string key)
	{
		Key = key;
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		if (string.IsNullOrEmpty(Key))
			return "[NoKey]";

		// Try to get the localization service from the app
		if (Application.Current?.TryFindResource("LocalizationService") is LocalizationService locService)
		{
			var binding = new Binding($"[{Key}]")
			{
				Source = locService,
				Mode = BindingMode.OneWay
			};

			if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
			{
				if (target.TargetObject is DependencyObject depObj && target.TargetProperty is DependencyProperty depProp)
				{
					return binding.ProvideValue(serviceProvider);
				}
			}

			return binding;
		}

		return $"[{Key}]";
	}
}
