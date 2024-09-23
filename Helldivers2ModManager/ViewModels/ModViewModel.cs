using CommunityToolkit.Mvvm.ComponentModel;
using Helldivers2ModManager.Models;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Helldivers2ModManager.ViewModels
{
	internal sealed partial class ModViewModel(ModData mod) : ObservableObject
	{
		public Guid Guid => _mod.Manifest.Guid;

		public string Name => _mod.Manifest.Name;

		public string Description => _mod.Manifest.Description;

		public Visibility OptionsVisible => _mod.Manifest.Options is null ? Visibility.Collapsed : Visibility.Visible;

		public IReadOnlyList<string>? Options => _mod.Manifest.Options;

		public ImageSource Icon { get; } = new BitmapImage(mod.Manifest.IconPath is null ? new Uri("../Resources/Images/logo_icon.png", UriKind.Relative) : new Uri(Path.Combine(mod.Directory.FullName, mod.Manifest.IconPath), UriKind.Absolute));

		public ModData Data => _mod;
		public int SelectedOption
		{
			get => _mod.Option;
			set
			{
				OnPropertyChanging();
				_mod.Option = value;
				OnPropertyChanged();
			}
		}

		private readonly ModData _mod = mod;
		[ObservableProperty]
		private bool _enabled = false;
	}
}
