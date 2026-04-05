using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public record CreateTradeOfferCommand(
		PlayerId FromPlayerId,
		PlayerId ToPlayerId,
		ResourceDefId OfferedResourceId,
		decimal OfferedAmount,
		ResourceDefId WantedResourceId,
		decimal WantedAmount,
		string? Note
	);

	public record AcceptTradeOfferCommand(PlayerId AcceptingPlayerId, TradeOfferId OfferId);
	public record DeclineTradeOfferCommand(PlayerId DecliningPlayerId, TradeOfferId OfferId);
	public record CancelTradeOfferCommand(PlayerId CancellingPlayerId, TradeOfferId OfferId);
}
