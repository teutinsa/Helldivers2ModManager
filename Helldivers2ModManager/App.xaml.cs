// Ignore Spelling: App

using Helldivers2ModManager.Services;
using Helldivers2ModManager.Stores;
using Helldivers2ModManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Helldivers2ModManager;

internal partial class App : Application
{
	public static new App Current => (App)Application.Current;
	
	public static Version Version { get; } = new Version(1, 0, 0, 0);

	public IHost Host { get; }

	private readonly ILogger _logger;

	public App()
	{
		HostApplicationBuilder builder = new();

		AddServices(builder.Services);
		AddStores(builder.Services);
		AddViewModels(builder.Services);
		builder.Services.AddLogging(log =>
		{
#if DEBUG
			log.AddDebug();
#endif
			log.AddFile("ModManager.log");
		});
		builder.Services.AddTransient<MainWindow>();
		
		Host = builder.Build();

		_logger = Host.Services.GetRequiredService<ILogger<App>>();

		AppDomain.CurrentDomain.UnhandledException += (_, e) => _logger.LogError("An unhandled exception occured!");
		Current.DispatcherUnhandledException += (_, e) => _logger.LogError(e.Exception, "An unhandled exception occured!");
		TaskScheduler.UnobservedTaskException += (_, e) => _logger.LogError(e.Exception, "An unhandled exception occured!");
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		MainWindow = Host.Services.GetRequiredService<MainWindow>();
		MainWindow.Show();
	}

	private static void AddServices(IServiceCollection services)
	{
		services.AddTransient<NexusService>();
	}

	private static void AddStores(IServiceCollection services)
	{
		services.AddSingleton(static provider => new NavigationStore(provider, provider.GetRequiredService<DashboardPageViewModel>()));
		services.AddSingleton<ModStore>();
		services.AddSingleton<SettingsStore>();
	}

	private static void AddViewModels(IServiceCollection services)
	{
		services.AddTransient<MainViewModel>();
		services.AddTransient<DashboardPageViewModel>();
		services.AddTransient<SettingsPageViewModel>();
	}
}

