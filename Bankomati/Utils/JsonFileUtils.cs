#nullable enable
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Bankomati.Utils;

internal class JsonFileUtils : FileUtils
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public JsonFileUtils(string path, string defaultContent = "{}")
        : base(path, defaultContent) { }

    public override void Write<T>(T data)
    {
        string json = JsonSerializer.Serialize(data, _options);
        WriteAllText(json);
    }

    public override T Read<T>()
    {
        string json = ReadAllText();
        if (string.IsNullOrWhiteSpace(json))
            return default!;

        return JsonSerializer.Deserialize<T>(json, _options)!;
    }

    public override async Task WriteAsync<T>(T data)
    {
        string json = JsonSerializer.Serialize(data, _options);
        using StreamWriter writer = new StreamWriter(_path);
        await writer.WriteAsync(json);
    }

    public override async Task<T> ReadAsync<T>()
    {
        using StreamReader reader = new StreamReader(_path);
        string json = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(json))
            return default!;

        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}