using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using KellySharp;

namespace BrowserGameEngine.Persistence {
	public class GameStateJsonSerializer {
		public byte[] Serialize(WorldStateImmutable worldStateImmutable) {
			return JsonSerializer.SerializeToUtf8Bytes<WorldStateImmutable>(worldStateImmutable, GetOptions());
		}

		public WorldStateImmutable Deserialize(byte[] blob) {
			var result = JsonSerializer.Deserialize<WorldStateImmutable>(blob, GetOptions());
			if (result is null) throw new InvalidDataException("Deserialized world state is null — blob may be empty or corrupted.");
			return result;
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
				.AddParser<TechNodeId>((str) => Id.TechNode(str))
				.AddParser<UnitId>((str) => Id.UnitId(Guid.Parse(str)))
				.AddParser<AllianceId>((str) => AllianceIdFactory.Create(str))
				.AddParser<MessageId>((str) => new MessageId(Guid.Parse(str)))
				.Build();
		}
	}
}
