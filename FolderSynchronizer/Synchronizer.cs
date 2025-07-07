using Microsoft.Extensions.Logging;
using System.Collections;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text.Json;

namespace FolderSynchronizer
{
    public class Synchronizer
    {
        private readonly IFileSystem _fs;
		private List<Timer> _timers;
		private ILogger _logger;

		public Synchronizer(IFileSystem fileSystem, ILogger logger) {
			_fs = fileSystem;
			_timers = new List<Timer>();
			_logger = logger;
		}

		public void SynchronizePeriodically(string pathToFolder, string pathToReplica, long intervalInSeconds) {
			_timers.Add(new Timer(_ => Synchronize(pathToFolder, pathToReplica), null, 0, intervalInSeconds));
		}

		public void Synchronize(string pathToFolder, string pathToReplica) {
			_logger.LogInformation($"Starting synchronization of {pathToFolder} to {pathToReplica}");

            if (!_fs.Directory.Exists(pathToFolder)) {
                throw new DirectoryNotFoundException("The directory to synchronize was not found.");
            }

			SyncFolder(pathToFolder, pathToReplica);
        }

        private void SyncFolder(string pathToFolder, string pathToReplica) {
			if (!_fs.Directory.Exists(pathToReplica)) {
				_logger.LogInformation($"Creating directory {pathToReplica}");
				try {
					_fs.Directory.CreateDirectory(pathToReplica);
				} catch (Exception) {
					_logger.LogError($"Failed to create directory {pathToReplica}, skipping syncing folder.", pathToReplica);
					return;	//doesn't need to throw exception (skipping this is fine + forwarding the info), but syncing the files here can't be continued
				}
			}
			SyncFiles(pathToFolder, pathToReplica);	

			HashSet<string> orgFoldersRel = _fs.Directory.GetDirectories(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> replicaFoldersRel = _fs.Directory.GetDirectories(pathToReplica)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();

			foreach (var folderPathRel in orgFoldersRel) {
				string orgFolderPathAbs = Path.Combine(pathToFolder, folderPathRel);
				string replicaFolderPathAbs = Path.Combine(pathToReplica, folderPathRel);
				SyncFolder(orgFolderPathAbs, replicaFolderPathAbs);
			}

            var deletedFolders = replicaFoldersRel.Except(orgFoldersRel);
            foreach (var folderPathRel in deletedFolders) {
				string folderPathAbs = Path.Combine(pathToReplica, folderPathRel);
				_logger.LogInformation($"Deleting directory {pathToReplica}");
				try {
					_fs.Directory.Delete(folderPathAbs, true);
				} catch (Exception) {
					_logger.LogError($"Failed to delete directory {folderPathAbs}.", folderPathAbs);
				}
				
			}
		}

		private void SyncFiles(string pathToFolder, string pathToReplica) {
			HashSet<string> orgFilePaths = _fs.Directory.GetFiles(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> repFilePaths = _fs.Directory.GetFiles(pathToReplica)
				.Select(path => Path.GetRelativePath(pathToReplica, path))
				.ToHashSet();

            foreach (string path in orgFilePaths) {
				string sourceFilePath = Path.Combine(pathToFolder, path);
				string replicaFilePath = Path.Combine(pathToReplica, path);

                if (repFilePaths.Contains(path) && AreFilesEqual(sourceFilePath, replicaFilePath)) {  //file was not updated -> nothing has to be done
					continue;
				}

				_logger.LogInformation($"Copying file {replicaFilePath}");
				try {
					_fs.File.Copy(sourceFilePath, replicaFilePath, true);
				} catch (Exception) {
					_logger.LogError($"Failed to copy file {replicaFilePath}", replicaFilePath);
				}
			}

            var deletedFiles = repFilePaths.Except(orgFilePaths);
            foreach (string path in deletedFiles) { 
				string pathAbs = Path.Combine(pathToReplica, path);
				_logger.LogInformation($"Deleting file {pathAbs}");
				try {
					_fs.File.Delete(pathAbs);
				} catch (Exception) {
					_logger.LogError($"Failed to delete file {pathAbs}", pathAbs);
				}
			}
        }

		public bool AreFilesEqual(string filePath1, string filePath2) {
			if (!_fs.File.Exists(filePath1) || !_fs.File.Exists(filePath2)) {
				return false;
			}
			using (var sha256 = SHA256.Create()) {
				using (var stream1 = _fs.File.OpenRead(filePath1))
				using (var stream2 = _fs.File.OpenRead(filePath2)) {
					var hash1 = sha256.ComputeHash(stream1);
					var hash2 = sha256.ComputeHash(stream2);

					return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
				}
			}
		}
	}
}
