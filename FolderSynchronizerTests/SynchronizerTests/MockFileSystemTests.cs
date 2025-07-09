using FolderSynchronizerTests.HelperClasses;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FolderSynchronizerTests;

public class MockFileSystemTests : SynchronizerTests
{
	string baseFolderPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(MockFileSystemTests));
	string baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(MockFileSystemTests) + "_Replica");

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		if (Directory.Exists(baseFolderPath)) {
			Directory.Delete(baseFolderPath, true);
		}
		if (Directory.Exists(baseReplicaPath)) {
			Directory.Delete(baseReplicaPath, true);
		}
		Directory.CreateDirectory(baseFolderPath);
		Directory.CreateDirectory(baseReplicaPath);
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		if (Directory.Exists(baseFolderPath)) {
			Directory.Delete(baseFolderPath, true);
		}
		if (Directory.Exists(baseReplicaPath)) {
			Directory.Delete(baseReplicaPath, true);
		}
	}

	[Test]
	public void Synchronize_MockOneFileFirstTime_Pass() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		FolderSynchronizer.Synchronizer synchronizer = new FolderSynchronizer.Synchronizer(fs, fs);

		string folderPath = Path.Combine("C:", "Source", TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine("C:", "Replica", TestContext.CurrentContext.Test.Name);

		string content = gulashRecipe[0];

		// create source folder
		string filePath = CreateFile(fs, folderPath, content);
		// synchronize
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content, "Synchronized file is not the same as the original.");
	}

	[Test]
	public void Synchronize_MockOneFileMultipleChanges_Pass() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		FolderSynchronizer.Synchronizer synchronizer = new FolderSynchronizer.Synchronizer(fs, fs);

		string folderPath = Path.Combine("C:", "Source", TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine("C:", "Replica", TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[0];
		string content2 = gulashRecipe[1];

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, content1);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// edit folder and sync
		fs.File.WriteAllText(filePath, content2);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content2, "Synchronized file is not the same as the original.");
	}
}

