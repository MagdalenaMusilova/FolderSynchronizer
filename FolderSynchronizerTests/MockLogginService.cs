using FolderSynchronizerConsoleUI;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizerTests
{
	public class MockLogginService : ILoggingService
	{
		public List<string> logs;
		public List<(string message, object[] args)> errorLogs;

		public MockLogginService() {
			logs = new List<string>();
			errorLogs = new List<(string message, object[] args)>();
		}

		public void Log(string message) {
			logs.Add(message);
		}

		public void LogError(string message, params object[] args) {
			errorLogs.Add((message, args));
		}
	}
}
