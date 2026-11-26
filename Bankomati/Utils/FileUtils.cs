#nullable enable
using Bankomati.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Bankomati.Utils;

internal abstract class FileUtils : IFileUtils
{
    protected readonly string _path;

    protected FileUtils(string path, string? defaultContent = null)
    {
        _path = path;

        if (!File.Exists(path))
        {
            using StreamWriter writer = new StreamWriter(path);
            if (defaultContent != null)
                writer.Write(defaultContent);
        }
    }

    protected string ReadAllText()
    {
        using StreamReader reader = new StreamReader(_path);
        return reader.ReadToEnd();
    }

    protected void WriteAllText(string content)
    {
        using StreamWriter writer = new StreamWriter(_path);
        writer.Write(content);
    }

    public abstract void Write<T>(T data);
    public abstract T Read<T>();

    public virtual async Task WriteAsync<T>(T data)
    {
        using StreamWriter writer = new StreamWriter(_path);
        string text = data is null ? string.Empty : data.ToString() ?? string.Empty;
        await writer.WriteAsync(text);
    }

    public virtual async Task<T> ReadAsync<T>()
    {
        using StreamReader reader = new StreamReader(_path);
        string content = await reader.ReadToEndAsync();
        return content is T value ? value : default!;
    }
}