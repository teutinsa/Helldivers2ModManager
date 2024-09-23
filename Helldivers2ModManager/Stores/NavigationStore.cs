using Helldivers2ModManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Helldivers2ModManager.Stores;

internal sealed class NavigationStore(IServiceProvider provider, PageViewModelBase initialViewModel)
{
	public PageViewModelBase CurrentViewModel => _currentViewModel;

	public event EventHandler? Navigated;

	private readonly IServiceProvider _provider = provider;
	private PageViewModelBase _currentViewModel = initialViewModel;

	public void Navigate(PageViewModelBase viewModel)
	{
		_currentViewModel = viewModel;
		Navigated?.Invoke(this, EventArgs.Empty);
	}

	public void Navigate<T>() where T : PageViewModelBase
	{
		Navigate(_provider.GetRequiredService<T>());
	}
}
