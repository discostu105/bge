using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

// Thank you, https://dev.to/thebuzzsaw/converting-dictionaries-in-system-text-json-474b

namespace KellySharp {
	public class DictionaryJsonConverterFactoryBuilder {
		private readonly Dictionary<Type, Delegate> _parsers = new Dictionary<Type, Delegate>();
		private readonly Dictionary<Type, Delegate> _serializers = new Dictionary<Type, Delegate>();

		public DictionaryJsonConverterFactoryBuilder AddParser<T>(Converter<string, T> parser) {
			_parsers.Add(typeof(T), parser);
			return this;
		}

		public DictionaryJsonConverterFactoryBuilder AddSerializer<T>(Converter<T, string> serializer) {
			_serializers.Add(typeof(T), serializer);
			return this;
		}

		public DictionaryJsonConverterFactoryBuilder Add<T>(
			Converter<string, T> parser, Converter<T, string> serializer) {
			return AddParser(parser).AddSerializer(serializer);
		}

		public DictionaryJsonConverterFactoryBuilder SetParser<T>(Converter<string, T> parser) {
			_parsers[typeof(T)] = parser;
			return this;
		}

		public DictionaryJsonConverterFactoryBuilder SetSerializer<T>(Converter<T, string> serializer) {
			_serializers[typeof(T)] = serializer;
			return this;
		}

		public DictionaryJsonConverterFactoryBuilder Set<T>(
			Converter<string, T> parser, Converter<T, string> serializer) {
			return SetParser(parser).SetSerializer(serializer);
		}

		public DictionaryJsonConverterFactoryBuilder AddDefaults() {
			return this
				.AddParser(sbyte.Parse)
				.AddParser(short.Parse)
				.AddParser(int.Parse)
				.AddParser(long.Parse)
				.AddParser(byte.Parse)
				.AddParser(ushort.Parse)
				.AddParser(uint.Parse)
				.AddParser(ulong.Parse)
				.AddParser(BigInteger.Parse)
				.AddParser(float.Parse)
				.AddParser(double.Parse)
				.AddParser(decimal.Parse)
				.AddParser(Guid.Parse);
		}

		public DictionaryJsonConverterFactory Build() {
			return new DictionaryJsonConverterFactory(
				_parsers.ToImmutableDictionary(),
				_serializers.ToImmutableDictionary());
		}
	}
}