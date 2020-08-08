using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public record BuildAssetCommand(PlayerId PlayerId, string AssetId) : ICommand;
	public record BuildUnitCommand(PlayerId PlayerId, string UnitId) : ICommand;
	public record ChangePlayerNameCommand(PlayerId PlayerId, string NewName) : ICommand;
	public record HarvestResourceCommand(PlayerId PlayerId, string ResourceId, int Count) : ICommand;
}
