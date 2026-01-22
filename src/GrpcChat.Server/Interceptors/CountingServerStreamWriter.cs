using Grpc.Core;

namespace GrpcChat.Server.Interceptors;

internal class CountingServerStreamWriter<T> : IServerStreamWriter<T>
{
    private readonly IServerStreamWriter<T> _inner;
    private readonly Action _onWrite;

    public CountingServerStreamWriter(IServerStreamWriter<T> inner, Action onWrite)
    {
        _inner = inner;
        _onWrite = onWrite;
    }

    public WriteOptions? WriteOptions
    {
        get => _inner.WriteOptions;
        set => _inner.WriteOptions = value;
    }

    public Task WriteAsync(T message)
    {
        _onWrite();
        return _inner.WriteAsync(message);
    }
}
