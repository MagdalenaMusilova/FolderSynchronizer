using FolderSynchronizer.Manifest;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
	public class FileScanner
	{
		private readonly IFileSystem _fs;
		private readonly int _chunkSize;

		public FileScanner(IFileSystem fs, int chunkSize) {
			_fs = fs;
			_chunkSize = chunkSize;
		}

		public List<Chunk> SplitFileIntoChunks(string pathToFile) {
			List<Chunk> chunks = new List<Chunk>();


			var buffer = new byte[_chunkSize];
			using (var stream = _fs.File.OpenRead(pathToFile)) {
				while (true) {
					int bytesRead = stream.Read(buffer, 0, _chunkSize);
					if (bytesRead <= 0) {
						break;
					}

					string hash = GetBufferHash(buffer);
					chunks.Add(new Chunk() {
						Hash = hash,
						Size = bytesRead
					});
				}
			}

			return chunks;
		}

		public string GetFileChecksum(string pathToFile) {
			byte[] rawHash;
			using (var stream = _fs.File.OpenRead(pathToFile)) {
				rawHash = MD5.HashData(stream);
			}
			return Convert.ToHexString(rawHash);
		}


		private static string GetBufferHash(byte[] bytes) {
			byte[] rawHash = MD5.HashData(bytes);
			return Convert.ToHexString(rawHash);
		}
	}
}
