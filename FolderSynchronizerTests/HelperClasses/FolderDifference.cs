using System.IO.Abstractions;

namespace FolderSynchronizerTests.HelperClasses
{
	internal class FolderDifference
	{
		public List<string> missingFiles = new List<string>();
		public List<string> abundantFiles = new List<string>();
		public List<string> differentFiles = new List<string>();
		public List<string> missingFolders = new List<string>();
		public List<string> abundantFolders = new List<string>();

		private FolderDifference() { }

		public static FolderDifference CompareFolders(IFileSystem fs, string sourceFolder, string replicaFolder) {
			FolderDifference fd = new FolderDifference();

			fd.AddFolderDifferences(fs, sourceFolder, replicaFolder);

			return fd;
		}

		private void AddFolderDifferences(IFileSystem fs, string sourceFolder, string replicaFolder) {
			if (fs.Directory.Exists(sourceFolder) && !fs.Directory.Exists(replicaFolder)) {
				missingFolders.Add(replicaFolder);
			}

			var sourceFiles = fs.Directory.GetFiles(sourceFolder).Select(path => Path.GetRelativePath(sourceFolder, path));
			var replicaFiles = fs.Directory.GetFiles(replicaFolder).Select(path => Path.GetRelativePath(replicaFolder, path));
			missingFiles.AddRange(sourceFiles.Except(replicaFiles));
			abundantFiles.AddRange(replicaFiles.Except(sourceFiles));

			string[] sharedFiles = sourceFiles.Intersect(replicaFiles).ToArray();
			foreach (var file in sharedFiles) {
				if (!SameFile(fs, Path.Combine(sourceFolder, file), Path.Combine(replicaFolder, file))) {
					differentFiles.Add(file);
				}
			}

			var sourceDirectories = fs.Directory.GetDirectories(sourceFolder).Select(path => Path.GetRelativePath(sourceFolder, path));
			var replicaDirectories = fs.Directory.GetDirectories(replicaFolder).Select(path => Path.GetRelativePath(replicaFolder, path));
			missingFolders.AddRange(sourceDirectories.Except(replicaDirectories));
			abundantFiles.AddRange(replicaDirectories.Except(sourceDirectories));

			string[] sharedDirectories = sourceDirectories.Intersect(replicaDirectories).ToArray();
			foreach (var dir in sharedDirectories) {
				AddFolderDifferences(fs, Path.Combine(sourceFolder, dir), Path.Combine(replicaFolder, dir));
			}
		}

		private static bool SameFile(IFileSystem fs, string sourceFile, string replicaFile) {
			string sourceContent = fs.File.ReadAllText(sourceFile);
			string replicaContent = fs.File.ReadAllText(replicaFile);
			return sourceContent == replicaContent;
		}

		public bool AreFoldersEqual() {
			return missingFiles.Count == 0 && abundantFiles.Count == 0 && differentFiles.Count == 0 &&
				missingFolders.Count == 0 && abundantFolders.Count == 0;
		}

		public string DifferencesToString() {
			string res = "";
			if (missingFiles.Count != 0) {
				res += "Missing files: " + string.Join(", ", missingFiles) + "; ";
			}
			if (abundantFiles.Count != 0) {
				res += "File that shouldn't be in replica: " + string.Join(", ", abundantFiles) + "; ";
			}
			if (differentFiles.Count != 0) {
				res += "Files with different content: " + string.Join(", ", differentFiles) + "; ";
			}
			if (missingFolders.Count != 0) {
				res += "Missing folders: " + string.Join(", ", missingFolders) + "; ";
			}
			if (abundantFolders.Count != 0) {
				res += "Folders that shouldn't be in replica: " + string.Join(", ", abundantFolders) + "; ";
			}
			return res;
		}
	}
}
