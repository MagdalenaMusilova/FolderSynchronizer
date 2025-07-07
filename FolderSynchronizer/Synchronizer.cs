using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Json;

namespace FolderSynchronizer
{
    public class Synchronizer
    {
        private readonly IFileSystem _fsSource;
		private readonly IFileSystem _fsReplica;
		private List<Timer> _timers;
		private ILogger _logger;
		private int _bufferSize;

		public Synchronizer(IFileSystem sourceFileSystem, IFileSystem replicaFileSystem, ILogger logger, int bufferSize = 1024 * 1024) {
			_fsSource = sourceFileSystem;
			_fsReplica = replicaFileSystem;
			_timers = new List<Timer>();
			_logger = logger;
			_bufferSize = bufferSize;
		}

		public void SynchronizePeriodically(string pathToFolder, string pathToReplica, long intervalInSeconds) {
			_timers.Add(new Timer(_ => Synchronize(pathToFolder, pathToReplica), null, 0, intervalInSeconds));
		}

		public void Synchronize(string pathToFolder, string pathToReplica) {
			_logger.LogInformation($"Starting synchronization of {pathToFolder} to {pathToReplica}");

            if (!_fsSource.Directory.Exists(pathToFolder)) {
                throw new DirectoryNotFoundException("The directory to synchronize was not found.");
            }

			SyncFolder(pathToFolder, pathToReplica);
        }

        private void SyncFolder(string pathToFolder, string pathToReplica) {
			if (!_fsReplica.Directory.Exists(pathToReplica)) {
				_logger.LogInformation($"Creating directory {pathToReplica}");
				try {
					_fsReplica.Directory.CreateDirectory(pathToReplica);
				} catch (Exception e) {
					_logger.LogError($"Failed to create directory {pathToReplica}, skipping syncing folder.", e);
					return;	//doesn't need to throw exception (skipping this is fine + forwarding the info), but syncing the files here can't be continued
				}
			}
			SyncFiles(pathToFolder, pathToReplica);	

			HashSet<string> orgFoldersRel = _fsSource.Directory.GetDirectories(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> replicaFoldersRel = _fsReplica.Directory.GetDirectories(pathToReplica)
				.Select(path => Path.GetRelativePath(pathToReplica, path))
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
					_fsReplica.Directory.Delete(folderPathAbs, true);
				} catch (Exception e) {
					_logger.LogError($"Failed to delete directory {folderPathAbs}.", e);
				}
				
			}
		}

		private void SyncFiles(string pathToFolder, string pathToReplica) {
			HashSet<string> orgFilePaths = _fsSource.Directory.GetFiles(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> repFilePaths = _fsReplica.Directory.GetFiles(pathToReplica)
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
					CopyFile(sourceFilePath, replicaFilePath);
				} catch (Exception e) {
					_logger.LogError($"Failed to copy file {replicaFilePath}", e);
				}
			}

            var deletedFiles = repFilePaths.Except(orgFilePaths);
            foreach (string path in deletedFiles) { 
				string pathAbs = Path.Combine(pathToReplica, path);
				_logger.LogInformation($"Deleting file {pathAbs}");
				try {
					_fsReplica.File.Delete(pathAbs);
				} catch (Exception e) {
					_logger.LogError($"Failed to delete file {pathAbs}", e);
				}
			}
        }

		private void CopyFile(string sourceFile, string replicaFile) {
			using (var sourceStream = _fsSource.File.OpenRead(sourceFile))
			using (var replicaStream = _fsReplica.File.OpenWrite(replicaFile)) {
				byte[] buffer = new byte[_bufferSize];
				int numOfReadBytes;
				while ((numOfReadBytes = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
					replicaStream.Write(buffer, 0, numOfReadBytes);
				}
			}
		}

		public bool AreFilesEqual(string sourceFile, string replicaFile) {
			if (!_fsSource.File.Exists(sourceFile) || !_fsReplica.File.Exists(replicaFile)) {
				return false;
			}
			using (var sha256 = SHA256.Create()) {
				using (var stream1 = _fsSource.File.OpenRead(sourceFile))
				using (var stream2 = _fsReplica.File.OpenRead(replicaFile)) {
					var hash1 = sha256.ComputeHash(stream1);
					var hash2 = sha256.ComputeHash(stream2);

					return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
				}
			}
		}
	}
}
