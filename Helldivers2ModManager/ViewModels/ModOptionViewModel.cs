using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Helldivers2ModManager.ViewModels;

internal sealed class ModOptionViewModel(ModViewModel vm, int idx) : ObservableObject
{
	public string Name => _vm.Data.Manifest.V1.Options![_idx].Name;

	public bool Enabled
	{
		get => _vm.Data.EnabledOptions[_idx];

		set
		{
			OnPropertyChanging();
			_vm.Data.EnabledOptions[_idx] = value;
			OnPropertyChanged();
		}
	}

	public string Description => _vm.Data.Manifest.V1.Options![_idx].Description;

	public Visibility ImageVisibility => _vm.Data.Manifest.V1.Options![_idx].Image is not null ? Visibility.Visible : Visibility.Collapsed;

	public ImageSource? Image
	{
		get
		{
			if (_vm.Data.Manifest.V1.Options![_idx].Image is string path)
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(Path.Combine(_vm.Data.Directory.FullName, path));
				bmp.CacheOption = BitmapCacheOption.None;
				bmp.EndInit();
				return bmp;
			}
			return null;
		}
	}

	public Visibility SubOptionVisibility => _vm.Data.Manifest.V1.Options![_idx].SubOptions is not null ? Visibility.Visible : Visibility.Collapsed;

	public ModSubOptionViewModel[]? SubOptions => _subs;

	public int SelectedSubOption
	{
		get => _vm.Data.SelectedOptions[_idx];

		set
		{
			OnPropertyChanging();
			_vm.Data.SelectedOptions[_idx] = value;
			OnPropertyChanged();
		}
	}

	private readonly ModViewModel _vm = vm;
	private readonly int _idx = idx;
	private readonly ModSubOptionViewModel[]? _subs = vm.Data.Manifest.V1.Options![idx].SubOptions?.Select((_, i) => new ModSubOptionViewModel(vm, idx, i)).ToArray();
}
