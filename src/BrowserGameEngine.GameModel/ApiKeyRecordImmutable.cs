using System;

namespace BrowserGameEngine.GameModel {
	public record ApiKeyRecordImmutable(
		string KeyId,
		string KeyHash,
		string KeyPrefix,
		DateTime CreatedAt,
		string? Name = null,
		DateTime? LastAccessedAt = null
	);
}
