using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Services.Nexus;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Helldivers2ModManager.ViewModels;

internal sealed partial class NexusModViewModel : ObservableObject
{
	public string Name => _mod.Name;

	public string Summary => _mod.Summary;

	public string Author => _mod.Author;

	public string Version => _mod.Version;


	private static readonly BitmapImage s_image;
	private readonly NexusMod _mod;
	[ObservableProperty]
	private ImageSource _picture;

	static NexusModViewModel()
	{
		s_image = new BitmapImage();
		s_image.BeginInit();
		s_image.UriSource = new Uri(@"..\Resources\Images\logo_icon.png", UriKind.Relative);
		s_image.EndInit();
	}

	public NexusModViewModel(NexusMod mod)
	{
		_mod = mod;
		_picture = s_image;
		if (_mod.PrictureUrl is not null)
			Task.Run(LoadImage);
	}
	
	private async Task LoadImage()
	{
		using var client = new HttpClient();
		var data = await client.GetByteArrayAsync(_mod.PrictureUrl);
		using var stream = new MemoryStream(data);

		var bmp = new BitmapImage();
		bmp.BeginInit();
		bmp.CacheOption = BitmapCacheOption.OnLoad;
		bmp.StreamSource = stream;
		bmp.EndInit();
		bmp.Freeze();

		Picture = bmp;
	}

	[RelayCommand]
	void Download()
	{
		Process.Start(new ProcessStartInfo($"https://www.nexusmods.com/helldivers2/mods/{_mod.ModId}")
		{
			UseShellExecute = true
		});
	}
}
