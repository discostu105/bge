using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.GameDefinition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}/{id?}")]
	public class AssetsController : ControllerBase {
		private readonly ILogger<AssetsController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly AssetRepository assetRepository;
		private readonly AssetRepositoryWrite assetRepositoryWrite;
		private readonly ResourceRepository resourceRepository;
		private readonly UnitRepository unitRepository;
		private readonly GameDef gameDef;

		public AssetsController(ILogger<AssetsController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, AssetRepository assetRepository
				, AssetRepositoryWrite assetRepositoryWrite
				, ResourceRepository resourceRepository
				, UnitRepository unitRepository
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.assetRepository = assetRepository;
			this.assetRepositoryWrite = assetRepositoryWrite;
			this.resourceRepository = resourceRepository;
			this.unitRepository = unitRepository;
			this.gameDef = gameDef;
		}

		/// <summary>Returns all buildings (assets) available to the current player, including build status and prerequisites.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(AssetsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult<AssetsViewModel>> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			return new AssetsViewModel {
				Assets = gameDef.GetAssetsByPlayerType(playerRepository.GetPlayerType(currentUserContext.PlayerId!)).Select(x => CreateAssetViewModel(x)).ToList()
			};
		}

		/// <summary>Initiates construction of a building. The build is added to the queue and completes after the required ticks.</summary>
		/// <param name="assetDefId">Building definition ID (e.g. "barracks").</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult> Build([FromQuery] string assetDefId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				assetRepositoryWrite.BuildAsset(new BuildAssetCommand(currentUserContext.PlayerId!, Id.AssetDef(assetDefId)));
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			} catch (AssetAlreadyBuiltException e) {
				return BadRequest(e.Message);
			} catch (AssetAlreadyQueuedException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost]
		public async Task Upgrade(string assetDefId) {
			throw new NotImplementedException();
		}

		private AssetViewModel CreateAssetViewModel(AssetDef assetDef) {
			return new AssetViewModel {
				Definition = AssetDefinitionViewModel.Create(assetDef),
				Built = assetRepository.HasAsset(currentUserContext.PlayerId!, assetDef.Id),
				Prerequisites = string.Join(", ", gameDef.GetAssetNames(assetDef.Prerequisites)),
				PrerequisitesMet = assetRepository.PrerequisitesMet(currentUserContext.PlayerId!, assetDef),
				Cost = CostViewModel.Create(assetDef.Cost),
				CanAfford = resourceRepository.CanAfford(currentUserContext.PlayerId!, assetDef.Cost),
				AlreadyQueued = assetRepository.IsBuildQueued(currentUserContext.PlayerId!, assetDef.Id),
				TicksLeftForBuild = assetRepository.TicksLeft(currentUserContext.PlayerId!, assetDef.Id),
				AvailableUnits = gameDef.GetUnitsForAsset(assetDef.Id)
					.Select(x => UnitDefinitionViewModel.Create(x, unitRepository.PrerequisitesMet(currentUserContext.PlayerId!, x))).ToList()
			};
		}
	}
}
