using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace Helldivers2ModManager;

internal static class FileLoggerExtensions
{
	public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filename)
	{
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(provider => new FileLoggerProvider(filename)));
		return builder;
	}
}

[ProviderAlias("File")]
internal sealed class FileLoggerProvider : ILoggerProvider
{
	private readonly FileStream _fileStream;
	private readonly StreamWriter _stream;

	public FileLoggerProvider(string filename)
	{
		if (!Directory.Exists("logs"))
			Directory.CreateDirectory("logs");

		_fileStream = new FileStream(Path.Combine("logs", $"{filename}_{DateTime.UtcNow:dd-MM-yyyy_HH-mm-ss}.log"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
		_stream = new StreamWriter(_fileStream);
	}

	public ILogger CreateLogger(string categoryName)
	{
		return new FileLogger(categoryName, _stream);
	}

	public void Dispose()
	{
		_stream.Dispose();
		_fileStream.Dispose();
	}
}

internal sealed class FileLogger(string name, StreamWriter stream) : ILogger
{
	private readonly string _name = name;
	private readonly StreamWriter _stream = stream;

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return null;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel != LogLevel.None;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
			return;

		ArgumentNullException.ThrowIfNull(formatter);

		string message = formatter.Invoke(state, exception);
		if (string.IsNullOrEmpty(message))
			return;

		var builder = new StringBuilder();
		builder.Append('[');
		builder.Append(DateTime.Now.ToString("HH:mm::ss"));
		builder.Append("] ");
		builder.Append(_name);
		builder.Append(" -> ");
		builder.Append(logLevel.ToString());
		builder.Append(": ");
		builder.Append(message);

		if (exception is not null)
		{
			builder.AppendLine();
			builder.Append('\t');
			builder.Append(exception.GetType().Name);
			builder.Append(": ");
			builder.Append(exception.Message);
			if (exception.StackTrace is not null)
			{
				builder.AppendLine();
				builder.Append("\t\t");
				builder.Append(exception.StackTrace?.ReplaceLineEndings($"{Environment.NewLine}\t\t"));
			}
		}

		_stream.WriteLine(builder.ToString());
		_stream.Flush();
	}
}