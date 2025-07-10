namespace FolderSynchronizer
{
	/// <summary>
	/// A chunk of a file used for comparison or synchronization.
	/// </summary>
	internal class Chunk
	{
		/// <summary>
		/// Chunk with no information.
		/// </summary>
		public static readonly Chunk Empty = new Chunk();

		/// <summary>
		/// The cryptographic hash of the chunk's content.
		/// </summary>
		public byte[] hash;
		/// <summary>
		/// The size of the chunk in bytes.
		/// </summary>
		public int size;
		/// <summary>
		/// The starting byte index of this chunk within the file.
		/// </summary>
		public int index;

	}
}
