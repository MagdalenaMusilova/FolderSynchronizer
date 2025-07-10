using spkl.Diffs;
using System.IO.Abstractions;
using System.Security.Cryptography;

namespace FolderSynchronizer
{
	/// <summary>
	/// Handles one-way synchronization from a source directory to a replica directory.
	/// </summary>
	public class Synchronizer : IDisposable
	{
		private readonly IFileSystem _fsSource;
		private readonly IFileSystem _fsReplica;
		private Timer? _timer = null;
		private ILoggingService _logger;
		private int _bufferSize;
		private byte[] _buffer;
		private byte[] _buffer2;


		/// <param name="sourceFileSystem">The file system for the source directory.</param>
		/// <param name="replicaFileSystem">The file system for the replica directory.</param>
		/// <param name="bufferSize">Size of the buffer used for reading and comparing files.</param>
		public Synchronizer(IFileSystem sourceFileSystem, IFileSystem replicaFileSystem, int bufferSize = 1024 * 1024) {
			_fsSource = sourceFileSystem;
			_fsReplica = replicaFileSystem;
			_buffer = new byte[bufferSize];
			_buffer2 = new byte[bufferSize];
			_bufferSize = bufferSize;
		}

		/// <summary>
		/// Sets up synchronizer to sync folder every n seconds.
		/// </summary>
		/// <param name="pathToFolder">Path to the source folder.</param>
		/// <param name="pathToReplica">Path to the replica folder.</param>
		/// <param name="intervalInSeconds">How many seconds shoudl be between synchronizations.</param>
		/// <param name="logger">Logger instance for logging of folder changes.</param>
		public void SynchronizePeriodically(string pathToFolder, string pathToReplica, long intervalInSeconds, ILoggingService logger) {
			_logger = logger;
			if (_timer == null) {
				_logger.Log($"Starting synchronization of {pathToFolder} to {pathToReplica} every {intervalInSeconds}.");
				_timer = new Timer(_ => Synchronize(pathToFolder, pathToReplica, logger), null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalInSeconds));
				_logger = logger;
			} else {
				throw new ArgumentException("Trying to synchronize folders when one synchronization is already running.");
			}
		}

		/// <summary>
		/// Synchronizes the source folder with the replica one time.
		/// </summary>
		public void Synchronize(string pathToFolder, string pathToReplica, ILoggingService logger) {
			_logger = logger;
			_logger.Log($"Starting synchronization of {pathToFolder} to {pathToReplica}.");

			if (!_fsSource.Directory.Exists(pathToFolder)) {
				throw new DirectoryNotFoundException("The directory to synchronize was not found.");
			}

			SyncFolder(pathToFolder, pathToReplica);
		}

		/// <summary>
		/// Stops the periodic synchronization.
		/// </summary>
		public void StopSynchronizePeriodically() {
			Dispose();
		}

		public void Dispose() {
			_timer?.Dispose();
			_logger?.Dispose();
		}

		/// <summary>
		/// Synchronizes the contents of a folder and its subfolders.
		/// </summary>
		private void SyncFolder(string pathToFolder, string pathToReplica) {
			if (!_fsReplica.Directory.Exists(pathToReplica)) {
				_logger.Log($"Creating directory {pathToReplica}.");
				try {
					_fsReplica.Directory.CreateDirectory(pathToReplica);
				} catch (Exception e) {
					_logger.LogError($"Failed to create directory {pathToReplica}, skipping syncing folder.", e);
					return; // doesn't need to throw exception (skipping this is fine + forwarding the info). But don't continue syncing this folder as that is not possible
				}
			}
			SyncFiles(pathToFolder, pathToReplica);

			HashSet<string> orgFoldersRel = _fsSource.Directory.GetDirectories(pathToFolder)
				.Select(path => Path.GetRelativePath(pathToFolder, path))
				.ToHashSet();
			HashSet<string> replicaFoldersRel = _fsReplica.Directory.GetDirectories(pathToReplica)
				.Select(path => Path.GetRelativePath(pathToReplica, path))
				.ToHashSet();

			// Sync all subdirectories
			foreach (var folderPathRel in orgFoldersRel) {
				string orgFolderPathAbs = Path.Combine(pathToFolder, folderPathRel);
				string replicaFolderPathAbs = Path.Combine(pathToReplica, folderPathRel);
				SyncFolder(orgFolderPathAbs, replicaFolderPathAbs);
			}

			// Remove directories in replica that don't exist in source
			var deletedFolders = replicaFoldersRel.Except(orgFoldersRel);
			foreach (var folderPathRel in deletedFolders) {
				string folderPathAbs = Path.Combine(pathToReplica, folderPathRel);
				_logger.Log($"Deleting directory {folderPathAbs}.");
				try {
					_fsReplica.Directory.Delete(folderPathAbs, true);
				} catch (Exception e) {
					_logger.LogError($"Failed to delete directory {folderPathAbs}.", e);
				}

			}
		}

		/// <summary>
		/// Synchronizes all files in a folder (not subfolders).
		/// </summary>
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
					// check if file equals
					bool filesEqual;
					try {
						filesEqual = AreFilesEqual(sourceFilePath, replicaFilePath);	// can throw exception
					} catch (Exception e) {
						_logger.LogError($"Failed to synchronize files {sourceFilePath} and {repFilePaths}.", e);
						continue;
					}

					if (filesEqual) {   // file wasn't changed since last sync -> nothing has to be done
						SetFileAttributes(sourceFilePath, replicaFilePath); // make sure that the attributes are updated
						continue;
					} else {    // file was changed since last sync -> update it
						_logger.Log($"Updating file {replicaFilePath}.");
						try {
							SyncFile(sourceFilePath, replicaFilePath);
						} catch (Exception e) {
							_logger.LogError($"Failed to update file {replicaFilePath}", e);
							continue;
						}
					}
				} else {    // completely new file
					_logger.Log($"Copying file {replicaFilePath} .");
					try {
						CopyFile(sourceFilePath, replicaFilePath);
					} catch (Exception e) {
						_logger.LogError($"Failed to copy file {replicaFilePath}", e);
						continue;
					}
				}
			}

			// Remove files in replica that don't exist in source
			var deletedFiles = repFilePaths.Except(orgFilePaths);
			foreach (string path in deletedFiles) {
				string pathAbs = Path.Combine(pathToReplica, path);
				_logger.Log($"Deleting file {pathAbs} .");
				try {
					_fsReplica.File.Delete(pathAbs);
				} catch (Exception e) {
					_logger.LogError($"Failed to delete file {pathAbs}", e);
				}
			}
		}

		/// <summary>
		/// Synchronizes a single file that was updated.
		/// </summary>
		private void SyncFile(string sourceFile, string replicaFile) {
			List<Chunk> sourceChunks = SplitFileIntoChunks(_fsSource, sourceFile);
			List<Chunk> replicaChunks = SplitFileIntoChunks(_fsReplica, replicaFile);
			sourceChunks.Add(Chunk.Empty);	// add empty chunk at the end to prevent exceptions due to indexing 0 at empty list
			replicaChunks.Add(Chunk.Empty);

			MyersDiff<Chunk> diff = new MyersDiff<Chunk>(replicaChunks.ToArray(), sourceChunks.ToArray());
			var edits = diff.GetEditScript();

			string tempFile;
			try {
				tempFile = GetTempFileName(replicaFile);
			} catch (Exception e) {
				_logger.LogError($"Failed to update file file {replicaFile}.", e);
				return;
			}


			try {
				using (var sourceStream = _fsSource.File.OpenRead(sourceFile))
				using (var replicaStream = _fsReplica.File.OpenRead(replicaFile))
				using (var tempStream = _fsReplica.File.OpenWrite(tempFile)) {
					long index = 0;

					foreach ((int indexReplicaChunk, int sourceIndexChunk, int removeLenghtChunk, int insertLenghtChunk) in edits) {
						// Calculate diff values relative from relative to chunks to relative to bytes 
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

						// Write bytes until the next edit
						while (index + _bufferSize <= indexReplica) {
							CopyStream(replicaStream, tempStream, _bufferSize);
							index += _bufferSize;
						}
						// Perform edits
						if (removeLenght > 0) { // Skip bytes that were removed
							replicaStream.Seek(removeLenght, SeekOrigin.Current);
							index += removeLenght;
						}
						if (insertLenght > 0) { // Insert new text from source
							sourceStream.Seek(sourceIndex, SeekOrigin.Begin);
							for (int i = 0; i < insertLenght / _bufferSize; i++) {
								CopyStream(sourceStream, tempStream, _bufferSize);
							}
							CopyStream(sourceStream, tempStream, (int)(insertLenght % _bufferSize));
						}
					}
					// Write the rest of file
					while (CopyStream(replicaStream, tempStream, _bufferSize) > 0) { }
				}
				SetFileAttributes(sourceFile, tempFile);
				_fsReplica.File.Replace(tempFile, replicaFile, null);
			} catch (Exception) {
				_fsReplica.File.Delete(tempFile);
				throw;
			}
		}

		/// <summary>
		/// Copies a block of bytes from one stream to another.
		/// </summary>
		private int CopyStream(FileSystemStream source, FileSystemStream dest, int len) {
			int bytesRead = source.Read(_buffer, 0, len);
			dest.Write(_buffer, 0, bytesRead);
			return bytesRead;
		}

		/// <summary>
		/// Fully copies one file.
		/// </summary>
		private void CopyFile(string sourceFile, string replicaFile) {
			try {
				using (var sourceStream = _fsSource.File.OpenRead(sourceFile))
				using (var tempStream = _fsReplica.File.OpenWrite(replicaFile)) {
					int bytesRead;
					while ((bytesRead = sourceStream.Read(_buffer, 0, _bufferSize)) > 0) {
						tempStream.Write(_buffer, 0, bytesRead);
					}
				}
				SetFileAttributes(sourceFile, replicaFile);
			} catch {
				_fsReplica.File.Delete(replicaFile);
			}
		}

		/// <summary>
		/// Copies file attributes and timestamps from source to replica.
		/// </summary>
		private void SetFileAttributes(string sourceFile, string replicaFile) {
			var attributes = _fsSource.File.GetAttributes(sourceFile);
			var lastUpdate = _fsSource.File.GetLastWriteTimeUtc(sourceFile);
			var created = _fsSource.File.GetCreationTimeUtc(sourceFile);

			_fsReplica.File.SetAttributes(replicaFile, attributes);
			_fsReplica.File.SetLastWriteTimeUtc(replicaFile, lastUpdate);
			_fsReplica.File.SetCreationTimeUtc(replicaFile, created);
		}

		/// <summary>
		/// Generates a temporary file name for a replica file.
		/// </summary>
		private string GetTempFileName(string filePath) {
			for (int i = 0; i < 5; i++) {   // try 5 times to find a name that is not already used in the folder
				string res = $"{filePath}_{Guid.NewGuid()}";
				if (!_fsReplica.File.Exists(res)) {
					return res;
				}
			}
			throw new ArgumentException($"Failed to generate name for temporary file when copying {filePath}.");
		}

		/// <summary>
		/// Splits a file into hash chunks for comparison.
		/// </summary>
		private List<Chunk> SplitFileIntoChunks(IFileSystem fs, string pathToFile) {
			List<Chunk> chunks = new List<Chunk>();

			int index = 0;
			using (var stream = fs.File.OpenRead(pathToFile)) {
				SHA256 sha256 = SHA256.Create();
				int bytesRead;

				while ((bytesRead = stream.Read(_buffer, 0, _bufferSize)) > 0) {
					byte[] hash = sha256.ComputeHash(_buffer, 0, bytesRead);
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

		/// <summary>
		/// Checks whether two files are equal.
		/// </summary>
		private bool AreFilesEqual(string sourceFile, string replicaFile) {
			if (!_fsSource.File.Exists(sourceFile) || !_fsReplica.File.Exists(replicaFile) ||
				_fsSource.FileInfo.New(sourceFile).Length != _fsReplica.FileInfo.New(replicaFile).Length) {
				return false;
			}

			using (var stream1 = _fsSource.File.OpenRead(sourceFile))
			using (var stream2 = _fsReplica.File.OpenRead(replicaFile)) {
				SHA256 sha256 = SHA256.Create();
				int bytesRead;
				while ((bytesRead = stream1.Read(_buffer, 0, _bufferSize)) > 0) {
					stream2.Read(_buffer2, 0, _bufferSize);
					var hash1 = sha256.ComputeHash(_buffer, 0, bytesRead);
					var hash2 = sha256.ComputeHash(_buffer2, 0, bytesRead);
					if (!hash1.SequenceEqual(hash2)) {
						return false;
					}
				}
			}
			return true;
		}
	}
}
