using FolderSynchronizer;
using System.Collections;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;

namespace FolderSynchronizerTests;

public class FolderSynchronizerTest
{
	readonly static string _baseSourcePath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(FileScannerTest));
	readonly static string _baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(FileScannerTest) + "_Replica");
	private IFileSystem _fs = new FileSystem();
	private IFileSystem _mockFs = new MockFileSystem();
	private Synchronizer _synchronizer;
	private Synchronizer _mockSynchronizer;
	int _fileIndex = 0;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		_synchronizer = new Synchronizer(_fs);
		_mockSynchronizer = new Synchronizer(_mockFs);

		if (_fs.Directory.Exists(_baseSourcePath)) {
			_fs.Directory.Delete(_baseSourcePath, true);
		}
		if (_fs.Directory.Exists(_baseReplicaPath)) {
			_fs.Directory.Delete(_baseReplicaPath, true);
		}
		_fs.Directory.CreateDirectory(_baseSourcePath);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		if (_fs.Directory.Exists(_baseSourcePath)) {
			_fs.Directory.Delete(_baseSourcePath, true);
		}
		if (_fs.Directory.Exists(_baseReplicaPath)) {
			_fs.Directory.Delete(_baseReplicaPath, true);
		}
	}

	private string CreateFile(string folderPath, int size) {
		if (!_fs.Directory.Exists(folderPath)) {
			_fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString());

		_fs.File.WriteAllBytes(filePath, new byte[size]);
		return filePath;
	}

	private string CreateFile(string folderPath, string content) {
		if (!_fs.Directory.Exists(folderPath)) {
			_fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString() + ".txt");

		_fs.File.WriteAllText(filePath, content);
		return filePath;
	}

	public bool FilesEqual(string filePath1, string filePath2) {
		using (var sha256 = SHA256.Create()) {
			using (var stream1 = File.OpenRead(filePath1))
			using (var stream2 = File.OpenRead(filePath2)) {
				var hash1 = sha256.ComputeHash(stream1);
				var hash2 = sha256.ComputeHash(stream2);

				return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
			}
		}
	}



	[Test]
    public void Synchronize_OneFileFirstTime_Pass()
    {
		string folderPath = Path.Combine(_baseSourcePath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(_baseReplicaPath, TestContext.CurrentContext.Test.Name);
		// create source folder
		string filePath = CreateFile(folderPath, 1024);
		// synchronize
		_synchronizer.Synchronize(folderPath, replicaPath);
		// assert results
		string filePathReplica = Path.Combine(replicaPath,Path.GetRelativePath(folderPath, filePath));
		Assert.That(FilesEqual(filePath, filePathReplica), "Synchronized file is not the same as the original.");
	}

	[Test]
	public void Synchronize_OneFileMultipleChanges_Pass() {
		string folderPath = Path.Combine(_baseSourcePath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(_baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content1 = "Melt the lard or butter/oil in a Dutch oven or other heavy soup pot over medium high heat and cook the onions until beginning to brown, about 7-10 minutes. Add the beef and cook until the beef is just starting to brown, 7-10 minutes.";
		string content2 = "Add the bell peppers, tomatoes, and garlic and cook for another 6-8 minutes.  (Note about peppers:  Outside of Hungary it’s very difficult to find the peppers they use there.  The best ones to use in their place are red and some yellow/orange.  Avoid regular green bell peppers as they have a starkly different flavor profile.)";

		// create source folder and sync
		string filePath = CreateFile(folderPath, content1);
		_synchronizer.Synchronize(folderPath, replicaPath);
		// edit folder and sync
		_fs.File.WriteAllText(filePath, content2);
		_synchronizer.Synchronize(folderPath, replicaPath);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		Assert.That(FilesEqual(filePath, filePathReplica), "Synchronized file is not the same as the original.");
	}
}
