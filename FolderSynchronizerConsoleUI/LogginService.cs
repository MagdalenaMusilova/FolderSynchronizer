using FolderSynchronizerConsoleUI;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizerTests
{
	public class LogginService : ILoggingService
	{
		private bool _consoleEnabled = false;
		private StreamWriter? _logFileStream = null;

		public LogginService(string? logFile = null, bool consoleEnabled = false) {
			_consoleEnabled = consoleEnabled;
			if (logFile != null) {
				_logFileStream = new StreamWriter(new FileStream(logFile, FileMode.Append));
			}
		}

		~LogginService() { 
			if (_logFileStream != null) {
				_logFileStream.Close();
			}
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
