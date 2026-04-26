using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class ApiKeyRecord {
		public required string KeyId { get; init; }
		public required string KeyHash { get; init; }
		public required string KeyPrefix { get; init; }
		public required DateTime CreatedAt { get; init; }
		public string? Name { get; set; }
		public DateTime? LastAccessedAt { get; set; }
	}

	internal static class ApiKeyRecordExtensions {
		internal static ApiKeyRecordImmutable ToImmutable(this ApiKeyRecord r) {
			return new ApiKeyRecordImmutable(
				KeyId: r.KeyId,
				KeyHash: r.KeyHash,
				KeyPrefix: r.KeyPrefix,
				CreatedAt: r.CreatedAt,
				Name: r.Name,
				LastAccessedAt: r.LastAccessedAt
			);
		}

		internal static ApiKeyRecord ToMutable(this ApiKeyRecordImmutable r) {
			return new ApiKeyRecord {
				KeyId = r.KeyId,
				KeyHash = r.KeyHash,
				KeyPrefix = r.KeyPrefix,
				CreatedAt = r.CreatedAt,
				Name = r.Name,
				LastAccessedAt = r.LastAccessedAt
			};
		}
	}
}
