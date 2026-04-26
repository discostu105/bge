using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/games")]
public class SpectatorController : ControllerBase
{
	private readonly GlobalState _globalState;
	private readonly GameRegistry _gameRegistry;
	private readonly PlayerRepository _playerRepository;
	private readonly ResourceRepository _resourceRepository;
	private readonly OnlineStatusRepository _onlineStatusRepository;

	public SpectatorController(
		GlobalState globalState,
		GameRegistry gameRegistry,
		PlayerRepository playerRepository,
		ResourceRepository resourceRepository,
		OnlineStatusRepository onlineStatusRepository)
	{
		_globalState = globalState;
		_gameRegistry = gameRegistry;
		_playerRepository = playerRepository;
		_resourceRepository = resourceRepository;
		_onlineStatusRepository = onlineStatusRepository;
	}

	/// <summary>Returns a live snapshot of the top 20 players for anonymous spectators.</summary>
	[HttpGet("{gameId}/spectate")]
	[ProducesResponseType(typeof(SpectatorSnapshotViewModel), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<SpectatorSnapshotViewModel> GetSpectatorSnapshot(string gameId)
	{
		var record = _globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
		if (record == null) return NotFound();

		var instance = _gameRegistry.TryGetInstance(record.GameId);
		if (instance == null)
		{
			return Ok(new SpectatorSnapshotViewModel(
				GameId: gameId,
				GameName: record.Name,
				GameStatus: record.Status.ToString(),
				TopPlayers: Array.Empty<SpectatorPlayerEntryViewModel>(),
				Tick: 0
			));
		}

		var entries = _playerRepository.GetAll()
			.Select(p => new {
				Player = p,
				Land = _resourceRepository.GetLand(p.PlayerId),
			})
			.OrderByDescending(x => x.Land)
			.Take(20)
			.Select((x, i) => new SpectatorPlayerEntryViewModel(
				Rank: i + 1,
				PlayerId: x.Player.PlayerId.Id,
				PlayerName: x.Player.Name,
				Land: x.Land,
				IsOnline: _onlineStatusRepository.IsOnline(x.Player.PlayerId),
				IsAgent: x.Player.ApiKeys.Count > 0
			))
			.ToList();

		return Ok(new SpectatorSnapshotViewModel(
			GameId: gameId,
			GameName: record.Name,
			GameStatus: record.Status.ToString(),
			TopPlayers: entries,
			Tick: 0
		));
	}
}
