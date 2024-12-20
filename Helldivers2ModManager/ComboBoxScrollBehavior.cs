using System.Windows;
using System.Windows.Controls;

namespace Helldivers2ModManager;

public static class ComboBoxScrollBehavior
{
	public static readonly DependencyProperty DisableScrollProperty = DependencyProperty.RegisterAttached("DisableScroll", typeof(bool), typeof(ComboBoxScrollBehavior), new PropertyMetadata(false, OnDisableScrollChanged));

	public static bool GetDisableScroll(DependencyObject obj)
	{
		return (bool)obj.GetValue(DisableScrollProperty);
	}

	public static void SetDisableScroll(DependencyObject obj, bool value)
	{
		obj.SetValue(DisableScrollProperty, value);
	}

	private static void OnDisableScrollChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is ComboBox comboBox)
		{
			if ((bool)e.NewValue)
				comboBox.PreviewMouseWheel += ComboBox_PreviewMouseWheel;
			else
				comboBox.PreviewMouseWheel -= ComboBox_PreviewMouseWheel;
		}
	}

	private static void ComboBox_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
	{
		e.Handled = true;
	}
}
