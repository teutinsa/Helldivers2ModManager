using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Services;
using Helldivers2ModManager.Stores;
using System.Collections.ObjectModel;
using System.Windows;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class BrowsePageViewModel : PageViewModelBase
{
	public override string Title => "Browse";

	public ObservableCollection<NexusModViewModel> Mods { get; }

	private readonly NavigationStore _navStore;
	private readonly SSOStore _ssoStore;
	private readonly NexusService _nexusService;
	[ObservableProperty]
	private Visibility _loginVisibility = Visibility.Hidden;
	[ObservableProperty]
	private Visibility _progressVisibility = Visibility.Hidden;
	[ObservableProperty]
	private Visibility _modsVisibility = Visibility.Hidden;

	public BrowsePageViewModel(NavigationStore navigationStore, SSOStore ssoStore, NexusService nexusService)
	{
		_navStore = navigationStore;
		_ssoStore = ssoStore;
		_nexusService = nexusService;

		Mods = [];

		_nexusService.UseApiKey("kl6RcAbeYGrCBqaQnm1clB8YoL3Bs/uu+3biKCZVc793rQ==--V2dPz0FSmF1wFfmw--Ig5t7ERtaBum7CglVuIA5A==");
		_ = LoadRecentlyUpdatedModsAsync();
	}

	private async Task LoadRecentlyUpdatedModsAsync()
	{
		LoginVisibility = Visibility.Hidden;
		ProgressVisibility = Visibility.Visible;
		ModsVisibility = Visibility.Hidden;

		var mods = await _nexusService.GetUpdatedAsync();
		Mods.Clear();

		if (mods is not null && mods.All(static m => m is not null))
		{
			foreach (var mod in mods.Reverse().Where(static m => m.Available))
				if (mod is not null)
					Mods.Add(new NexusModViewModel(mod));
		}
		else
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = "Error retrieving mods!"
			});
		}

		LoginVisibility = Visibility.Hidden;
		ProgressVisibility = Visibility.Hidden;
		ModsVisibility = Visibility.Visible;
	}

	[RelayCommand]
	void Back()
	{
		_navStore.Navigate<DashboardPageViewModel>();
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Login()
	{
		var key = await _ssoStore.GetApiKeyAsync();
		if (key is null)
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = "Authentication error!"
			});
			return;
		}
	}
}
