﻿using BrowserGameEngine.Shared;
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

namespace BrowserGameEngine.FrontendServer.Controllers {
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class AssetsController : ControllerBase {
		private readonly ILogger<AssetsController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly AssetRepository assetRepository;
		private readonly AssetRepositoryWrite assetRepositoryWrite;
		private readonly GameDef gameDef;

		public AssetsController(ILogger<AssetsController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, AssetRepository assetRepository
				, AssetRepositoryWrite assetRepositoryWrite
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.assetRepository = assetRepository;
			this.assetRepositoryWrite = assetRepositoryWrite;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public AssetsViewModel Get() {
			var playerAssets = assetRepository.Get(currentUserContext.PlayerId);
			return new AssetsViewModel {
				Assets = gameDef.GetAssetsByPlayerType(currentUserContext.PlayerTypeId).Select(x => CreateAssetViewModel(x, playerAssets)).ToList()
			};
		}

		private AssetViewModel CreateAssetViewModel(AssetDef assetDef, IEnumerable<AssetImmutable> playerAssets) {
			var playerAsset = playerAssets.SingleOrDefault(x => x.AssetDefId == assetDef.Id);

			if (playerAsset == null) {
				// not built yet
				return new AssetViewModel {
					Definition = AssetDefinitionViewModel.Create(assetDef),
					Count = 0,
					PrerequisitesMet = assetRepository.PrerequisitesMet(currentUserContext.PlayerId, assetDef),
					Cost = CostViewModel.Create(assetDef.Cost)
				};
			} else {
				// already built
				return new AssetViewModel {
					Definition = AssetDefinitionViewModel.Create(assetDef),
					Count = 1,
					Level = playerAsset.Level,
					PrerequisitesMet = assetRepository.PrerequisitesMet(currentUserContext.PlayerId, assetDef),
					Cost = CostViewModel.Create(assetDef.Cost)
				};
			}
		}
	}
}
