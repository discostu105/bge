using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition;

public enum ItemCategory {
	Cosmetic
}

public record ShopItemDef(
	string ItemId,
	string Name,
	string Description,
	ItemCategory Category,
	decimal Price,
	bool IsAvailable
);

public class ShopConfig {
	public List<ShopItemDef> Items { get; init; } = new();
}
