// Ignore Spelling: App

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Helldivers2ModManager;

internal partial class App : Application
{
	public static readonly Version Version = new(1, 3, 0, 0);

	public static readonly string? VersionAddition = "End of Life";

	public new static App Current => (App)Application.Current;

	public IHost Host { get; }
	
	public LogLevel LogLevel { get; set; }

	private readonly ILogger? _logger;

	public App()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, e) => LogUnhandledException(e.ExceptionObject as Exception);
		DispatcherUnhandledException += (_, e) => LogUnhandledException(e.Exception);
		TaskScheduler.UnobservedTaskException += (_, e) => LogUnhandledException(e.Exception);

		HostApplicationBuilder builder = new();

		AddServices(builder.Services);
		builder.Services.AddLogging(log =>
		{
#if DEBUG
			log.SetMinimumLevel(LogLevel.Trace);
			log.AddDebug();
#endif
			log.AddConsole();
			log.AddFile("ModManager");
		});
		builder.Services.AddTransient<MainWindow>();
		
		Host = builder.Build();

		_logger = Host.Services.GetRequiredService<ILogger<App>>();
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		MainWindow = Host.Services.GetRequiredService<MainWindow>();
		MainWindow.Show();
	}

	private static void AddServices(IServiceCollection services)
	{
		var tuples = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Select(static type => (type, type.GetCustomAttribute<RegisterServiceAttribute>()))
			.Where(static tuple => tuple.Item2 is not null)
			.Cast<ValueTuple<Type, RegisterServiceAttribute>>()
			.ToArray();

		foreach (var (type, attr) in tuples)
		{
			var desc = attr.Contract is null
				? new ServiceDescriptor(type, attr.Lifetime)
				: new ServiceDescriptor(attr.Contract, type, attr.Lifetime);
			services.Add(desc);
		}
	}
	
	private void LogUnhandledException(Exception? ex)
	{
		if (_logger is null)
			MessageBox.Show($"An unhandled exception occurred before logging could be initialized!\n\n{ex?.ToString()}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
		else
			_logger?.LogError(ex, "An unhandled exception occured!");
	}
}

