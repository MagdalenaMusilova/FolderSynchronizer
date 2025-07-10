using System.IO.Abstractions;

namespace FolderSynchronizerTests.HelperClasses
{
	internal static class FileCreator
	{
		private static int _fileIndex = 0;	// only need to ensure that indexes in one test are unique, across multiple tests they can be shared -> this doesn't need to be mutex 

		public static string CreateFile(IFileSystem fs, string folderPath, long size) {
			if (!fs.Directory.Exists(folderPath)) {
				fs.Directory.CreateDirectory(folderPath);
			}
			string filePath = Path.Combine(folderPath, (++_fileIndex).ToString());

			fs.File.WriteAllBytes(filePath, new byte[size]);
			return filePath;
		}

		public static string CreateFile(IFileSystem fs, string folderPath, string content) {
			if (!fs.Directory.Exists(folderPath)) {
				fs.Directory.CreateDirectory(folderPath);
			}
			string filePath = Path.Combine(folderPath, (++_fileIndex).ToString() + ".txt");

			fs.File.WriteAllText(filePath, content);
			return filePath;
		}
	}
}
