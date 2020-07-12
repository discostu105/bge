using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer {
	public static class GameServerExtensions {
		public static void AddGameServer(this IServiceCollection services, WorldStateImmutable worldStateImmutable) {
			services.AddSingleton<WorldState>(worldStateImmutable.ToMutable());
			services.AddSingleton<PlayerRepository>();
			services.AddSingleton<PlayerRepository>();
			services.AddSingleton<PlayerRepositoryWrite>();
			services.AddSingleton<ScoreRepository>();
			services.AddSingleton<AssetRepository>();
			services.AddSingleton<AssetRepositoryWrite>();
			services.AddSingleton<UnitRepository>();
			services.AddSingleton<UnitRepositoryWrite>();
		}
	}
}
