using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace BrowserGameEngine.FrontendServer;

/// <summary>
/// Persists ASP.NET Core Data Protection XML keys to S3 so all container
/// instances share a single key ring and OAuth state cookies survive
/// rolling deployments.
/// </summary>
internal sealed class S3DataProtectionKeyRepository : IXmlRepository
{
	private readonly IAmazonS3 _s3;
	private readonly string _bucketName;
	private readonly string _keyPrefix;

	public S3DataProtectionKeyRepository(IAmazonS3 s3, string bucketName, string keyPrefix)
	{
		_s3 = s3;
		_bucketName = bucketName;
		_keyPrefix = keyPrefix.TrimEnd('/') + '/';
	}

	public IReadOnlyCollection<XElement> GetAllElements()
	{
		var list = _s3.ListObjectsV2Async(new ListObjectsV2Request {
			BucketName = _bucketName,
			Prefix = _keyPrefix
		}).GetAwaiter().GetResult();

		var elements = new List<XElement>();
		foreach (var obj in list.S3Objects ?? []) {
			var response = _s3.GetObjectAsync(_bucketName, obj.Key).GetAwaiter().GetResult();
			using var stream = response.ResponseStream;
			elements.Add(XElement.Load(stream));
		}
		return elements;
	}

	public void StoreElement(XElement element, string friendlyName)
	{
		var key = $"{_keyPrefix}{friendlyName}.xml";
		using var ms = new MemoryStream();
		element.Save(ms);
		ms.Position = 0;
		_s3.PutObjectAsync(new PutObjectRequest {
			BucketName = _bucketName,
			Key = key,
			InputStream = ms,
			ContentType = "application/xml"
		}).GetAwaiter().GetResult();
	}
}
