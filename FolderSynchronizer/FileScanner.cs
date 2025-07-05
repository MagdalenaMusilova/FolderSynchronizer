using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
	internal class FileScanner
	{
		private readonly IFileSystem _fs;
		private readonly int _chunkSize;


		public FileScanner(IFileSystem fs, int chunkSize) {
			_fs = fs;
			_chunkSize = chunkSize;
		}

		public List<Chunk> SplitFileIntoChunks(string pathToFile) {
			List<Chunk> chunks = new List<Chunk>();

			var stream = _fs.File.OpenRead(pathToFile);
			var buffer = new byte[_chunkSize];
			while (true) {
				int bytesRead = stream.Read(buffer, 0, _chunkSize);
				if (bytesRead < 0) {
					break;
				}

				string hash = GetHash(buffer);
				chunks.Add(new Chunk() {
					Hash = hash,
					Size = bytesRead
				});
			}

			return chunks;
		}

		public string GetFileChecksum(string pathToFile) {
			var stream = _fs.File.OpenRead(pathToFile);
			byte[] rawHash = MD5.HashData(stream);
			return Convert.ToHexString(rawHash);
		}

		private string GetHash(byte[] bytes) {
			byte[] rawHash = MD5.HashData(bytes);
			return Convert.ToHexString(rawHash);
		}
	}
}
