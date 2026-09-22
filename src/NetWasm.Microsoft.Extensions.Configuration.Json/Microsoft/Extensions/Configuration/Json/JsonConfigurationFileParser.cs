using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json;

internal sealed class JsonConfigurationFileParser
{
    private readonly Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _paths = new();

    public static IDictionary<string, string?> Parse(Stream input) => new JsonConfigurationFileParser().ParseStream(input);

    private Dictionary<string, string?> ParseStream(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        using var reader = new StreamReader(input);
        using JsonDocument document = JsonDocument.Parse(reader.ReadToEnd(), new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new FormatException("The top-level JSON element must be an object.");
        VisitObject(document.RootElement);
        return _data;
    }

    private void VisitObject(JsonElement element)
    {
        bool empty = true;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            empty = false;
            Enter(property.Name);
            VisitValue(property.Value);
            _paths.Pop();
        }
        if (empty && _paths.Count != 0) _data[_paths.Peek()] = null;
    }

    private void VisitArray(JsonElement element)
    {
        int index = 0;
        foreach (JsonElement child in element.EnumerateArray())
        {
            Enter(index.ToString());
            VisitValue(child);
            _paths.Pop();
            index++;
        }
        if (index == 0 && _paths.Count != 0) _data[_paths.Peek()] = string.Empty;
    }

    private void VisitValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObject(value);
                return;
            case JsonValueKind.Array:
                VisitArray(value);
                return;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                string key = _paths.Peek();
                if (_data.ContainsKey(key)) throw new FormatException("A duplicate JSON configuration key was found: " + key);
                _data[key] = value.ToString();
                return;
            case JsonValueKind.Null:
                _data[_paths.Peek()] = null;
                return;
            default:
                throw new FormatException("Unsupported JSON configuration token: " + value.ValueKind);
        }
    }

    private void Enter(string name) => _paths.Push(_paths.Count == 0 ? name : _paths.Peek() + ConfigurationPath.KeyDelimiter + name);
}
