using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
	public class Chunk
	{
		public required string Hash { get; set; }
		public required int Size { get; set; }
	}
}
