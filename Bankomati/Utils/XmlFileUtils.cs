using Bankomati.Utils;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

internal class XmlFileUtils : FileUtils
{
    public XmlFileUtils(string path, string? defaultContent = null)
        : base(path, defaultContent) { }

    public override void Write<T>(T data)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)  
        };

        using XmlWriter writer = XmlWriter.Create(_path, settings);
        serializer.Serialize(writer, data);
    }

    public override T Read<T>()
    {
        if (!File.Exists(_path))
            return default;

        string content;
        using (StreamReader reader = new StreamReader(_path, Encoding.UTF8))
        {
            content = reader.ReadToEnd();
        }

       
        if (string.IsNullOrWhiteSpace(content))
            return default;

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using StringReader stringReader = new StringReader(content);
            return (T?)serializer.Deserialize(stringReader);
        }
        catch (InvalidOperationException)
        {
           
            return default;
        }
    }
}