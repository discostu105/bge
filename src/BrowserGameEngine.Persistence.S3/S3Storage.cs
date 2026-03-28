using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public IEnumerable<string> List(string folderPrefix) {
			var fullPrefix = GetKey(folderPrefix) + "/";
			var request = new ListObjectsV2Request { BucketName = bucketName, Prefix = fullPrefix };
			var response = s3Client.ListObjectsV2Async(request).GetAwaiter().GetResult();
			var stripLen = string.IsNullOrEmpty(keyPrefix) ? 0 : keyPrefix.Length + 1;
			return response.S3Objects.Select(o => o.Key.Substring(stripLen));
		}

		public async Task Delete(string name) {
			await s3Client.DeleteObjectAsync(bucketName, GetKey(name));
		}
	}
}
