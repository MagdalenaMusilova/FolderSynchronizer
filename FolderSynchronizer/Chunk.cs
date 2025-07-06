using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FolderSynchronizer
{
	public class Chunk
	{
		public required string Hash { get; set; }
		public required int Size { get; set; }
		[JsonIgnore]
		public int Index {  get; set; }


		public override bool Equals(object obj) {
			if (obj is Chunk other) {
				return Size == other.Size && Hash == other.Hash;
			}
			return false;
		}

		public override int GetHashCode() {
			return HashCode.Combine(Hash, Size);
		}

		public static void IndexChunks(ref List<Chunk> chunks) {
			int index = 0;
			foreach (var chunk in chunks) {
				chunk.Index = index;
				index += chunk.Size;
			}
		}
	}
}
