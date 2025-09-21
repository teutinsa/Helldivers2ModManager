using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Media;

namespace Helldivers2ModManager.ViewModels;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class MainViewModel : ObservableObject
{
	public string Title => $"HD2 Mod Manager {Version} - {CurrentViewModel.Title}";

	public PageViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

	public Brush Background => _background;

	public string Version => string.IsNullOrEmpty(App.VersionAddition) ? $"v{App.Version}" : $"v{App.Version} {App.VersionAddition}";

	private static readonly ProcessStartInfo s_helpStartInfo = new(@"https://teutinsa.github.io/hd2mm-site/index.html") { UseShellExecute = true };
	private readonly NavigationStore _navigationStore;
	private readonly SolidColorBrush _background;

	public MainViewModel(NavigationStore navigationStore)
	{
		_navigationStore = navigationStore;
		_background = new SolidColorBrush(Color.FromScRgb(0.7f, 0, 0, 0));

		_navigationStore.Navigated += NavigationStore_Navigated;
	}

	private void NavigationStore_Navigated(object? sender, EventArgs e)
	{
		OnPropertyChanged(nameof(CurrentViewModel));
		OnPropertyChanged(nameof(Title));
	}

	[RelayCommand]
	void Help()
	{
		Process.Start(s_helpStartInfo);
	}
}
