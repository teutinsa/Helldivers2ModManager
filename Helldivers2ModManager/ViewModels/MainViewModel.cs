using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using System.Windows.Media;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
	public string Title => "HD2 Mod Manager - " + CurrentViewModel.Title;

	public PageViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

	public Brush Background => _background;

	public string Version => App.Version.ToString();

	private readonly NavigationStore _navigationStore;
	private readonly SettingsStore _settingsStore;
	private readonly SolidColorBrush _background;

	public MainViewModel(NavigationStore navigationStore, SettingsStore settingsStore)
	{
		_navigationStore = navigationStore;
		_settingsStore = settingsStore;
		_background = new SolidColorBrush(Color.FromScRgb(_settingsStore.Opacity, 0, 0, 0));

		_navigationStore.Navigated += NavigationStore_Navigated;
		_settingsStore.SettingsChanged += SettingsStore_SettingsChanged;
	}

	private void SettingsStore_SettingsChanged(object? sender, EventArgs e)
	{
		_background.Color = Color.FromScRgb(_settingsStore.Opacity, 0, 0, 0);
		OnPropertyChanged(nameof(Background));
	}

	private void NavigationStore_Navigated(object? sender, EventArgs e)
	{
		OnPropertyChanged(nameof(CurrentViewModel));
		OnPropertyChanged(nameof(Title));
	}

	[RelayCommand]
	void Help()
	{
		_navigationStore.Navigate<HelpPageViewModel>();
	}
}
