using FolderSynchronizer;
using FolderSynchronizerTests.HelperClasses;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FolderSynchronizerTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class LoggingServiceTest
{
	List<string> gulashRecipe = new List<string>() {
		"Peel the onion and chop it roughly. Cut the beef into 1-1.½-inch pieces.",
		"Melt the lard or vegetable oil over medium-high heat in a pot with a thick bottom (I use a heavy iron cast Dutch oven). Fry onions until almost brown. Stir frequently to avoid burning. Finally, add the crushed caraway seeds and stir for another minute.",
		"Add beef chunks, season with salt, and fry them from all sides until a golden brown crust is created. Expect the meat releases some juices when fried. Stir frequently, and scrape off the burnt residue on the bottom of the pot with a wooden spatula. If necessary, reduce the heat or add a spoon or two of water.",
		"Turn the stove on medium heat, and add ground pepper, sweet paprika, and tomato paste. Fry for 1 minute while stirring. The base can’t get burnt, or else the goulash will taste bitter. Add 1-2 spoons of water to prevent burning.",
		"Pour in water so the meat is almost covered. Add bay leaves. Bring to a boil, reduce the heat to a minimum, cover with a lid and allow the beef to simmer for 2–2.5 hours or until soft.",
		"Check the goulash from time to time. Add some water if the level of liquid in the pot falls. When the gulas contains too much water, cook it uncovered at the end. The liquid will be reduced, and the gulas will gain a richer flavor and a nice red color. Stir occasionally.",
		"Once the beef cubes are tender, add a little flour to thicken the stew. This will help create a smoother, more cohesive sauce.",
		"Take off the pot's lid. Using a small sieve, carefully dust the surface of the stew with a tablespoon of flour. Do not stir. Cover with the lid and leave to cook for a further 15 minutes. Remove the lid and stir. The sauce will be just thick enough!",
		"Remove bay leaves, add crushed garlic, dried marjoram, and stir. Season with salt to your liking. Cover with a lid and let it rest off heat for 10 minutes.",
		"Serve the goulash in a deep bowl with a piece of bread or warm slices of Czech dumplings (an iconic side dish!) arranged on the side of a plate. Top the dish with a few raw onion circles and sprinkle some green parsley for the final touch."
		};
	string baseFolderPath = Path.Combine("C:", "Source");
	string baseReplicaPath = Path.Combine("C:", "Replica");

	private bool AllFilesMentionedInLogs(MockLoggingService loggingService, List<string> files, string basePath, out List<string> notMentionedFiles) {
		notMentionedFiles = new List<string>();
		foreach (string file in files) {
			if (!FileMentionedInLogs(loggingService, file, basePath)) {
				notMentionedFiles.Add(file);
			}
		}
		return notMentionedFiles.Count == 0;
	}

	private bool FileMentionedInLogs(MockLoggingService loggingService, string file, string basePath) {
		return loggingService.logs.Any(l => l.Contains(Path.GetRelativePath(basePath, file)));
	}

	private bool FileMentionedInErrors(MockLoggingService loggingService, string file, string basePath) {
		return loggingService.errorLogs.Any(l => l.message.Contains(Path.GetRelativePath(basePath, file)));
	}

	[Test]
	public void LogFileCreation() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, 8);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		Assert.That(FileMentionedInLogs(logger, filePath, folderPath), "The logs don't contain information about a file being created.");
	}

	[Test]
	public void LogFileDeletion() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, 8);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		logger.Clear();
		Assert.That(logger.logs.Count == 0, "MockLoggingService contains logs even thought it was cleared.");
		// update source folder and sync
		fs.File.Delete(filePath);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		Assert.That(FileMentionedInLogs(logger, filePath, folderPath), "The logs don't contain information about a file being deleted.");
	}

	[Test]
	public void LogFileUpdate() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = FileCreator.CreateFile(fs, folderPath, 8);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		logger.Clear();
		Assert.That(logger.logs.Count == 0, "MockLoggingService contains logs even thought it was cleared.");
		// update source folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[0]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		Assert.That(FileMentionedInLogs(logger, filePath, folderPath), "The logs don't contain information about a file being updated.");
	}

	[Test]
	public void LogFileCreationDeletionUpdate() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		List<string> filePaths = new List<string>();
		filePaths.Add(FileCreator.CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]));
		filePaths.Add(FileCreator.CreateFile(fs, folderPath, gulashRecipe[4]));
		filePaths.Add(FileCreator.CreateFile(fs, folderPath, gulashRecipe[1] + gulashRecipe[1]));
		filePaths.Add(FileCreator.CreateFile(fs, folderPath, gulashRecipe[2] + gulashRecipe[1] + gulashRecipe[2]));
		filePaths.Add(FileCreator.CreateFile(fs, folderPath, gulashRecipe[2] + gulashRecipe[1]));
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		List<string> notMentioned = new List<string>();
		Assert.That(AllFilesMentionedInLogs(logger, filePaths, folderPath, out notMentioned), $"The creation of the following files isn't in the logs: {String.Join(", ", notMentioned)}");
		logger.Clear();
		Assert.That(logger.logs.Count == 0, "MockLoggingService contains logs even thought it was cleared.");

		// update source folder and sync
		//create new files
		List<string> createdFiles = new List<string>();
		string filePath1 = FileCreator.CreateFile(fs, folderPath, gulashRecipe[2] + gulashRecipe[1]);
		filePaths.Add(filePath1);
		createdFiles.Add(filePath1);
		string filePath2 = FileCreator.CreateFile(fs, folderPath, gulashRecipe[2] + gulashRecipe[1]);
		filePaths.Add(filePath2);
		createdFiles.Add(filePath2);
		//update files
		List<string> updatedFiles = new List<string>();
		updatedFiles.Add(filePaths[0]);
		updatedFiles.Add(filePaths[3]);
		fs.File.WriteAllText(updatedFiles[0], gulashRecipe[1] + gulashRecipe[2]);
		fs.File.WriteAllText(updatedFiles[1], gulashRecipe[5]);
		//delete files
		List<string> deletedFiles = new List<string>();
		deletedFiles.Add(filePaths[1]);
		deletedFiles.Add(filePaths[4]);
		fs.File.Delete(deletedFiles[0]);
		fs.File.Delete(deletedFiles[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		Assert.That(updatedFiles.Intersect(deletedFiles).Count() == 0, "Test in invalid. One of the updated files was deleted.");
		Assert.That(createdFiles.Intersect(updatedFiles).Count() == 0, "Test in invalid. File that was mark as newly created was then mark as updated.");
		Assert.That(createdFiles.Intersect(deletedFiles).Count() == 0, "Test in invalid. One of the newly created files was deleted.");

		List<string> createdNotMentioned = new List<string>();
		Assert.That(AllFilesMentionedInLogs(logger, createdFiles, folderPath, out createdNotMentioned), $"The creation of the following files isn't in the logs: {String.Join(", ", createdNotMentioned)}");
		List<string> updatedNotMentioned = new List<string>();
		Assert.That(AllFilesMentionedInLogs(logger, updatedFiles, folderPath, out updatedNotMentioned), $"The update of the following files isn't in the logs: {String.Join(", ", updatedNotMentioned)}");
		List<string> deletedNotMentioned = new List<string>();
		Assert.That(AllFilesMentionedInLogs(logger, deletedFiles, folderPath, out deletedNotMentioned), $"The deletion of the following files isn't in the logs: {String.Join(", ", deletedNotMentioned)}");
		Assert.That(logger.errorLogs.Count == 0, $"Logger contains error even thought there should be any. Error messages: {String.Join(" | ", logger.errorLogs)}");
	}

	[Test]
	public void LogError() {
		var fsMock = new Mock<FileSystem>() { CallBase = true };
		var fs = fsMock.Object;
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[2];
		string content2 = gulashRecipe[3];

		// create source
		string failDelete = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0]);
		string failUpdate = FileCreator.CreateFile(fs, folderPath, content1);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		Assert.That(logger.errorLogs.Count == 0, $"Logger contains error even thought there should be any after the initial synchronization. Error messages: {String.Join(" | ", logger.errorLogs)}");
		Assert.That(fs.File.Exists(failDelete), $"Test in invalid. Failed to create file {failDelete}.");
		Assert.That(fs.File.Exists(failUpdate), $"Test in invalid. Failed to create file {failUpdate}.");

		// update folder
		string failCreate = FileCreator.CreateFile(fs, folderPath, gulashRecipe[0]);
		fs.File.WriteAllText(failUpdate, content2);
		fs.File.Delete(failDelete);

		// set failures
		fsMock.Setup(f => f.File.OpenWrite(
			It.Is<string>(p => p.StartsWith(replicaPath))))
			.Throws(new IOException());
		fsMock.Setup(f => f.File.Copy(
			It.IsAny<string>(),
			It.Is<string>(p => p.StartsWith(replicaPath))))
			.Throws(new IOException());
		fsMock.Setup(f => f.File.Delete(
			It.Is<string>(p => p.StartsWith(replicaPath))))
			.Throws(new IOException());

		// test
		try {
			synchronizer.Synchronize(folderPath, replicaPath, logger);
		} catch (Exception) {
			Assert.Fail("Failure to synchronize file shouldn't throw an exception, only write error to logs");
		}

		// assert results
		Assert.That(FileMentionedInErrors(logger, failDelete, folderPath), "The logs don't contain information that file failed to be deleted.");
		Assert.That(FileMentionedInErrors(logger, failUpdate, folderPath), "The logs don't contain information that file failed to be updated.");
		Assert.That(FileMentionedInErrors(logger, failCreate, folderPath), "The logs don't contain information that file failed to be created.");
	}
}
