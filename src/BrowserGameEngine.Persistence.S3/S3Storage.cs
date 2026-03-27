using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace BrowserGameEngine.Persistence.S3 {
	public class S3Storage : IBlobStorage {
		private readonly IAmazonS3 s3Client;
		private readonly string bucketName;
		private readonly string keyPrefix;

		public S3Storage(IAmazonS3 s3Client, string bucketName, string keyPrefix = "") {
			this.s3Client = s3Client;
			this.bucketName = bucketName;
			this.keyPrefix = keyPrefix;
		}

		private string GetKey(string name) => string.IsNullOrEmpty(keyPrefix) ? name : $"{keyPrefix}/{name}";

		public bool Exists(string name) {
			try {
				var response = s3Client.GetObjectMetadataAsync(bucketName, GetKey(name)).GetAwaiter().GetResult();
				return true;
			} catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
				return false;
			}
		}

		public async Task<byte[]> Load(string name) {
			var response = await s3Client.GetObjectAsync(bucketName, GetKey(name));
			using var memoryStream = new MemoryStream();
			await response.ResponseStream.CopyToAsync(memoryStream);
			return memoryStream.ToArray();
		}

		public async Task Store(string name, byte[] blob) {
			using var stream = new MemoryStream(blob);
			var request = new PutObjectRequest {
				BucketName = bucketName,
				Key = GetKey(name),
				InputStream = stream
			};
			await s3Client.PutObjectAsync(request);
		}
	}
}
