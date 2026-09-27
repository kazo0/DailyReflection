namespace DailyReflection.Uno.Droid;

/// <summary>
/// Writes log entries straight to logcat. Console output does not reach logcat in the
/// Native AOT store builds, so without this a release build's warnings and errors (a
/// failed startup migration, a skipped legacy setting) went nowhere.
/// </summary>
internal sealed class LogcatLoggerProvider : ILoggerProvider
{
	private const string Tag = "DailyReflection";

	public ILogger CreateLogger(string categoryName) => new LogcatLogger(categoryName);

	public void Dispose()
	{
	}

	private sealed class LogcatLogger(string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var message = $"{category}: {formatter(state, exception)}";
			if (exception is not null)
			{
				message += System.Environment.NewLine + exception;
			}

			global::Android.Util.Log.WriteLine(ToPriority(logLevel), Tag, message);
		}

		private static global::Android.Util.LogPriority ToPriority(LogLevel logLevel) => logLevel switch
		{
			LogLevel.Trace => global::Android.Util.LogPriority.Verbose,
			LogLevel.Debug => global::Android.Util.LogPriority.Debug,
			LogLevel.Information => global::Android.Util.LogPriority.Info,
			LogLevel.Warning => global::Android.Util.LogPriority.Warn,
			LogLevel.Error => global::Android.Util.LogPriority.Error,
			_ => global::Android.Util.LogPriority.Assert,
		};
	}
}
