using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using Helldivers2ModManager.ViewModels.Create;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class CreatePageViewModel : PageViewModelBase
{
	public override string Title => "Create";

	public WizardViewModelBase CurrentPage
	{
		get => _currentPage;

		set
		{
			OnPropertyChanging();
			if (_currentPage is not null)
				_currentPage.IsValidChanged -= CurrentPage_IsValidChanged;
			_currentPage = value;
			_currentPage.IsValidChanged += CurrentPage_IsValidChanged;
			OnPropertyChanged();
		}
	}

	private readonly ILogger<CreatePageViewModel> _logger;
	private readonly NavigationStore _navigationStore;
	private readonly ChoosePageViewModel _choosePage;
	private readonly List<WizardViewModelBase> _pages;
	private WizardViewModelBase _currentPage;

	public CreatePageViewModel(ILogger<CreatePageViewModel> logger, NavigationStore navigationStore)
	{
		_logger = logger;
		_navigationStore = navigationStore;
		_choosePage = new ChoosePageViewModel();
		_pages = [
			new IntroPageViewModel(),
			_choosePage,
			null
		];
		_currentPage = _pages[0];
		_currentPage.IsValidChanged += CurrentPage_IsValidChanged;
	}

	[RelayCommand]
	void Cancel()
	{
		_navigationStore.Navigate<DashboardPageViewModel>();
	}

	bool CanBack()
	{
		return CurrentPage != _pages.First();
	}

	[RelayCommand(CanExecute = nameof(CanBack))]
	void Back()
	{
		CurrentPage = _pages[_pages.IndexOf(CurrentPage) - 1];
		BackCommand.NotifyCanExecuteChanged();
		NextCommand.NotifyCanExecuteChanged();
	}

	bool CanNext()
	{
		return CurrentPage.IsValid() && CurrentPage != _pages.Last();
	}

	[RelayCommand(CanExecute = nameof(CanNext))]
	void Next()
	{
		CurrentPage = _pages[_pages.IndexOf(CurrentPage) + 1];
		BackCommand.NotifyCanExecuteChanged();
		NextCommand.NotifyCanExecuteChanged();
	}

	private void CurrentPage_IsValidChanged(object? sender, EventArgs e)
	{
		NextCommand.NotifyCanExecuteChanged();
	}
}
