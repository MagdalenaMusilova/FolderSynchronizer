using FolderSynchronizerConsoleUI;

namespace FolderSynchronizer
{
	public class LoggingService : ILoggingService
	{
		private bool _consoleEnabled = false;
		private StreamWriter? _logFileStream = null;

		public LoggingService(string? logFile = null, bool consoleEnabled = false) {
			_consoleEnabled = consoleEnabled;
			if (logFile != null) {
				_logFileStream = new StreamWriter(new FileStream(logFile, FileMode.Append));
			}
		}

		public void Dispose() {
			_logFileStream?.Flush();
			_logFileStream?.Dispose();
		}

		public void Log(string message) {
			if (_consoleEnabled) {
				Console.WriteLine(message);
			}
			if (_logFileStream != null) {
				_logFileStream.WriteLine(message);
			}
		}

		public void LogError(string message, params object[] args) {
			if (_consoleEnabled) {
				Console.WriteLine(message);
			}
			if (_logFileStream != null) {
				_logFileStream.WriteLine(message);
			}
		}
	}
}
