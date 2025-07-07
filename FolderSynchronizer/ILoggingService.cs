using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizerConsoleUI
{
	public interface ILoggingService
	{
		public void Log(string message);
		public void LogError(string message, params object[] args);
	}
}
