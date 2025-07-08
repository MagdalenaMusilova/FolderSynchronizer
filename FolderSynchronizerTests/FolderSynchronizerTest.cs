using FolderSynchronizer;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FolderSynchronizerTests;

public class FolderSynchronizerTest
{
	int _fileIndex = 0;


	private string CreateFile(IFileSystem fs, string folderPath, int size) {
		if (!fs.Directory.Exists(folderPath)) {
			fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString());

		fs.File.WriteAllBytes(filePath, new byte[size]);
		return filePath;
	}

	private string CreateFile(IFileSystem fs, string folderPath, string content) {
		if (!fs.Directory.Exists(folderPath)) {
			fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString() + ".txt");

		fs.File.WriteAllText(filePath, content);
		return filePath;
	}



	[Test]
    public void Synchronize_OneFileFirstTime_Pass()
    {
		IFileSystem fs = new MockFileSystem();
		MockLogginService logger = new MockLogginService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine("C:", "Source", TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine("C:", "Replica", TestContext.CurrentContext.Test.Name);

		string content = "Melt the lard or butter/oil in a Dutch oven or other heavy soup pot over medium high heat and cook the onions until beginning to brown, about 7-10 minutes. Add the beef and cook until the beef is just starting to brown, 7-10 minutes.";

		// create source folder
		string filePath = CreateFile(fs, folderPath, content);
		// synchronize
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		// assert results
		string filePathReplica = Path.Combine(replicaPath,Path.GetRelativePath(folderPath, filePath));
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content, "Synchronized file is not the same as the original.");
	}

	[Test]
	public void Synchronize_OneFileMultipleChanges_Pass() {
		IFileSystem fs = new MockFileSystem();
		MockLogginService logger = new MockLogginService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine("C:", "Source", TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine("C:", "Replica", TestContext.CurrentContext.Test.Name);
		string content1 = "Melt the lard or butter/oil in a Dutch oven or other heavy soup pot over medium high heat and cook the onions until beginning to brown, about 7-10 minutes. Add the beef and cook until the beef is just starting to brown, 7-10 minutes.";
		string content2 = "Add the bell peppers, tomatoes, and garlic and cook for another 6-8 minutes.  (Note about peppers:  Outside of Hungary it’s very difficult to find the peppers they use there.  The best ones to use in their place are red and some yellow/orange.  Avoid regular green bell peppers as they have a starkly different flavor profile.)";

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

	[Test]
	public async Task SynchronizePeriodically_OneFileMultipleChanges_Pass() {
		IFileSystem fs = new MockFileSystem();
		MockLogginService logger = new MockLogginService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine("C:", "Source", TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine("C:", "Replica", TestContext.CurrentContext.Test.Name);
		string content1 = "Melt the lard or butter/oil in a Dutch oven or other heavy soup pot over medium high heat and cook the onions until beginning to brown, about 7-10 minutes. Add the beef and cook until the beef is just starting to brown, 7-10 minutes.";
		string content2 = "Add the bell peppers, tomatoes, and garlic and cook for another 6-8 minutes.  (Note about peppers:  Outside of Hungary it’s very difficult to find the peppers they use there.  The best ones to use in their place are red and some yellow/orange.  Avoid regular green bell peppers as they have a starkly different flavor profile.)";

		// create source folder and start sync
		string filePath = CreateFile(fs, folderPath, content1);
		synchronizer.SynchronizePeriodically(folderPath, replicaPath, 3, logger);
		// edit folder and wait for sync
		fs.File.WriteAllText(filePath, content2);
		await Task.Delay(700);
		// assert results
		string filePathReplica = Path.Combine(replicaPath, Path.GetRelativePath(folderPath, filePath));
		string replicaContent = fs.File.ReadAllText(filePathReplica);
		Assert.That(replicaContent == content2, "Synchronized file is not the same as the original.");
	}
}
