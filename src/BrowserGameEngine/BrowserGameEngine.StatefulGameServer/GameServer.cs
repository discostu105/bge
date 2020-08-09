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
			services.AddSingleton<PlayerRepositoryWrite>();
			services.AddSingleton<ResourceRepository>();
			services.AddSingleton<ResourceRepositoryWrite>();
			services.AddSingleton<ScoreRepository>();
			services.AddSingleton<AssetRepository>();
			services.AddSingleton<AssetRepositoryWrite>();
			services.AddSingleton<UnitRepository>();
			services.AddSingleton<UnitRepositoryWrite>();
		}
	}
}

// workaround for roslyn bug: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
namespace System.Runtime.CompilerServices {
	public class IsExternalInit { }
}