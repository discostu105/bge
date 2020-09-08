using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrowserGameEngine.GameModel {
	[TypeConverter(typeof(PlayerId))]
	public record PlayerId(string Id) {
		public override string ToString() => Id;
	}

	public static class PlayerIdFactory {
		public static PlayerId Create(string id) {
			return new PlayerId(id);
		}
	}

	public class PlayerIdConverter : JsonConverter<PlayerId> {
		[return: MaybeNull]
		public override PlayerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			return PlayerIdFactory.Create(reader.GetString());
		}

		public override void Write(Utf8JsonWriter writer, PlayerId value, JsonSerializerOptions options) {
			writer.WriteStringValue(value.Id);
		}
	}
}
