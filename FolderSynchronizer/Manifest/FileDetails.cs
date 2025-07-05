using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer.Manifest
{
	public class FileDetails
	{
		public required long Size { get; set; }
		public required List<Chunk> Chunks { get; set; }
		public required string Checksum { get; set; }
	}
}
