using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Helldivers2ModManager.ViewModels;

internal sealed class ModSubOptionViewModel(ModViewModel vm, int idx, int subIdx) : ObservableObject
{
	public string Name => _vm.Data.Manifest.V1.Options![_idx].SubOptions![_subIdx].Name;

	public string Description => _vm.Data.Manifest.V1.Options![_idx].SubOptions![_subIdx].Description;

	public Visibility ImageVisibility => _vm.Data.Manifest.V1.Options![_idx].SubOptions![_subIdx].Image is not null ? Visibility.Visible : Visibility.Collapsed;

	public ImageSource? Image
	{
		get
		{
			if (_vm.Data.Manifest.V1.Options![_idx].SubOptions![_subIdx].Image is string path)
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(Path.Combine(_vm.Data.Directory.FullName, path));
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();
				return bmp;
			}
			return null;
		}
	}

	private readonly ModViewModel _vm = vm;
	private readonly int _idx = idx;
	private readonly int _subIdx = subIdx;
}
