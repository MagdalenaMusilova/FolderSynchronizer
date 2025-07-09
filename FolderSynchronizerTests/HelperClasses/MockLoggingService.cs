using FolderSynchronizerConsoleUI;

namespace FolderSynchronizerTests.HelperClasses
{
	public class MockLoggingService : ILoggingService
	{
		public List<string> logs;
		public List<(string message, Exception e)> errorLogs;

		public MockLoggingService() {
			logs = new List<string>();
			errorLogs = new List<(string message, Exception e)>();
		}

		public void Dispose() {
		}

		public void Log(string message) {
			logs.Add(message);
		}

		public void LogError(string message, Exception e) {
			errorLogs.Add((message, e));
		}

		public void Clear() {
			logs.Clear();
			errorLogs.Clear();
		}
	}
}
