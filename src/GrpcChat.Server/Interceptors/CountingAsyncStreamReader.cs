using Grpc.Core;

namespace GrpcChat.Server.Interceptors;

internal class CountingAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IAsyncStreamReader<T> _inner;
    private readonly Action _onRead;

    public CountingAsyncStreamReader(IAsyncStreamReader<T> inner, Action onRead)
    {
        _inner = inner;
        _onRead = onRead;
    }

    public T Current => _inner.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        var result = await _inner.MoveNext(cancellationToken);
        if (result)
        {
            _onRead();
        }
        return result;
    }
}
