using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Helldivers2ModManager.Models;

namespace Helldivers2ModManager.ViewModels;

internal sealed class ModOptionViewModel(ModViewModel vm, int idx) : ObservableObject
{
	public string Name => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].Name;

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

	public string Description => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].Description;

	public Visibility ImageVisibility => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].Image is not null ? Visibility.Visible : Visibility.Collapsed;

	public ImageSource? Image
	{
		get
		{
			var path = ((V1ModManifest)_vm.Data.Manifest).Options![_idx].Image;
			if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
				return null;
			var bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri(Path.Combine(_vm.Data.Directory.FullName, path));
			bmp.CacheOption = BitmapCacheOption.None;
			bmp.EndInit();
			return bmp;
		}
	}

	public Visibility SubOptionVisibility => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions is not null ? Visibility.Visible : Visibility.Collapsed;

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
	private readonly ModSubOptionViewModel[]? _subs = ((V1ModManifest)vm.Data.Manifest).Options![idx].SubOptions?.Select((_, i) => new ModSubOptionViewModel(vm, idx, i)).ToArray();
}
