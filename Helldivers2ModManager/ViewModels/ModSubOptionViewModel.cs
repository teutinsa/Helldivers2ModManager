using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Helldivers2ModManager.Models;

namespace Helldivers2ModManager.ViewModels;

internal sealed class ModSubOptionViewModel(ModViewModel vm, int idx, int subIdx) : ObservableObject
{
	public string Name => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Name;

	public string Description => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Description;

	public Visibility ImageVisibility => ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Image is not null ? Visibility.Visible : Visibility.Collapsed;

	public ImageSource? Image
	{
		get
		{
			var path = ((V1ModManifest)_vm.Data.Manifest).Options![_idx].SubOptions![_subIdx].Image;
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

	private readonly ModViewModel _vm = vm;
	private readonly int _idx = idx;
	private readonly int _subIdx = subIdx;
}
