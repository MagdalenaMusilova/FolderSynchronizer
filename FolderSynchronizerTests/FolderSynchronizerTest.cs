using FolderSynchronizer;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FolderSynchronizerTests;

public class FolderSynchronizerTest
{
	[TestFixture]
	[Parallelizable(ParallelScope.All)]
	class FolderDifference
	{
		public List<string> missingFiles = new List<string>();
		public List<string> abundantFiles = new List<string>();
		public List<string> differentFiles = new List<string>();
		public List<string> missingFolders = new List<string>();
		public List<string> abundantFolders = new List<string>();

		public bool AreFoldersEqual() {
			return missingFiles.Count != 0 || abundantFiles.Count != 0 || differentFiles.Count != 0 ||
				missingFolders.Count != 0 || abundantFolders.Count != 0;
		}

		public string DifferencesToString() {
			string res = "";
			if (missingFiles.Count != 0) {
				res += "Missing files: " + String.Join(", ", missingFiles) + "; ";
			}
			if (abundantFiles.Count != 0) {
				res += "File that shouldn't be in replica: " + String.Join(", ", abundantFiles) + "; ";
			}
			if (differentFiles.Count != 0) {
				res += "Files with different content: " + String.Join(", ", differentFiles) + "; ";
			}
			if (missingFolders.Count != 0) {
				res += "Missing folders: " + String.Join(", ", missingFolders) + "; ";
			}
			if (abundantFolders.Count != 0) {
				res += "Folders that shouldn't be in replica: " + String.Join(", ", abundantFolders) + "; ";
			}
			return res;
		}
	}

	int _fileIndex = 0;
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

	string baseFolderPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(FolderSynchronizerTest));
	string baseReplicaPath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(FolderSynchronizerTest) + "_Replica");
	string baseFolderPathAsync = Path.Combine(Environment.CurrentDirectory, "Test_Asnyc", nameof(FolderSynchronizerTest));
	string baseReplicaPathAsync = Path.Combine(Environment.CurrentDirectory, "Test_Asnyc", nameof(FolderSynchronizerTest) + "_Replica");

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		if (Directory.Exists(baseFolderPath)) { 
			Directory.Delete(baseFolderPath, true);
		}
		if (Directory.Exists(baseReplicaPath)) {
			Directory.Delete(baseReplicaPath, true);
		}
		if (Directory.Exists(baseFolderPathAsync)) {
			Directory.Delete(baseFolderPathAsync, true);
		}
		if (Directory.Exists(baseReplicaPathAsync)) {
			Directory.Delete(baseReplicaPathAsync, true);
		}
		Directory.CreateDirectory(baseFolderPath);
		Directory.CreateDirectory(baseReplicaPath);
		Directory.CreateDirectory(baseFolderPathAsync);
		Directory.CreateDirectory(baseReplicaPathAsync);
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


	private string CreateFile(IFileSystem fs, string folderPath, Int64 size) {
		if (size > short.MaxValue) {
			return CreateBigFile(fs, folderPath, size);
		}

		if (!fs.Directory.Exists(folderPath)) {
			fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString());

		fs.File.WriteAllBytes(filePath, new byte[size]);
		return filePath;
	}

	private string CreateBigFile(IFileSystem fs, string folderPath, Int64 size) {
		if (!fs.Directory.Exists(folderPath)) {
			fs.Directory.CreateDirectory(folderPath);
		}
		string filePath = Path.Combine(folderPath, (++_fileIndex).ToString());

		byte[] buffer = new byte[short.MaxValue];
		for (int i = 0; i < size/ short.MaxValue; i++) {
			fs.File.WriteAllBytes(filePath, buffer);
		}
		fs.File.WriteAllBytes(filePath, new byte[size% short.MaxValue]);
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

	private FolderDifference GetFolderDifferences(IFileSystem fs, string sourceFolder, string replicaFolder) {
		FolderDifference fd = new FolderDifference();
		
		AddFolderDifferences(fs, sourceFolder, replicaFolder, ref fd);

		return fd;
	}

	private void AddFolderDifferences(IFileSystem fs, string sourceFolder, string replicaFolder, ref FolderDifference fd) {
		if (fs.Directory.Exists(sourceFolder) && fs.Directory.Exists(replicaFolder)) {
			fd.missingFolders.Add(replicaFolder);
		}

		var sourceFiles = fs.Directory.GetFiles(sourceFolder).Select(path => Path.GetRelativePath(baseFolderPath, path));
		var replicaFiles = fs.Directory.GetFiles(replicaFolder).Select(path => Path.GetRelativePath(replicaFolder, path));
		fd.missingFiles.AddRange(sourceFiles.Except(replicaFiles));
		fd.abundantFiles.AddRange(replicaFiles.Except(sourceFiles));

		string[] sharedFiles = sourceFiles.Intersect(replicaFiles).ToArray();
		foreach (var file in sharedFiles) {
			if (!SameFile(fs, Path.Combine(sourceFolder, file), Path.Combine(replicaFolder, file))){
				fd.differentFiles.Add(file);
			}
		}

		var sourceDirectories = fs.Directory.GetDirectories(sourceFolder).Select(path => Path.GetRelativePath(baseFolderPath, path));
		var replicaDirectories = fs.Directory.GetDirectories(replicaFolder).Select(path => Path.GetRelativePath(replicaFolder, path));
		fd.missingFolders.AddRange(sourceDirectories.Except(replicaDirectories));
		fd.abundantFiles.AddRange(replicaDirectories.Except(sourceDirectories));

		string[] sharedDirectories = sourceDirectories.Intersect(replicaDirectories).ToArray();
		foreach (var dir in sharedDirectories) {
			AddFolderDifferences(fs, Path.Combine(baseFolderPath, dir), Path.Combine(replicaFolder, dir), ref fd);
		}
	}

	private bool SameFile(IFileSystem fs, string sourceFile, string replicaFile) { 
		string sourceContent = fs.File.ReadAllText(sourceFile);
		string replicaContent = fs.File.ReadAllText(replicaFile);
		return sourceContent == replicaContent;
	}

	private void RemoveFilePermissions(MockFileSystem fs, string filePath, bool allowRead) {
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			throw new Exception("Can't be run on this operating system.");
		}
		FileSecurity fileSecurity = fs.File.GetAccessControl(filePath);
		WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
		FileSystemAccessRule rule = new FileSystemAccessRule(
			currentUser.User,
			allowRead ? FileSystemRights.Write : FileSystemRights.Read | FileSystemRights.Write,
			AccessControlType.Deny);
		fileSecurity.SetAccessRule(rule);
		fs.File.SetAccessControl(filePath, fileSecurity);
	}


	[Test]
	public void Synchronize_MockOneFileFirstTime_Pass() {
		IFileSystem fs = new MockFileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

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
		Synchronizer synchronizer = new Synchronizer(fs, fs);

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

	[Test]
	public void Synchronize_OneFileOneSync_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content = gulashRecipe[0];

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, content);
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
		string filePath = CreateFile(fs, folderPath, content);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.Delete(filePath);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
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
		string filePath = CreateFile(fs, folderPath, content);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_UpdateFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[0];
		string content2 = gulashRecipe[1];

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, content1);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, content2);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_UpdateFileRemoveStart_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileRemoveEnd_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[0]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileRemoveMiddle_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[0] + gulashRecipe[2]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_FileMultipleChanges_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath = CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2] + gulashRecipe[3] + gulashRecipe[4] + gulashRecipe[5] + gulashRecipe[6] + gulashRecipe[7] + gulashRecipe[8]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath, gulashRecipe[8] + gulashRecipe[1] + gulashRecipe[6] + gulashRecipe[4] + gulashRecipe[7] + gulashRecipe[7] + gulashRecipe[7] + gulashRecipe[8]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
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
		string filePath = CreateFile(fs, folderPath, 0);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_NestedFolders_Pass() {
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
		string filePath1 = CreateFile(fs, subfolderPath1, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]);
		string filePath2 = CreateFile(fs, subfolderPath1, gulashRecipe[1] + gulashRecipe[1]);
		string filePath3 = CreateFile(fs, subfolderPath1, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[6]);
		string filePath4 = CreateFile(fs, subfolderPath2, gulashRecipe[3]);
		string filePath5 = CreateFile(fs, subsubfolderPath, gulashRecipe[4]);
		string filePath6 = CreateFile(fs, subsubfolderPath, 0);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.WriteAllText(filePath1, gulashRecipe[0] + gulashRecipe[1]);
		fs.File.WriteAllText(filePath5, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[5]);
		fs.File.WriteAllText(filePath6, gulashRecipe[1] + gulashRecipe[5] + gulashRecipe[7]);
		string filePath7 = CreateFile(fs, subfolderPath1, gulashRecipe[0]);
		string filePath8 = CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[1]);
		string filePath9 = CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[2]);
		string filePath10 = CreateFile(fs, folderPath, gulashRecipe[3]);
		fs.File.Delete(filePath2);
		fs.File.Delete(filePath3);
		fs.File.Delete(filePath4);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
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
		string filePath = CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.File.Move(filePath, filePath + "_Copy");
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
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
		string filePath1 = CreateFile(fs, folderPath, gulashRecipe[0]);
		string filePath2 = CreateFile(fs, subfolderPath1, gulashRecipe[1]);
		string filePath3 = CreateFile(fs, subsubfolderPath, gulashRecipe[2]);
		string filePath4 = CreateFile(fs, subsubfolderPath, gulashRecipe[3]);
		string filePath5 = CreateFile(fs, subfolderPath2, gulashRecipe[4]);
		string filePath6 = CreateFile(fs, subfolderPath2, gulashRecipe[5]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		fs.Directory.Delete(subfolderPath1, true);
		string filePath7 = CreateFile(fs, subfolderPath2, gulashRecipe[6]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
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
		string filePath1 = CreateFile(fs, folderPath, gulashRecipe[0]);
		string filePath2 = CreateFile(fs, subfolderPath1, gulashRecipe[1]);
		
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// update folder and sync
		string subfolderPath2 = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name,	"sub2");
		string subfolderPath3 = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name, "sub3");
		string subsubfolderPath = Path.Combine(subfolderPath1, TestContext.CurrentContext.Test.Name, "subsub");
		string sub5folderPath = Path.Combine(subfolderPath1, TestContext.CurrentContext.Test.Name, "subsub2", "subsub3", "subsub4", "subsub5");

		fs.Directory.Delete(subfolderPath1, true);
		string filePath3 = CreateFile(fs, subfolderPath2, gulashRecipe[2]);
		string filePath4 = CreateFile(fs, subfolderPath2, gulashRecipe[3]);
		fs.Directory.CreateDirectory(subfolderPath3);
		string filePath5 = CreateFile(fs, sub5folderPath, gulashRecipe[4]);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public void Synchronize_HugeFile_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPath, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPath, TestContext.CurrentContext.Test.Name);
		string smallFileContent1 = gulashRecipe[1] + gulashRecipe[2] + gulashRecipe[3];
		string smallFileContent2 = gulashRecipe[1] + gulashRecipe[4] + gulashRecipe[5] + gulashRecipe[6] + gulashRecipe[3];
		Int64 hugeFileSize1 = (Int64)500 * 1024 * 1024; //500 MB
		string hugeFileContent2 = gulashRecipe[5] + gulashRecipe[6] + gulashRecipe[7];

		// create source folder and sync
		string smallFile = CreateFile(fs, folderPath, smallFileContent1);
		string hugeFile = CreateFile(fs, folderPath, hugeFileSize1);
		synchronizer.Synchronize(folderPath, replicaPath, logger);
		long sourceHugeFileSize = fs.FileInfo.New(hugeFile).Length;
		long replicaHugeFileSize = fs.FileInfo.New(hugeFile).Length;
		Assert.That(replicaHugeFileSize == sourceHugeFileSize, "The replicated huge file doesn't have the correct size after first synchronization.");

		// update folder and sync
		fs.File.WriteAllText(smallFile, smallFileContent2);
		fs.File.WriteAllText(hugeFile, hugeFileContent2);
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		string replicaSmallFileContent = fs.File.ReadAllText(smallFile);
		Assert.That(replicaSmallFileContent == smallFileContent2, "The content of the small file does not match the source.");
		long replicaHugeFileSize2 = fs.FileInfo.New(hugeFile).Length;
		Assert.That(replicaHugeFileSize2 < 1024 * 5, "The replicated huge file doesn't have the correct size.");
		string replicaBigFileContent = fs.File.ReadAllText(hugeFile);
		Assert.That(replicaBigFileContent == hugeFileContent2, "The content of the huge file does not match the source.");
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
		CreateFile(fs, folderPath, gulashRecipe[0] + gulashRecipe[1]);
		CreateFile(fs, subFolderPath1, gulashRecipe[2] + gulashRecipe[3] + gulashRecipe[4]);
		CreateFile(fs, subFolderPath1, gulashRecipe[0] + gulashRecipe[5] + gulashRecipe[7]);
		CreateFile(fs, subFolderPath2, gulashRecipe[4]);
		CreateFile(fs, subsubsubFolderPath, gulashRecipe[3] + gulashRecipe[5] + gulashRecipe[8]);

		// create replica
		CreateFile(fs, replicaPath, gulashRecipe[7] + gulashRecipe[8]);
		CreateFile(fs, replicaPath, gulashRecipe[1] + gulashRecipe[2] + gulashRecipe[3]);
		CreateFile(fs, subReplicaPath, gulashRecipe[8] + gulashRecipe[1] + gulashRecipe[7]);
		CreateFile(fs, subsubReplicaPath, gulashRecipe[0]);
		CreateFile(fs, subsubReplicaPath, gulashRecipe[3] + gulashRecipe[5]);

		// sync
		synchronizer.Synchronize(folderPath, replicaPath, logger);

		// assert results
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");
	}

	[Test]
	public async Task SynchronizePeriodically_OneFileMultipleChanges_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPathAsync, TestContext.CurrentContext.Test.Name);
		string replicaPath = Path.Combine(baseReplicaPathAsync, TestContext.CurrentContext.Test.Name);
		string content1 = gulashRecipe[0];
		string content2 = gulashRecipe[1];

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
		// cleanup
		Directory.Delete(folderPath, true);
		Directory.Delete(replicaPath, true);
	}

	[Test]
	public async Task SynchronizePeriodically_NestedFoldersCreateDeleteEdit_Pass() {
		IFileSystem fs = new FileSystem();
		MockLoggingService logger = new MockLoggingService();
		Synchronizer synchronizer = new Synchronizer(fs, fs);

		string folderPath = Path.Combine(baseFolderPathAsync, TestContext.CurrentContext.Test.Name);
		string subfolderPath1 = Path.Combine(folderPath, "sub1");
		string subfolderPath2 = Path.Combine(folderPath, "sub2");
		string subsubfolderPath = Path.Combine(subfolderPath1, "subsub");
		string subsubsubsubfolderPath = Path.Combine(folderPath, "sub3", "subsub", "subsubsub", "subsubsubsub");
		string replicaPath = Path.Combine(baseReplicaPathAsync, TestContext.CurrentContext.Test.Name);

		// create source folder and sync
		string filePath1 = CreateFile(fs, subfolderPath1, gulashRecipe[0] + gulashRecipe[1] + gulashRecipe[2]);
		string filePath2 = CreateFile(fs, subfolderPath1, gulashRecipe[1] + gulashRecipe[1]);
		string filePath3 = CreateFile(fs, subfolderPath1, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[6]);
		string filePath4 = CreateFile(fs, subfolderPath2, gulashRecipe[3]);
		string filePath5 = CreateFile(fs, subsubfolderPath, gulashRecipe[4]);
		string filePath6 = CreateFile(fs, subsubfolderPath, 0);
		synchronizer.SynchronizePeriodically(folderPath, replicaPath, 5, logger);

		// update folder
		await Task.Delay(200);
		fs.File.WriteAllText(filePath1, gulashRecipe[0] + gulashRecipe[1]);
		fs.File.WriteAllText(filePath5, gulashRecipe[2] + gulashRecipe[4] + gulashRecipe[5]);
		fs.File.WriteAllText(filePath6, gulashRecipe[1] + gulashRecipe[5] + gulashRecipe[7]);
		string filePath7 = CreateFile(fs, subfolderPath1, gulashRecipe[0]);
		string filePath8 = CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[1]);
		string filePath9 = CreateFile(fs, subsubsubsubfolderPath, gulashRecipe[2]);
		string filePath10 = CreateFile(fs, folderPath, gulashRecipe[3]);
		fs.File.Delete(filePath2);
		fs.File.Delete(filePath3);
		fs.File.Delete(filePath4);

		// assert results
		await Task.Delay(600);
		Assert.That(fs.Directory.Exists(replicaPath), "The replica folder wasn't created.");
		FolderDifference fd = GetFolderDifferences(fs, folderPath, replicaPath);
		Assert.That(fd.AreFoldersEqual(), $"The synchronized folder isn't the same as the original folder. {fd.DifferencesToString()}");

		// cleanup
		Directory.Delete(folderPath, true);
		Directory.Delete(replicaPath, true);
	}
}
