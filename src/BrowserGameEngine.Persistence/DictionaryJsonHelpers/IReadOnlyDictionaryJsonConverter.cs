using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// Thank you, https://dev.to/thebuzzsaw/converting-dictionaries-in-system-text-json-474b

namespace KellySharp {
	public class IReadOnlyDictionaryJsonConverter<TKey, TValue> :
		JsonConverter<IReadOnlyDictionary<TKey, TValue>?> where TKey : notnull {
		private readonly Converter<string, TKey> _keyParser;
		private readonly Converter<TKey, string> _keySerializer;

		public IReadOnlyDictionaryJsonConverter(
			Converter<string, TKey> keyParser,
			Converter<TKey, string> keySerializer) {
			_keyParser = keyParser;
			_keySerializer = keySerializer;
		}

		public override IReadOnlyDictionary<TKey, TValue>? Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options) {
			return ImmutableDictionaryJsonConverter<TKey, TValue>.Read(
				ref reader, _keyParser, options);
		}

		public override void Write(
			Utf8JsonWriter writer,
			IReadOnlyDictionary<TKey, TValue>? value,
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