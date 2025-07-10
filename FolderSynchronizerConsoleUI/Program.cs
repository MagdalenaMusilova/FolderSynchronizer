using FolderSynchronizer;
using System.IO.Abstractions;

namespace FolderSynchronizerConsoleUI
{
	internal class Program
	{
		static void Main(string[] args) {
			//arguments: [Path to source folder] [Path to replica folder] [Interval between updates] [Path to save logs] --quiet
			//If interval between updates = 0, the synchronization will be done once
			//If the Path to save logs is missing, the logging will be written only to console (unless --quiet is also included)
			//Put --quiet at the end of the command to not write logs into console

			if (args.Length >= 3) {
				string sourceFolder = args[0];
				string replicaFolder = args[1];
				int intervalBetweenUpdates;
				if (!int.TryParse(args[2], out intervalBetweenUpdates) || intervalBetweenUpdates < 0) {
					Console.WriteLine("Interval between updates must be a number bigger or equal to zero!");
					Console.WriteLine();
					WriteHelp();
					return;
				}

				string? logFile = null;
				if (args.Length > 3 && !args[3].StartsWith('-')) {	//check if 4th argument is log file
					logFile = args[3];
				}

				bool quiet = args.Contains("--quiet");

				IFileSystem fs = new FileSystem();
				ILoggingService loggingService = new LoggingService(logFile, !quiet);

				Synchronizer synchronizer = new Synchronizer(fs, fs);

				try {
					if (intervalBetweenUpdates > 0) {
						synchronizer.SynchronizePeriodically(sourceFolder, replicaFolder, intervalBetweenUpdates, loggingService);
						Console.WriteLine("Press Ctrl‑C to exit…");
						new ManualResetEvent(false).WaitOne();
					} else {
						synchronizer.Synchronize(sourceFolder, replicaFolder, loggingService);
					}
				} catch (Exception e) {
					Console.WriteLine("Error occured while synchronizing folders.");
					Console.WriteLine(e);
					return;
				}
			} else {
				WriteHelp();
				return;
			}
		}

		private static void WriteHelp() {
			Console.WriteLine("Usage: FolderSync.exe <source_folder> <replica_folder> <interval_seconds> <log_file_path> [--quiet]");
			Console.WriteLine();
			Console.WriteLine("Arguments:");
			Console.WriteLine("  <source_folder>     Path to the source folder to sync from");
			Console.WriteLine("  <replica_folder>    Path to the replica folder to sync to");
			Console.WriteLine("  <interval_seconds>  Sync interval in seconds (e.g., 60)");
			Console.WriteLine("                     - If set to 0, synchronization runs once and exits");
			Console.WriteLine("  <log_file_path>     Path to the log file (e.g., C:\\Logs\\sync.log)");
			Console.WriteLine("                     - If omitted, logs are written only to the console");
			Console.WriteLine("  --quiet             (Optional) Suppresses console output; logs only to file");
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("  --help              Show this help message and exit");
			Console.WriteLine();
			Console.WriteLine("Example:");
			Console.WriteLine("  FolderSync.exe \"C:\\Source\" \"D:\\Replica\" 0  \"C:\\Logs\\sync.log\" --quiet");
		}
	}
}
