using FolderSynchronizer.Manifest;
using System.IO.Abstractions;
using System.Text.Json;

namespace FolderSynchronizer
{
    public class FolderSynchronizer
    {
        public static readonly string manifestPathRel = "manifest.json";
        private readonly IFileSystem _fs;
        private readonly FileScanner _scanner;

        public FolderSynchronizer(IFileSystem? fileSystem = null, int chunkSize = 4096) {
            _fs = fileSystem ?? new FileSystem();
            _scanner = new FileScanner(_fs, chunkSize);
        }

        public void Synchronize(string pathToFolder, string pathToReplica) {
            if (_fs.Directory.Exists(pathToReplica)) { //check if this is the first time syncing this file
                ReplicaManifest manifest = GetManifest(pathToReplica);
                if (manifest.FolderPath != pathToFolder) {
                    throw new ArgumentException($"The replica folder is already used for syncing another folder. It's current used as replica for {manifest.FolderPath}");
                }
                UpdatesSync(pathToFolder, pathToReplica, manifest);
            } else {
                FullSync(pathToFolder, pathToReplica);
            }
        }

        private void FullSync(string pathToFolder, string pathToReplica) {
            CopyAllFiles(pathToFolder, pathToReplica);
            ReplicaManifest manifest = CreateNewManifest(pathToFolder, pathToReplica);
            
            string jsonstring = JsonSerializer.Serialize(manifest);
            string manifestPath = Path.Combine(pathToReplica, manifestPathRel);
            _fs.File.WriteAllText(manifestPath, jsonstring);
        }

        private void UpdatesSync(string pathToFolder, string pathToReplica, ReplicaManifest manifest) { 
            throw new NotImplementedException();
        }


        private void CopyAllFiles(string pathToFolder, string pathToReplica) {
			throw new NotImplementedException();
		}

        private ReplicaManifest CreateNewManifest(string pathToFolder, string pathToReplica) {
			Dictionary<string, FileDetails> fileDetails = new Dictionary<string, FileDetails>();
            string[] filePaths = _fs.Directory.GetFiles(pathToReplica, "*", SearchOption.AllDirectories);
            foreach (string path in filePaths) {
				string relativePath = Path.GetRelativePath(pathToReplica, path);
				List<Chunk> chunks = _scanner.SplitFileIntoChunks(path);
                IFileInfo fileInfo = _fs.FileInfo.New(path);
                string checksum = _scanner.GetFileChecksum(path);

                fileDetails.Add(relativePath, new FileDetails() { 
                    Chunks = chunks,
                    Size = fileInfo.Length,
                    Checksum = checksum
                });
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
