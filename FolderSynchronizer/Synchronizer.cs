using FolderSynchronizer.Manifest;
using spkl.Diffs;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;

namespace FolderSynchronizer
{
    public class Synchronizer
    {
        public static readonly string manifestPathRel = "manifest.json";
        private readonly IFileSystem _fs;
        private readonly FileScanner _scanner;

        public Synchronizer(IFileSystem? fileSystem = null, int chunkSize = 4096) {
            _fs = fileSystem ?? new FileSystem();
            _scanner = new FileScanner(_fs, chunkSize);
        }

        public void Synchronize(string pathToFolder, string pathToReplica) {
            if (_fs.Directory.Exists(pathToReplica)) { //check if this is the first time syncing this file
                ReplicaManifest manifest = GetManifest(Path.Combine(pathToReplica, "manifest.json"));
                if (manifest.FolderPath != pathToFolder) {
                    throw new ArgumentException($"The replica folder is already used for syncing another folder. It's current used as replica for {manifest.FolderPath}");
                }
                UpdateSync(pathToFolder, pathToReplica, manifest);
            } else {
                FullSync(pathToFolder, pathToReplica);
            }
        }

        private void FullSync(string pathToFolder, string pathToReplica) {
			CopyAllFilesInDirectory(pathToFolder, pathToReplica);
            AddManifestToReplica(pathToFolder, pathToReplica);
		}

        private void UpdateSync(string pathToFolder, string pathToReplica, ReplicaManifest manifest) {
            List<FileDetails> fileDetails = new List<FileDetails>();

            string[] filesPathsAbs = _fs.Directory.GetFiles(pathToFolder, "*", SearchOption.AllDirectories);
            string[] filePaths = filesPathsAbs.Select(path => Path.GetRelativePath(pathToFolder, path)).ToArray();
            foreach (string path in filePaths) {
				string sourceFilePath = Path.Combine(pathToFolder, path);
				string replicaFilePath = Path.Combine(pathToReplica, path);

				if (manifest.Files.ContainsKey(path)) { //possibly updated file
					List<Chunk> sourceChunks = _scanner.SplitFileIntoChunks(sourceFilePath);
                    if (sourceChunks.SequenceEqual(manifest.Files[path].Chunks)) {  //file was not updated -> no change
                        continue;
                    }

                    FileSynchronizer.SynchronizeFile(_fs, sourceFilePath, replicaFilePath, sourceChunks, manifest.Files[path].Chunks);
					fileDetails.Add(GetFileDetails(replicaFilePath));
				} else {    //completely new file
					_fs.File.Copy(sourceFilePath, replicaFilePath);
                    fileDetails.Add(GetFileDetails(replicaFilePath));
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



        private void AddManifestToReplica(string pathToFolder, string pathToReplica, ReplicaManifest? manifest = null) {
            manifest = manifest ?? CreateNewManifest(pathToFolder, pathToReplica);
			string jsonstring = JsonSerializer.Serialize(manifest);
			string manifestPath = Path.Combine(pathToReplica, manifestPathRel);
			_fs.File.WriteAllText(manifestPath, jsonstring);
		}

        private ReplicaManifest CreateNewManifest(string pathToFolder, string pathToReplica) {
			Dictionary<string, FileDetails> fileDetails = new Dictionary<string, FileDetails>();
            string[] filePaths = _fs.Directory.GetFiles(pathToReplica, "*", SearchOption.AllDirectories);
            foreach (string path in filePaths) {
				string relativePath = Path.GetRelativePath(pathToReplica, path);
				fileDetails.Add(relativePath, GetFileDetails(path));
            }

            DateTime created = DateTime.Now;
            ReplicaManifest manifest = new() {
                Created = created,
                Updated = created,
                FolderPath = pathToFolder,
                Files = fileDetails
            };
            return manifest;
        }

		private FileDetails GetFileDetails(string pathToFile) {			
			List<Chunk> chunks = _scanner.SplitFileIntoChunks(pathToFile);
			IFileInfo fileInfo = _fs.FileInfo.New(pathToFile);
			string checksum = _scanner.GetFileChecksum(pathToFile);
            return new FileDetails() {
                Chunks = chunks,
                Size = fileInfo.Length,
                Checksum = checksum
            };
		}

		private ReplicaManifest GetManifest(string pathToManifest) {
            if (!_fs.File.Exists(pathToManifest)) {
                throw new FileNotFoundException("The replica folder does not contain a manifest.");
            }

            string jsonString = _fs.File.ReadAllText(pathToManifest);
            ReplicaManifest manifest;
			try {
				manifest = JsonSerializer.Deserialize<ReplicaManifest>(jsonString);
                if (manifest == null) {
                    throw new Exception();
                }
			} catch (Exception) {
                throw new InvalidDataException("The manifest file is corrupted.");
			}
            
            return manifest;
        }
    }
}
