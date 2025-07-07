using Microsoft.Extensions.Logging;
using spkl.Diffs;
using System.Collections;
using System.IO.Abstractions;
using System.Security.Cryptography;

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

				if (repFilePaths.Contains(path)) {
					if (AreFilesEqual(sourceFilePath, replicaFilePath)) {   //file wasn't changed since last sync -> nothing has to be done
						continue;
					} else {    //file was changed since last sync -> update it
						_logger.LogInformation($"Updating file {replicaFilePath}");
						try {
							SyncFile(sourceFilePath, replicaFilePath);
						} catch (Exception e) {
							_logger.LogError($"Failed to update file {replicaFilePath}", e);
						}
					}
				} else {	//completely new file
					_logger.LogInformation($"Copying file {replicaFilePath}");
					try {
						CopyFile(sourceFilePath, replicaFilePath);
					} catch (Exception e) {
						_logger.LogError($"Failed to copy file {replicaFilePath}", e);
					}
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

		private void SyncFile(string sourceFile, string replicaFile) {
			List<Chunk> sourceChunks = SplitFileIntoChunks(_fsSource, sourceFile);
			List<Chunk> replicaChunks = SplitFileIntoChunks(_fsReplica, replicaFile);

			MyersDiff<Chunk> diff = new MyersDiff<Chunk>(replicaChunks.ToArray(), sourceChunks.ToArray());
			var edits = diff.GetEditScript();

			string tempFile = GetTempFileName(_fsReplica, replicaFile);

			try {
				using (var sourceStream = _fsSource.File.OpenRead(sourceFile))
				using (var replicaStream = _fsReplica.File.OpenRead(replicaFile))
				using (var tempStream = _fsReplica.File.OpenWrite(tempFile)) {
					long index = 0;
					foreach ((int indexReplicaChunk, int sourceIndexChunk, int removeLenghtChunk, int insertLenghtChunk) in edits) {
						int indexReplica = replicaChunks[indexReplicaChunk].index;
						int sourceIndex = sourceChunks[sourceIndexChunk].index;
						long removeLenght = 0;
						long insertLenght = 0;

						for (int i = indexReplicaChunk; i < removeLenghtChunk; i++) {
							removeLenght += replicaChunks[i].size;
						}
						for (int i = sourceIndexChunk; i < insertLenghtChunk; i++) {
							insertLenght += sourceChunks[i].size;
						}

						//write bytes until the next edit
						while (index + _bufferSize <= indexReplica) {
							CopyStream(replicaStream, tempStream, _bufferSize);
							index += _bufferSize;
						}
						//perform edits
						if (removeLenght > 0) { //skip bytes that were removed
							replicaStream.Seek(removeLenght, SeekOrigin.Current);
							index += removeLenght;
						}
						if (insertLenght > 0) {
							sourceStream.Seek(sourceIndex, SeekOrigin.Begin);
							for (int i = 0; i < insertLenght / _bufferSize; i++) {
								CopyStream(sourceStream, tempStream, _bufferSize);
							}
							CopyStream(sourceStream, tempStream, (int)(insertLenght % _bufferSize));
						}
					}
					//write the rest of file
					while (CopyStream(replicaStream, tempStream, _bufferSize) > 0) { }
				}
			} catch (Exception) {
				_fsReplica.File.Delete(tempFile);
				throw;
			}
			_fsReplica.File.Replace(tempFile, replicaFile, null);
		}

		private int CopyStream(FileSystemStream source, FileSystemStream dest, int len) {
			byte[] buffer = new byte[_bufferSize];
			int bytesRead = source.Read(buffer, 0, len);
			dest.Write(buffer, 0, bytesRead);
			return bytesRead;
		}

		private string GetTempFileName(IFileSystem fs, string filePath) {
			for (int i = 0; i < 5; i++) {
				string res = $"{filePath}_{Guid.NewGuid()}";
				if (!fs.File.Exists(res)) {
					return res ;
				}
			}
			throw new ArgumentException($"Failed to generate name for temporary file when copying {filePath}");
		}

		private List<Chunk> SplitFileIntoChunks(IFileSystem fs, string pathToFile) {
			List<Chunk> chunks = new List<Chunk>();

			var buffer = new byte[_bufferSize];
			int index = 0;
			using (var stream = fs.File.OpenRead(pathToFile)) {
				int bytesRead;
				while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
					byte[] hash = MD5.HashData(buffer);
					chunks.Add(new Chunk() {
						hash = hash,
						size = bytesRead,
						index = index,
					});
					index += bytesRead;
				}
			}

			return chunks;
		}

		private void CopyFile(string sourceFile, string replicaFile) {
			using (var sourceStream = _fsSource.File.OpenRead(sourceFile))
			using (var replicaStream = _fsReplica.File.OpenWrite(replicaFile)) {
				byte[] buffer = new byte[_bufferSize];
				int bytesRead;
				while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
					replicaStream.Write(buffer, 0, bytesRead);
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
