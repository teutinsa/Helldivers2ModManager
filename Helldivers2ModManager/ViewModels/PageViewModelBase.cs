using CommunityToolkit.Mvvm.ComponentModel;

namespace Helldivers2ModManager.ViewModels;

internal abstract class PageViewModelBase : ObservableObject
{
	public abstract string Title { get; }
}