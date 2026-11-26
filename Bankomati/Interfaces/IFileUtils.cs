namespace Bankomati.Interfaces;

internal interface IFileUtils
{
    void Write<T>(T data);
    T? Read<T>();
    Task WriteAsync<T>(T data);
    Task<T?> ReadAsync<T>();
}