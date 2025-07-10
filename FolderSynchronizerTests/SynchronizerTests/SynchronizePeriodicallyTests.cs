using FolderSynchronizer;
using FolderSynchronizerTests.HelperClasses;
using System.IO.Abstractions;

namespace FolderSynchronizerTests;

public class SynchronizePeriodicallyTests : SynchronizerTests
{
	string baseFolderPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(SynchronizePeriodicallyTests));
	string baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(SynchronizePeriodicallyTests) + "_Replica");

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
	public async Task SynchronizePeriodically_OneFileMultipleChanges_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[0];
		string content2 = gulashRecipe[1];

		// create source folder and start sync
		string filePath = FileCreator.CreateFile(fs, folderPath, content1);
		synchronizer.SynchronizePeriodically(folderPath, replicaPath, 3, logger);
		// edit folder and wait for sync
		fs.File.WriteAllText(filePath, content2);
		await Task.Delay(7000);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content2, "Synchronized file is not the same as the original.");
		// cleanup
		Directory.Delete(folderPath, true);
		Directory.Delete(replicaPath, true);
	}

	[Test]
	public async Task SynchronizePeriodically_NestedFoldersCreateDeleteEdit_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string subfolderPath1 = Path.Combine(folderPath, "sub1");
		string subfolderPath2 = Path.Combine(folderPath, "sub2");
		string subsubfolderPath = Path.Combine(subfolderPath1, "subsub");
		string subsubsubsubfolderPath = Path.Combine(folderPath, "sub3", "subsub", "subsubsub", "subsubsubsub");
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath1 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]);
		string filePath2 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[1] + gulashRecipe[1]);
		string filePath3 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[6]);
		string filePath4 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[3]);
		string filePath5 = FileCreator.CreateFile(fs, subsubfolderPath, gulashRecipe[4]);
		string filePath6 = FileCreator.CreateFile(fs, subsubfolderPath, 0);
		synchronizer.SynchronizePeriodically(folderPath, replicaPath, 5, logger);

		// update folder
		await Task.Delay(200);
		fs.File.WriteAllText(filePath1, gulashRecipe[0] + gulashRecipe[1]);
		fs.File.WriteAllText(filePath5, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[5]);
		fs.File.WriteAllText(filePath6, gulashRecipe[1] + gulashRecipe[5] + gulashRecipe[7]);
		string filePath7 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[0]);
		string filePath8 = FileCreator.CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[1]);
		string filePath9 = FileCreator.CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[2]);
		string filePath10 = FileCreator.CreateFile(fs, folderPath, gulashRecipe[3]);
		fs.File.Delete(filePath2);
		fs.File.Delete(filePath3);
		fs.File.Delete(filePath4);

		// assert results
		await Task.Delay(6000);
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");

		// cleanup
		Directory.Delete(folderPath, true);
		Directory.Delete(replicaPath, true);
	}
}
