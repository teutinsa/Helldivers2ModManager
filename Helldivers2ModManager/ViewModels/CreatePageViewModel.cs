using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class CreatePageViewModel(ILogger<CreatePageViewModel> logger, NavigationStore navigationStore) : PageViewModelBase
{
	public override string Title => "Create";
	
	private readonly ILogger<CreatePageViewModel> _logger = logger;
	private readonly NavigationStore _navigationStore = navigationStore;
	[ObservableProperty]
	private string _modName = string.Empty;
	[ObservableProperty]
	private string _modDescription = string.Empty;
	[ObservableProperty]
	private string _outputFile = string.Empty;

	[RelayCommand]
	void Cancel()
	{
		_navigationStore.Navigate<DashboardPageViewModel>();
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Create()
	{
		await Task.CompletedTask;
	}

	[RelayCommand]
	void BrowseOutput()
	{

	}
}
