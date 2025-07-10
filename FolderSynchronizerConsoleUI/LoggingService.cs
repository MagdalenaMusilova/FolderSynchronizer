using FolderSynchronizer;

namespace FolderSynchronizerConsoleUI
{
	public class LoggingService : ILoggingService
	{
		private bool _consoleEnabled = false;
		private StreamWriter? _logFileStream = null;

		public LoggingService(string? logFile = null, bool consoleEnabled = false) {
			_consoleEnabled = consoleEnabled;
			if (logFile != null) {
				_logFileStream = new StreamWriter(new FileStream(logFile, FileMode.Append)) { AutoFlush = true};
			}
		}

		public void Dispose() {
			_logFileStream?.Flush();
			_logFileStream?.Dispose();
			_logFileStream?.Close();
		}

		public void Log(string message) {
			if (_consoleEnabled) {
				Console.WriteLine(message);
			}
			if (_logFileStream != null) {
				_logFileStream.WriteLine(message);
			}
		}

		public void LogError(string message, Exception e) {
			if (_consoleEnabled) {
				Console.WriteLine(message);
			}
			if (_logFileStream != null) {
				_logFileStream.WriteLine(message);
			}
		}
	}
}
