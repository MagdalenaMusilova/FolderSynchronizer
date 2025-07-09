using FolderSynchronizer;
using FolderSynchronizerTests.HelperClasses;
using System.IO.Abstractions;

namespace FolderSynchronizerTests;

public class InputParametersTests : SynchronizerTests
{
	string baseFolderPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(InputParametersTests));
	string baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(InputParametersTests) + "_Replica");

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
	public void Synchronize_NonexistentFolder_ThrowException() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		Assert.Catch(() => synchronizer.Synchronize(folderPath, replicaPath, logger), "Syncing folder that doesn't exist doesn't throw an exception.");
		Assert.That(!fs.Directory.Exists(replicaPath), "Replica folder was created even thought the synchronization failed.");
	}

	[Test]
	public void Synchronize_EmptyStringSourceFolder_ThrowException() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		FolderSynchronizer.Synchronizer synchronizer = new FolderSynchronizer.Synchronizer(fs, fs);

		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		Assert.Catch(() => synchronizer.Synchronize(String.Empty, replicaPath, logger), "Syncing folder that doesn't exist doesn't throw an exception.");
		Assert.That(!fs.Directory.Exists(replicaPath), "Replica folder was created even thought the synchronization failed.");
	}

	[Test]
	public void Synchronize_EmptyStringReplicaFolder_ThrowException() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		FolderSynchronizer.Synchronizer synchronizer = new FolderSynchronizer.Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		Assert.Catch(() => synchronizer.Synchronize(folderPath, String.Empty, logger), "Syncing folder that doesn't exist doesn't throw an exception.");
	}
}
