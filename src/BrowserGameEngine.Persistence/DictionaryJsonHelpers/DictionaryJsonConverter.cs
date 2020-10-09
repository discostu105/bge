using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

// Thank you, https://dev.to/thebuzzsaw/converting-dictionaries-in-system-text-json-474b

namespace KellySharp {
	public class DictionaryJsonConverter<TKey, TValue> :
		JsonConverter<Dictionary<TKey, TValue>?> where TKey : notnull {
		public static Dictionary<TKey, TValue>? Read(
			ref Utf8JsonReader reader,
			Converter<string, TKey> keyParser,
			JsonSerializerOptions options) {
			if (reader.TokenType == JsonTokenType.Null)
				return null;

			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException("Dictionary must be JSON object.");

			var result = new Dictionary<TKey, TValue>();

			while (true) {
				if (!reader.Read())
					throw new JsonException("Incomplete JSON object");

				if (reader.TokenType == JsonTokenType.EndObject)
					return result;

				var key = keyParser(reader.GetString());

				if (!reader.Read())
					throw new JsonException("Incomplete JSON object");

				var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
				result.Add(key, value);
			}
		}

		private readonly Converter<string, TKey> _keyParser;
		private readonly Converter<TKey, string> _keySerializer;

		public DictionaryJsonConverter(
			Converter<string, TKey> keyParser,
			Converter<TKey, string> keySerializer) {
			_keyParser = keyParser;
			_keySerializer = keySerializer;
		}

		public override Dictionary<TKey, TValue>? Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options) {
			return Read(ref reader, _keyParser, options);
		}

		public override void Write(
			Utf8JsonWriter writer,
			Dictionary<TKey, TValue>? value,
			JsonSerializerOptions options) {
			if (value is null) {
				writer.WriteNullValue();
			} else {
				writer.WriteStartObject();

				foreach (var pair in value) {
					writer.WritePropertyName(_keySerializer(pair.Key));
					JsonSerializer.Serialize(writer, pair.Value, options);
				}

				writer.WriteEndObject();
			}
		}
	}
}