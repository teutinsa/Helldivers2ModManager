using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Models;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Helldivers2ModManager.ViewModels
{
	internal sealed partial class ModViewModel : ObservableObject
	{
		public Guid Guid => _mod.Manifest.Guid;

		public string Name => _mod.Manifest.Name;

		public string Description => _mod.Manifest.Description;

		public Visibility OptionsVisible
		{
			get
			{
				if (_mod.Manifest.Version == ModManifest.ManifestVersion.Legacy && _mod.Manifest.Legacy.Options is not null)
					return Visibility.Visible;
				return Visibility.Collapsed;
			}
		}

		public Visibility EditVisible => _mod.Manifest.Version == ModManifest.ManifestVersion.V1 ? Visibility.Visible : Visibility.Collapsed;

		public ImageSource Icon { get; }

		public ModData Data => _mod;

		public string[]? LegacyOptions { get; }

		public int LegacySelectedOption
		{
			get => _mod.Manifest.Version == ModManifest.ManifestVersion.Legacy ? _mod.SelectedOptions[0] : -1;

			set
			{
				if (_mod.Manifest.Version == ModManifest.ManifestVersion.Legacy)
				{
					OnPropertyChanging();
					_mod.SelectedOptions[0] = value;
					OnPropertyChanged();
				}
			}
		}

		public ModOptionViewModel[]? Options { get; }

		private readonly ModData _mod;
		[ObservableProperty]
		private bool _enabled;

		public ModViewModel(ModData mod)
		{
			_mod = mod;
			_enabled = false;

			switch (_mod.Manifest.Version)
			{
				case ModManifest.ManifestVersion.Legacy:
					LegacyOptions = _mod.Manifest.Legacy.Options?.ToArray();
					break;

				case ModManifest.ManifestVersion.V1:
					if (_mod.Manifest.V1.Options is null)
						break;
					Options = new ModOptionViewModel[_mod.Manifest.V1.Options.Count];
					for (int i = 0; i < _mod.Manifest.V1.Options.Count; i++)
						Options[i] = new ModOptionViewModel(this, i);
					break;
			}

			var bmp = new BitmapImage();
			bmp.BeginInit();
			if (_mod.Manifest.IconPath is string path)
			{
				bmp.UriSource = new Uri(Path.Combine(_mod.Directory.FullName, path));
				bmp.CacheOption = BitmapCacheOption.OnLoad;
			}
			else
				bmp.UriSource = new Uri(@"..\Resources\Images\logo_icon.png", UriKind.Relative);
			bmp.EndInit();
			Icon = bmp;
		}

		private void Update()
		{

		}
	}
}
