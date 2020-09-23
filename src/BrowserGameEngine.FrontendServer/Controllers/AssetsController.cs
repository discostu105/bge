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

namespace BrowserGameEngine.Server.Controllers {
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

		[HttpGet]
		public async Task<AssetsViewModel> Get() {
			return new AssetsViewModel {
				Assets = gameDef.GetAssetsByPlayerType(playerRepository.GetPlayerType(currentUserContext.PlayerId)).Select(x => CreateAssetViewModel(x)).ToList()
			};
		}

		[HttpPost]
		public async Task<ActionResult> Build([FromQuery] string assetDefId) {
			try {
				assetRepositoryWrite.BuildAsset(new BuildAssetCommand(currentUserContext.PlayerId, Id.AssetDef(assetDefId)));
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
				Built = assetRepository.HasAsset(currentUserContext.PlayerId, assetDef.Id),
				Prerequisites = string.Join(", ", gameDef.GetAssetNames(assetDef.Prerequisites)),
				PrerequisitesMet = assetRepository.PrerequisitesMet(currentUserContext.PlayerId, assetDef),
				Cost = CostViewModel.Create(assetDef.Cost),
				CanAfford = resourceRepository.CanAfford(currentUserContext.PlayerId, assetDef.Cost),
				AlreadyQueued = assetRepository.IsBuildQueued(currentUserContext.PlayerId, assetDef.Id),
				TicksLeftForBuild = assetRepository.TicksLeft(currentUserContext.PlayerId, assetDef.Id),
				AvailableUnits = gameDef.GetUnitsForAsset(assetDef.Id)
					.Select(x => UnitDefinitionViewModel.Create(x, unitRepository.PrerequisitesMet(currentUserContext.PlayerId, x))).ToList()
			};
		}
	}
}
