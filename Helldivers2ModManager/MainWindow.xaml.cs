using Helldivers2ModManager.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Helldivers2ModManager;

internal partial class MainWindow : Window
{
	public MainWindow(MainViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}

	protected override void OnActivated(EventArgs e)
	{
		DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, 1, sizeof(int));
		base.OnActivated(e);
	}

	private void MinButton_Click(object sender, RoutedEventArgs e)
	{
		WindowState = WindowState.Minimized;
	}

	private void MaxButton_Click(object sender, RoutedEventArgs e)
	{
		if (WindowState == WindowState.Maximized)
			WindowState = WindowState.Normal;
		else
			WindowState = WindowState.Maximized;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[LibraryImport("dwmapi.dll")]
	private static partial void DwmSetWindowAttribute(nint hwnd, uint dwAttribute, in int pvAttribute, uint cbAttribute);
}