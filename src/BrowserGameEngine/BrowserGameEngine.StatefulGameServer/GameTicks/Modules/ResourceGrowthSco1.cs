﻿using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// SCO: based on WBF's and existing "land", the resource "minerals" is increased.
	///      in SCO, there was also an assignement of WBFs to minerals and gas.
	/// </summary>
	public class ResourceGrowthSco1 : IGameTickModule {
		public string Name => "resource-growth-sco:1";
		private readonly GameDef gameDef;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly AssetRepository assetRepository;
		private readonly UnitRepository unitRepository;

		private UnitDefId workerUnit;
		private ResourceDefId growthResource;
		private ResourceDefId constraintResource;

		public ResourceGrowthSco1(GameDef gameDef
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, AssetRepository assetRepository
				, UnitRepository unitRepository
			) {
			this.gameDef = gameDef;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.assetRepository = assetRepository;
			this.unitRepository = unitRepository;
		}

		public void SetProperty(string name, string value) {
			switch(name) {
				case "worker-units":
					workerUnit = new UnitDefId(value);
					gameDef.ValidateUnitDefId(workerUnit);
					break;
				case "growth-resource":
					growthResource = new ResourceDefId(value);
					gameDef.ValidateResourceDefId(growthResource);
					break;
				case "constraint-resource":
					constraintResource = new ResourceDefId(value);
					gameDef.ValidateResourceDefId(constraintResource);
					break;
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}
		
		public void CalculateTick(PlayerId playerId) {
			//Console.WriteLine("yay");
		}
	}
}
