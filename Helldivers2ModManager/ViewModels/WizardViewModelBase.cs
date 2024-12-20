using CommunityToolkit.Mvvm.ComponentModel;

namespace Helldivers2ModManager.ViewModels;

internal abstract class WizardViewModelBase : ObservableObject
{
	public event EventHandler? IsValidChanged;

	public abstract bool IsValid();

	protected virtual void OnIsValidChanged()
	{
		IsValidChanged?.Invoke(this, EventArgs.Empty);
	}
}
