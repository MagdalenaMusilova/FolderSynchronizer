using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
	internal class Chunk
	{
		public required byte[] hash;
		public required int size;
		public required int index;
	}
}
