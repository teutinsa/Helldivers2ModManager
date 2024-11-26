using CommunityToolkit.Mvvm.ComponentModel;
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

		public Visibility OptionsVisible => _mod.Manifest.Options is null ? Visibility.Collapsed : Visibility.Visible;

		public IReadOnlyList<string>? Options => _mod.Manifest.Options;

		public ImageSource Icon { get; }

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

		private static readonly BitmapImage s_image;
		private readonly ModData _mod;
		[ObservableProperty]
		private bool _enabled;

		static ModViewModel()
		{
			s_image = new BitmapImage();
			s_image.BeginInit();
			s_image.UriSource = new Uri(@"..\Resources\Images\logo_icon.png", UriKind.Relative);
			s_image.EndInit();
		}

		public ModViewModel(ModData mod)
		{
			_mod = mod;
			_enabled = false;

			if (_mod.Manifest.IconPath is string path)
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(Path.Combine(_mod.Directory.FullName, path));
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();
				Icon = bmp;
			}
			else
				Icon = s_image;
		}
	}
}
