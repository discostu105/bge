using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public record BuildAssetCommand(PlayerId PlayerId, AssetDefId AssetDefId) : ICommand;
	public record BuildUnitCommand(PlayerId PlayerId, UnitDefId UnitDefId, int Count) : ICommand;
	public record MergeUnitsCommand(PlayerId PlayerId, UnitDefId UnitDefId) : ICommand;
	public record SplitUnitCommand(PlayerId PlayerId, UnitId UnitId, int SplitCount) : ICommand;
	public record SendUnitCommand(PlayerId PlayerId, UnitId UnitId, PlayerId EnemyPlayerId) : ICommand;
	public record ReturnUnitsHomeCommand(PlayerId PlayerId, PlayerId EnemyPlayerId) : ICommand;
	public record MergeAllUnitsCommand(PlayerId PlayerId) : ICommand;
	public record ChangePlayerNameCommand(PlayerId PlayerId, string NewName) : ICommand;
	public record HarvestResourceCommand(PlayerId PlayerId, string ResourceId, int Count) : ICommand;
}
