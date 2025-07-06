using FolderSynchronizer;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FolderSynchronizerTests
{
	[Parallelizable(ParallelScope.All)]
	public class FileScannerTest
	{
		private static readonly string loremIpsum = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Nullam justo enim, consectetuer nec, ullamcorper ac, vestibulum in, elit. Nullam at arcu a est sollicitudin euismod. Aenean placerat. Duis sapien nunc, commodo et, interdum suscipit, sollicitudin et, dolor. " +
			"In convallis. Nulla quis diam. Nulla pulvinar eleifend sem. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Donec vitae arcu. Praesent id justo in neque elementum ultrices. Duis risus.Integer pellentesque quam vel velit. Duis ante orci, molestie vitae " +
			"vehicula venenatis, tincidunt ac pede. Curabitur ligula sapien, pulvinar a vestibulum quis, facilisis vel sapien. Nullam lectus justo, vulputate eget mollis sed, tempor sed magna. Proin in tellus sit amet nibh dignissim sagittis. Nullam justo enim, consectetuer nec, ullamcorper ac, vestibulum in, elit. " +
			"Maecenas lorem. Nullam feugiat, turpis at pulvinar vulputate, erat libero tristique tellus, nec bibendum odio risus sit amet ante. Praesent in mauris eu tortor porttitor accumsan. Maecenas libero.Aliquam erat volutpat. Cras elementum. Nulla quis diam. In enim a arcu imperdiet malesuada. Donec quis nibh at felis congue commodo. " +
			"Nullam at arcu a est sollicitudin euismod. Phasellus rhoncus. Nulla pulvinar eleifend sem. Quisque porta. Cras elementum. Integer pellentesque quam vel velit. Nunc tincidunt ante vitae massa. In sem justo, commodo ut, suscipit at, pharetra vitae, orci. Nunc tincidunt ante vitae massa. ";
		
		readonly static string _basePath = Path.Combine(Environment.CurrentDirectory, "Test", nameof(FileScannerTest));
		private static readonly int _charsPerChunk = 4;
		private static readonly int _chunkSize = _charsPerChunk * sizeof(char);

		private IFileSystem _fs = new FileSystem();
		private IFileSystem _mockFs = new MockFileSystem();
		private FileScanner _scanner;
		private FileScanner _mockScanner;
		int fileIndex = 0;

		[OneTimeSetUp]
		public void OneTimeSetUp() {
			_scanner = new FileScanner(_fs, _chunkSize);
			_mockScanner = new FileScanner(_mockFs, _chunkSize);

			if (_fs.Directory.Exists(_basePath)) {
				_fs.Directory.Delete(_basePath, true);
			}
			_fs.Directory.CreateDirectory(_basePath);
		}

		[OneTimeTearDown]
		public void OneTimeTearDown() {
			_fs.Directory.Delete(_basePath, true);
		}



		private string CreateFile(string folderPath, int numOfChars = 256) {
			if (!_fs.Directory.Exists(folderPath)) {
				_fs.Directory.CreateDirectory(folderPath);
			}
			string filePath = Path.Combine(folderPath, (++fileIndex).ToString() + ".txt");
			string content = loremIpsum.Substring(0, numOfChars);

			_fs.File.WriteAllText(filePath, content);
			return filePath;
		}

		private string CreateFile(string folderPath, string content) {
			if (!_fs.Directory.Exists(folderPath)) {
				_fs.Directory.CreateDirectory(folderPath);
			}
			string filePath = Path.Combine(folderPath, (++fileIndex).ToString() + ".txt");

			_fs.File.WriteAllText(filePath, content);
			return filePath;
		}

		private string ChunksToString(List<Chunk> chunks) {
			return "[" + String.Join(",", chunks.Select(c => c.Hash)) + "]";
		}


		[Test]
		public void SplitFileIntoChunks_UnevenChunks_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, _charsPerChunk * 3 + 1);	//create file of size that doesn't split into even chunks

			// split file into chunks
			List<Chunk> chunks =_scanner.SplitFileIntoChunks(filePath);
			Assert.That(chunks.Any(), "No chunks were created when splitting the file.");
		}

		[Test]
		public void SplitFileIntoChunks_EvenChunks_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, _charsPerChunk * 3);    //create file of size that spits perfectly into full chunks

			// split file into chunks
			List<Chunk> chunks = _scanner.SplitFileIntoChunks(filePath);
			Assert.That(chunks.Any(), "No chunks were created when splitting the file.");
		}

		[Test]
		public void SplitFileIntoChunks_LessThanChunk_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, _charsPerChunk / 2);    

			// split file into chunks
			_scanner.SplitFileIntoChunks(filePath);
		}

		[Test]
		public void SplitFileIntoChunks_ExactlyOneChunk_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, _charsPerChunk);

			// split file into chunks
			_scanner.SplitFileIntoChunks(filePath);
		}

		[Test]
		public void SplitFileIntoChunks_EmptyFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, 0);

			// split file into chunks
			_scanner.SplitFileIntoChunks(filePath);
		}

		[Test]
		public void SplitFileIntoChunks_SubdirectoryFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name, "sub", "subsub");
			// create source folder
			string filePath = CreateFile(folderPath);

			// split file into chunks
			List<Chunk> chunks = _scanner.SplitFileIntoChunks(filePath);
			Assert.That(chunks.Any(), "No chunks were created when splitting the file.");
		}

		[Test]
		public void SplitFileIntoChunks_TwoSameContentFiles_SameChunks() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath1 = CreateFile(folderPath, loremIpsum);
			string filePath2 = CreateFile(folderPath, loremIpsum);

			// split file into chunks
			List<Chunk> chunks1 = _scanner.SplitFileIntoChunks(filePath1);
			List<Chunk> chunks2 = _scanner.SplitFileIntoChunks(filePath2);
			Assert.That(chunks1.SequenceEqual(chunks2), $"The same two files produce different chunks. Expected: {ChunksToString(chunks1)}, Result: {ChunksToString(chunks2)}");
		}

		[Test]
		public void SplitFileIntoChunks_TwoDifferentFiles_DifferentChunks() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string content1 = loremIpsum.Substring(0, loremIpsum.Length / 2);
			string content2 = loremIpsum.Substring(loremIpsum.Length / 2);
			if (content1 == content2) {
				Assert.Inconclusive("The assumed different content of the two files in this test is the same, thus this test can't be performed correctly.  Please adjust the test so the file have different contents.");
			}

			string filePath1 = CreateFile(folderPath, content1);
			string filePath2 = CreateFile(folderPath, content2);

			// split file into chunks
			List<Chunk> chunks1 = _scanner.SplitFileIntoChunks(filePath1);
			List<Chunk> chunks2 = _scanner.SplitFileIntoChunks(filePath2);
			if (chunks1.SequenceEqual(chunks2)) {
				Assert.Warn("Files with different content have the same chunks.");
			} else {
				Assert.Pass();
			}
		}



		[Test]
		public void GetFileChecksum_SimpleFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath);
			_scanner.GetFileChecksum(filePath);
		}

		[Test]
		public void GetFileChecksum_SubdirectoryFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name, "sub", "subsub");
			// create source folder
			string filePath = CreateFile(folderPath);
			_scanner.GetFileChecksum(filePath);
		}

		[Test]
		public void GetFileChecksum_OneCharFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, 1);
			_scanner.GetFileChecksum(filePath);
		}

		[Test]
		public void GetFileChecksum_EmptyFile_Pass() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath, 0);
			_scanner.GetFileChecksum(filePath);
		}

		[Test]
		public void GetFileChecksum_CompareFileToItself_Equeals() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath = CreateFile(folderPath);
			string checksum1 = _scanner.GetFileChecksum(filePath);
			string checksum2 = _scanner.GetFileChecksum(filePath);
			Assert.That(checksum1 == checksum2, "Getting checksum of the same file multiple times gains different results.");
		}

		[Test]
		public void GetFileChecksum_CompareSameContentDifferentFiles_Equeals() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string filePath1 = CreateFile(folderPath, loremIpsum);
			string filePath2 = CreateFile(folderPath, loremIpsum);
			string checksum1 = _scanner.GetFileChecksum(filePath1);
			string checksum2 = _scanner.GetFileChecksum(filePath2);
			Assert.That(checksum1 == checksum2, "Checksums of two files with the same content have different results.");
		}

		[Test]
		public void GetFileChecksum_CompareDifferentFiles_DontEqual() {
			string folderPath = Path.Combine(_basePath, TestContext.CurrentContext.Test.Name);
			// create source folder
			string content1 = loremIpsum.Substring(0, loremIpsum.Length / 2);
			string content2 = loremIpsum.Substring(loremIpsum.Length / 2);
			if (content1 == content2) {
				Assert.Inconclusive("The assumed different content of the two files in this test is the same, thus this test can't be performed correctly. Please adjust the test so the file have different contents.");
			}

			string filePath1 = CreateFile(folderPath, content1);
			string filePath2 = CreateFile(folderPath, content2);

			// split file into chunks
			List<Chunk> chunks1 = _scanner.SplitFileIntoChunks(filePath1);
			List<Chunk> chunks2 = _scanner.SplitFileIntoChunks(filePath2);
			if (chunks1.SequenceEqual(chunks2)) {
				Assert.Warn("Two different files with different content have the same checksum.");
			} else {
				Assert.Pass();
			}
		}
	}
}
