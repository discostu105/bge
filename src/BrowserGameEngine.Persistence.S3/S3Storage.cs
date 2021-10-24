using System;
using System.Threading.Tasks;

namespace BrowserGameEngine.Persistence.S3 {
	public class S3Storage : IBlobStorage {
		private const string bucketName = "*** provide bucket name ***";
		private const string keyName = "*** provide a name for the uploaded object ***";
		private const string filePath = "*** provide the full path name of the file to upload ***";
		// Specify your bucket region (an example region is shown).
		private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USWest2;
		private static IAmazonS3 s3Client;

		public bool Exists(string name) {
			throw new NotImplementedException();
		}

		public Task<byte[]> Load(string name) {
			throw new NotImplementedException();
		}

		public Task Store(string name, byte[] blob) {
			throw new NotImplementedException();
		}
	}
}
