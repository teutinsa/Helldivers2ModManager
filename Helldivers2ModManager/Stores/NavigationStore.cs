using Helldivers2ModManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Stores;

internal sealed class NavigationStore(IServiceProvider provider, PageViewModelBase initialViewModel)
{
	public PageViewModelBase CurrentViewModel => _currentViewModel;

	public event EventHandler? Navigated;

	private readonly IServiceProvider _provider = provider;
	private readonly ILogger<NavigationStore> _logger = provider.GetRequiredService<ILogger<NavigationStore>>();
	private PageViewModelBase _currentViewModel = initialViewModel;

	public void Navigate(PageViewModelBase viewModel)
	{
		_logger.LogInformation("Navigating to \"{}\"", viewModel.Title);
		_currentViewModel = viewModel;
		Navigated?.Invoke(this, EventArgs.Empty);
	}

	public void Navigate<T>() where T : PageViewModelBase
	{
		_logger.LogInformation("Resolving navigation for `{}`", typeof(T).Name);
		Navigate(_provider.GetRequiredService<T>());
	}
}
