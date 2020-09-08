# Dictionary JSON Converter

In transitioning from `Newtonsoft.Json` to `System.Text.Json`, users lost the ability convert dictionaries with non-`string` keys. This little library is intended to restore some of that functionality. It works with the following dictionary types:

- `IDictionary<TKey, TValue>`
- `Dictionary<TKey, TValue>`
- `SortedDictionary<TKey, TValue>`
- `ImmutableDictionary<TKey, TValue>`
- `ImmutableSortedDictionary<TKey, TValue>`

By default, it supports converting the following `TKey` types:

- Integer types: `sbyte`, `short`, `int`, `long`, `BigInteger`
- Unsigned integer types: `byte`, `ushort`, `uint`, `ulong`
- Floating point types: `float`, `double`, `decimal`
- `Guid`

Support for more `TKey` types can be added by the user.

## Basic Usage

To understand the problem itself, consider the following code.

```C#
var raw = "{\"49fc2162-744a-4a42-b685-ea1e30ce2a2f\": 99}";
var dictionary = JsonSerializer.Deserialize<Dictionary<Guid, int>>(raw);
```

This will throw a `NotSupportedException` in response to the fact that the dictionary specifies `Guid` for its `TKey`. This generally makes sense as JSON objects can only ever have strings for keys. However, it places unnecessary burden on the user to convert everything in two steps: first to string keys, and then parsed to some other type.

```C#
var options = new JsonSerializerOptions();
options.Converters.Add(DictionaryJsonConverterFactory.Default);
var raw = "{\"49fc2162-744a-4a42-b685-ea1e30ce2a2f\": 99}";
// Remember to add the options!
var dictionary = JsonSerializer.Deserialize<Dictionary<Guid, int>>(raw, options);
```

The dictionary now deserializes successfully.

## Advanced Usage

The default JSON converter factory adds support for many basic .NET types, but this behavior can be altered. Users can add support for custom types or even override the existing types. In order to accomplish this, a custom factory must be built.

```C#
var factory = new DictionaryJsonConverterFactoryBuilder()
    .AddDefaults() // Add support for the basic types listed above.
    .AddParser(MyCustomType.Parse) // Specify the method that will accept a string and return your type.
    .AddParser(SomeOtherType.Parse) // Chain the calls together!
    .AddSerializer((SomeOtherType sot) => sot.ToString(formatSettings, moreFormatSettings)) // Override the other direction.
    .Build(); // Build the actual factory.

var options = new JsonSerializerOptions();
options.Converters.Add(factory); // Ready to roll!
```
