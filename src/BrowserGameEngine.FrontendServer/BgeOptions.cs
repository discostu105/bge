using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
    public class BgeOptions {
		public const string Position = "Bge";

		/// <summary>
		/// Allows simple login without password for any account. For dev purposes only.
		/// </summary>
		public bool DevAuth { get; set; }

		/// <summary>
		/// User IDs that are granted admin privileges (e.g. access to admin endpoints, game creation).
		/// </summary>
		public List<string> AdminUserIds { get; set; } = new();

		// "localfile", "s3"
		public required string StorageType { get; set; }

		public required string S3BucketName { get; set; }
    }
}
