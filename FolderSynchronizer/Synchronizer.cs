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

		public Synchronizer(IFileSystem fileSystem) {
            _fs = fileSystem;
        }

        public void Synchronize(string pathToFolder, string pathToReplica) {
            if (!_fs.Directory.Exists(pathToFolder)) {
                throw new DirectoryNotFoundException("The directory to synchronize was not found.");
            }

			SyncFolder(pathToFolder, pathToReplica);
        }

        private void SyncFolder(string pathToFolder, string pathToReplica) {
			if (!_fs.Directory.Exists(pathToReplica)) {
				_fs.Directory.CreateDirectory(pathToReplica);
			}
            SyncFiles(pathToFolder, pathToReplica);

			HashSet<string> orgFoldersRel = _fs.Directory.GetDirectories(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> replicaFoldersRel = _fs.Directory.GetDirectories(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();

			foreach (var folderPathRel in orgFoldersRel) {
				string orgFolderPathAbs = Path.Combine(pathToFolder, folderPathRel);
				string replicaFolderPathAbs = Path.Combine(pathToFolder, folderPathRel);
				SyncFolder(orgFolderPathAbs, replicaFolderPathAbs);
			}

            var deletedFolders = replicaFoldersRel.Except(orgFoldersRel);
            foreach (var folderPathRel in deletedFolders) {
				string folderPathAbs = Path.Combine(pathToReplica, folderPathRel);
                _fs.Directory.Delete(folderPathAbs, true);
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
				_fs.File.Copy(sourceFilePath, replicaFilePath, true);
			}

            var deletedFiles = repFilePaths.Except(orgFilePaths);
            foreach (string path in deletedFiles) { 
				string pathAbs = Path.Combine(pathToReplica, path);
                _fs.File.Delete(pathAbs);
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
