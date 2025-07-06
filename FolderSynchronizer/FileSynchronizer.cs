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
	}
}
