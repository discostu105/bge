using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public record CreateAllianceCommand(PlayerId PlayerId, string AllianceName, string Password) : ICommand;
	public record JoinAllianceCommand(PlayerId PlayerId, AllianceId AllianceId, string Password) : ICommand;
	public record AcceptMemberCommand(PlayerId PlayerId, PlayerId MemberPlayerId) : ICommand;
	public record RejectMemberCommand(PlayerId PlayerId, PlayerId MemberPlayerId) : ICommand;
	public record LeaveAllianceCommand(PlayerId PlayerId) : ICommand;
	public record KickMemberCommand(PlayerId PlayerId, PlayerId MemberPlayerId) : ICommand;
	public record VoteLeaderCommand(PlayerId PlayerId, PlayerId VoteePlayerId) : ICommand;
	public record SetAlliancePasswordCommand(PlayerId PlayerId, string NewPassword) : ICommand;
	public record SetAllianceMessageCommand(PlayerId PlayerId, string Message) : ICommand;
	public record PostAllianceChatCommand(PlayerId PlayerId, AllianceId AllianceId, string Body) : ICommand;

	public record BuildAssetCommand(PlayerId PlayerId, AssetDefId AssetDefId) : ICommand;
	public record BuildUnitCommand(PlayerId PlayerId, UnitDefId UnitDefId, int Count) : ICommand;
	public record MergeUnitsCommand(PlayerId PlayerId, UnitDefId UnitDefId) : ICommand;
	public record SplitUnitCommand(PlayerId PlayerId, UnitId UnitId, int SplitCount) : ICommand;
	public record SendUnitCommand(PlayerId PlayerId, UnitId UnitId, PlayerId EnemyPlayerId) : ICommand;
	public record ReturnUnitsHomeCommand(PlayerId PlayerId, PlayerId EnemyPlayerId) : ICommand;
	public record MergeAllUnitsCommand(PlayerId PlayerId) : ICommand;
	public record ChangePlayerNameCommand(PlayerId PlayerId, string NewName) : ICommand;
	public record HarvestResourceCommand(PlayerId PlayerId, string ResourceId, int Count) : ICommand;
	public record AssignWorkersCommand(PlayerId PlayerId, int MineralWorkers, int GasWorkers) : ICommand;
	public record ColonizeCommand(PlayerId PlayerId, int Amount) : ICommand;
	public record ResearchUpgradeCommand(PlayerId PlayerId, UpgradeType UpgradeType) : ICommand;
	public record SendMessageCommand(PlayerId SenderId, PlayerId RecipientId, string Subject, string Body) : ICommand;
	public record MarkMessageReadCommand(PlayerId PlayerId, MessageId MessageId) : ICommand;
	public record AddToQueueCommand(PlayerId PlayerId, string Type, string DefId, int Count) : ICommand;
	public record RemoveFromQueueCommand(PlayerId PlayerId, Guid EntryId) : ICommand;
	public record ReorderQueueCommand(PlayerId PlayerId, Guid EntryId, int NewPriority) : ICommand;
	public record TradeResourceCommand(PlayerId PlayerId, ResourceDefId FromResource, int Amount) : ICommand;

	public record CreateMarketOrderCommand(PlayerId PlayerId, ResourceDefId OfferedResourceId, decimal OfferedAmount, ResourceDefId WantedResourceId, decimal WantedAmount) : ICommand;
	public record AcceptMarketOrderCommand(PlayerId BuyerPlayerId, MarketOrderId OrderId) : ICommand;
	public record CancelMarketOrderCommand(PlayerId PlayerId, MarketOrderId OrderId) : ICommand;
}
