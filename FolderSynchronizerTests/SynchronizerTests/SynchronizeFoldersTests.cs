using FolderSynchronizer;
using FolderSynchronizerTests.HelperClasses;
using System.IO.Abstractions;

namespace FolderSynchronizerTests;

public class SynchronizeFoldersTests : SynchronizerTests
{
	string baseFolderPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(SynchronizeFoldersTests));
	string baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(SynchronizeFoldersTests) + "_Replica");

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
	public void Synchronize_OneFileOneSync_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content = gulashRecipe[0];

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, content);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		Assert.That(fs.File.Exists(filePathReplica), "The replica file wasn't created.");
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content, "Synchronized file is not the same as the original.");
	}

	[Test]
	public void Synchronize_DeleteFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content = gulashRecipe[0];

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, content);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.Delete(filePath);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_CreateFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content = gulashRecipe[0];

		// create source folder and sync
		fs.Directory.CreateDirectory(folderPath);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, content);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_UpdateFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[0];
		string content2 = gulashRecipe[1];

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, content1);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, content2);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_UpdateFileRemoveStart_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileRemoveEnd_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[0]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileRemoveMiddle_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[0] + gulashRecipe[2]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileMultipleChanges_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2] + gulashRecipe[3] + gulashRecipe[4] + gulashRecipe[5] + gulashRecipe[6] + gulashRecipe[7] + gulashRecipe[8]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[8] + gulashRecipe[1] + gulashRecipe[6] + gulashRecipe[4] + gulashRecipe[7] + gulashRecipe[7] + gulashRecipe[7] + gulashRecipe[8]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_EmptyFolder_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		fs.Directory.CreateDirectory(folderPath);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		Assert.That(fs.Directory.GetFiles(replicaPath).Length == 0, "The synchronized folder isn't the same as the original folder.");
	}

	[Test]
	public void Synchronize_EmptyFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, 0);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_NestedFolders_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs, 16);

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
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
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
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_RenameFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.Move(filePath, filePath + "_Copy");
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_DeleteFolder_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string subfolderPath1 = Path.Combine(folderPath, "sub1");
		string subfolderPath2 = Path.Combine(folderPath, "sub2");
		string subsubfolderPath = Path.Combine(subfolderPath1, "subsub");
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath1 = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0]);
		string filePath2 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[1]);
		string filePath3 = FileCreator.CreateFile(fs, subsubfolderPath, gulashRecipe[2]);
		string filePath4 = FileCreator.CreateFile(fs, subsubfolderPath, gulashRecipe[3]);
		string filePath5 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[4]);
		string filePath6 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[5]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.Directory.Delete(subfolderPath1, true);
		string filePath7 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[6]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_CreateDeleteFolder_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string subfolderPath1 = Path.Combine(folderPath, "sub1");
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath1 = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0]);
		string filePath2 = FileCreator.CreateFile(fs, subfolderPath1, gulashRecipe[1]);

		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		string subfolderPath2 = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name, "sub2");
		string subfolderPath3 = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name, "sub3");
		string subsubfolderPath = Path.Combine(subfolderPath1, TestContext.CurrentContext.Test.Name, "subsub");
		string sub5folderPath = Path.Combine(subfolderPath1, TestContext.CurrentContext.Test.Name, "subsub2", "subsub3", "subsub4", "subsub5");

		fs.Directory.Delete(subfolderPath1, true);
		string filePath3 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[2]);
		string filePath4 = FileCreator.CreateFile(fs, subfolderPath2, gulashRecipe[3]);
		fs.Directory.CreateDirectory(subfolderPath3);
		string filePath5 = FileCreator.CreateFile(fs, sub5folderPath, gulashRecipe[4]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_PreexistingReplica_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string subFolderPath1 = Path.Combine(folderPath, "sub1");
		string subFolderPath2 = Path.Combine(folderPath, "sub2");
		string subsubsubFolderPath = Path.Combine(subFolderPath1, "subsub", "subsubsub");
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string subReplicaPath = Path.Combine(replicaPath, "sub");
		string subsubReplicaPath = Path.Combine(replicaPath, "sub", "subsub");

		// create source folder
		FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		FileCreator.CreateFile(fs, subFolderPath1, gulashRecipe[2] + gulashRecipe[3] + gulashRecipe[4]);
		FileCreator.CreateFile(fs, subFolderPath1, gulashRecipe[0] + gulashRecipe[5] + gulashRecipe[7]);
		FileCreator.CreateFile(fs, subFolderPath2, gulashRecipe[4]);
		FileCreator.CreateFile(fs, subsubsubFolderPath, gulashRecipe[3] + gulashRecipe[5] + gulashRecipe[8]);

		// create replica
		FileCreator.CreateFile(fs, replicaPath, gulashRecipe[7] + gulashRecipe[8]);
		FileCreator.CreateFile(fs, replicaPath, gulashRecipe[1] + gulashRecipe[2] + gulashRecipe[3]);
		FileCreator.CreateFile(fs, subReplicaPath, gulashRecipe[8] + gulashRecipe[1] + gulashRecipe[7]);
		FileCreator.CreateFile(fs, subsubReplicaPath, gulashRecipe[0]);
		FileCreator.CreateFile(fs, subsubReplicaPath, gulashRecipe[3] + gulashRecipe[5]);

		// sync
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		FolderDifference fd = FolderDifference.CompareFolders(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}
}
