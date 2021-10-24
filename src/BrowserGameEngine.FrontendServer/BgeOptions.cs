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

		// "localfile", "s3"
		public string StorageType { get; set; }

		public string S3BucketName { get; set; }
    }
}
