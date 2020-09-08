using BrowserGameEngine.GameDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameTicks {
	public class GameTickModuleRegistry {
		private List<IGameTickModule> modules = new List<IGameTickModule>();
		private readonly ILogger<GameTickModuleRegistry> logger;

		public IEnumerable<IGameTickModule> Modules => modules;

		public GameTickModuleRegistry(ILogger<GameTickModuleRegistry> logger
				, IServiceProvider serviceProvider
				, GameDef gameDef
			) {
			this.logger = logger;
			Discover(serviceProvider, gameDef);
		}

		private void Discover(IServiceProvider serviceProvider, GameDef gameDef) {
			var gameTickModules = serviceProvider.GetServices<IGameTickModule>();
			var gameTickModuleDefs = gameDef.GameTickModules;
			foreach(var moduleDef in gameTickModuleDefs) {
				var module = gameTickModules.SingleOrDefault(x => x.Name == moduleDef.Name);
				if (module == null) throw new InvalidGameDefException($"GameTickModule with name '{moduleDef.Name}' is not registered. Check name and dependency injection.");
				RegisterModule(module, moduleDef);
			}
		}

		public void RegisterModule(IGameTickModule module, GameTickModuleDef moduleDef) {
			logger.LogInformation("Registering GameTickModule {Name}", moduleDef.Name);
			modules.Add(module);
			ConfigureModule(module, moduleDef);
		}

		private void ConfigureModule(IGameTickModule module, GameTickModuleDef moduleDef) {
			foreach(var property in moduleDef.Properties) {
				module.SetProperty(property.Key, property.Value);
			}
		}
	}
}
