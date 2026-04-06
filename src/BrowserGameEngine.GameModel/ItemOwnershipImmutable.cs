namespace BrowserGameEngine.GameModel;

public record ItemOwnershipImmutable(
	string OwnershipId,
	string UserId,
	string ItemId,
	System.DateTime PurchasedAt
);
