using System;
using System.Security.Cryptography;
using System.Text.Json;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using KellySharp;

namespace BrowserGameEgnine.Persistence {
	public class GameStateJsonSerializer {
		public byte[] Serialize(WorldStateImmutable worldStateImmutable) {
			return JsonSerializer.SerializeToUtf8Bytes<WorldStateImmutable>(worldStateImmutable, GetOptions());
		}

		public WorldStateImmutable Deserialize(byte[] blob) {
			return JsonSerializer.Deserialize<WorldStateImmutable>(blob, GetOptions());
		}

		private static JsonSerializerOptions GetOptions() {
			return new JsonSerializerOptions {
				WriteIndented = true,
				Converters = { GetIdConverters() }
			};
		}

		private static DictionaryJsonConverterFactory GetIdConverters() {
			return new DictionaryJsonConverterFactoryBuilder()
				.AddParser<PlayerId>((str) => PlayerIdFactory.Create(str))
				.AddParser<AssetDefId>((str) => Id.AssetDef(str))
				.AddParser<UnitDefId>((str) => Id.UnitDef(str))
				.AddParser<ResourceDefId>((str) => Id.ResDef(str))
				.AddParser<PlayerTypeDefId>((str) => Id.PlayerType(str))
				.AddParser<UnitId>((str) => Id.UnitId(Guid.Parse(str)))
				.Build();
		}
	}
}
