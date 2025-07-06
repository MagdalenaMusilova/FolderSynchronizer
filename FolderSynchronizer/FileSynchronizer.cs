using FolderSynchronizer.Manifest;
using spkl.Diffs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
	internal class FileSynchronizer
	{
		public static void SynchronizeFile(IFileSystem fs, string pathToOrg, string pathToReplica, List<Chunk> orgChunks, List<Chunk> replicaChunks) {
			//Chunk.IndexChunks(ref orgChunks);
			//Chunk.IndexChunks(ref replicaChunks);

			//MyersDiff<Chunk> diff = new MyersDiff<Chunk>(replicaChunks.ToArray(), orgChunks.ToArray());
			//var edits = diff.GetEditScript();

			fs.File.Copy(pathToOrg, pathToReplica, true);
		}

		private static void AddChunksToStream(FileSystemStream source, FileSystemStream dest, List<Chunk> chunks, int chunkIndex, int numOfChunks) {
			int endChunkIndex = chunkIndex + numOfChunks - 1;
			int lenght = chunks[endChunkIndex].Index - chunks[chunkIndex].Index + chunks[endChunkIndex].Size;
			source.Seek(chunks[chunkIndex].Index, SeekOrigin.Begin);
			source.CopyTo(dest, lenght);
		}
	}
}
