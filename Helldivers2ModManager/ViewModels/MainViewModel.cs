using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
	public string Title => "HD2 Mod Manager - " + CurrentViewModel.Title;

	public PageViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

	private readonly NavigationStore _navigationStore;

	public MainViewModel(NavigationStore navigationStore)
	{
		_navigationStore = navigationStore;
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

	}
}
