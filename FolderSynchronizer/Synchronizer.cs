using spkl.Diffs;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;

namespace FolderSynchronizer
{
    public class Synchronizer
    {
        private readonly IFileSystem _fs;
        private readonly FileScanner _scanner;

        public Synchronizer(IFileSystem? fileSystem = null, int chunkSize = 4096) {
            _fs = fileSystem ?? new FileSystem();
            _scanner = new FileScanner(_fs, chunkSize);
        }

        public void Synchronize(string pathToFolder, string pathToReplica) {
            if (_fs.Directory.Exists(pathToReplica)) { //check if this is the first time syncing this file
                UpdateSync(pathToFolder, pathToReplica);
            } else {
                FullSync(pathToFolder, pathToReplica);
            }
        }

        private void FullSync(string pathToFolder, string pathToReplica) {
			CopyAllFilesInDirectory(pathToFolder, pathToReplica);
		}

        private void UpdateSync(string pathToFolder, string pathToReplica) {
            string[] filePathsAbs = _fs.Directory.GetFiles(pathToFolder, "*", SearchOption.AllDirectories);
            string[] repFilesPathsAbs = _fs.Directory.GetFiles(pathToReplica, "*", SearchOption.AllDirectories);

			HashSet<string> filePaths = filePathsAbs.Select(path => Path.GetRelativePath(pathToFolder, path)).ToHashSet();
			HashSet<string> repFilePaths = repFilesPathsAbs.Select(path => Path.GetRelativePath(pathToReplica, path)).ToHashSet();

            foreach (string path in filePaths) {
				string sourceFilePath = Path.Combine(pathToFolder, path);
				string replicaFilePath = Path.Combine(pathToReplica, path);

                if (repFilePaths.Contains(path)) {  //possibly updated file
					List<Chunk> sourceChunks = _scanner.SplitFileIntoChunks(sourceFilePath);
                    List<Chunk> replicaChunks = _scanner.SplitFileIntoChunks(replicaFilePath);
					if (sourceChunks.SequenceEqual(replicaChunks)) {  //file was not updated -> no change
						continue;
					}
					FileSynchronizer.SynchronizeFile(_fs, sourceFilePath, replicaFilePath, sourceChunks, replicaChunks);
				} else {
					_fs.File.Copy(sourceFilePath, replicaFilePath);
				}
            }
        }

        private void CopyAllFilesInDirectory(string pathToSourceFolder, string pathToDestReplica) {
            if (!_fs.Directory.Exists(pathToDestReplica)) {
                _fs.Directory.CreateDirectory(pathToDestReplica);
            }

			var filePaths = _fs.Directory.GetFiles(pathToSourceFolder);
            var filePathsRelative = filePaths.Select(path => Path.GetRelativePath(pathToSourceFolder, path));
			foreach (string filePath in filePathsRelative) {
				string source = Path.Combine(pathToSourceFolder, filePath);
				string dest = Path.Combine(pathToDestReplica, filePath);
                _fs.File.Copy(source, dest, false);
			}

            var subfolderPathsRelative = _fs.Directory.GetDirectories(pathToSourceFolder);
            foreach (string subfolderPath in subfolderPathsRelative) {
                string source = Path.Combine(pathToSourceFolder, subfolderPath);
                string dest = Path.Combine(pathToDestReplica, subfolderPath);
                CopyAllFilesInDirectory(source, dest);
            }
        }
    }
}
