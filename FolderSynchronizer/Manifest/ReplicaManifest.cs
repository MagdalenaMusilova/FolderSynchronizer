using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FolderSynchronizer.Manifest
{
	public class ReplicaManifest
	{
		public required DateTime Created { get; set; }
		public required DateTime Updated { get; set; }
		public required string FolderPath { get; set; }
		public required Dictionary<string, FileDetails> Files { get; set; }
		public string? ManifestChecksum { get; set; }

		[OnSerializing]
		private void SetChecksum() {
			ManifestChecksum = GetChecksum();
		}

		public string GetChecksum() {
			string? tmp = ManifestChecksum;
			ManifestChecksum = String.Empty;
			string jsonString = JsonSerializer.Serialize(this);
			ManifestChecksum = tmp;

			byte[] rawHash = MD5.HashData(Encoding.UTF8.GetBytes(jsonString));
			return Convert.ToHexString(rawHash);
		}

		public bool ChecksumMatches(string checksum) {
			string thisChecksum = GetChecksum();
			return thisChecksum == checksum;
		}
	}
}
