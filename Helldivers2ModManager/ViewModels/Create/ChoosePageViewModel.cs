using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Helldivers2ModManager.ViewModels.Create;

internal sealed partial class ChoosePageViewModel : WizardViewModelBase
{
	[ObservableProperty]
	private bool _hasOptions;
	[ObservableProperty]
	private bool _hasNoOptions;

	public override bool IsValid()
	{
		return HasOptions == !HasNoOptions;
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(HasOptions) || e.PropertyName == nameof(HasNoOptions))
			OnIsValidChanged();
		base.OnPropertyChanged(e);
	}
}
