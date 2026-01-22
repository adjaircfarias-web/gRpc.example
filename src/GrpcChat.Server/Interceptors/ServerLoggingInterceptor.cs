using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace GrpcChat.Server.Interceptors;

public class ServerLoggingInterceptor : Interceptor
{
    private readonly ILogger<ServerLoggingInterceptor> _logger;

    public ServerLoggingInterceptor(ILogger<ServerLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Method;
        var peer = context.Peer;

        _logger.LogInformation("Starting unary call {Method} from {Peer}", method, peer);

        try
        {
            var response = await continuation(request, context);
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed unary call {Method} in {Duration}ms",
                method, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error in unary call {Method} after {Duration}ms",
                method, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Method;
        var peer = context.Peer;
        var messageCount = 0;

        _logger.LogInformation(
            "Starting server streaming call {Method} from {Peer}",
            method, peer);

        var countingStream = new CountingServerStreamWriter<TResponse>(
            responseStream,
            () => Interlocked.Increment(ref messageCount));

        try
        {
            await continuation(request, countingStream, context);
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed server streaming call {Method} in {Duration}ms. Messages sent: {MessageCount}",
                method, stopwatch.ElapsedMilliseconds, messageCount);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "Server streaming call {Method} cancelled after {Duration}ms. Messages sent: {MessageCount}",
                method, stopwatch.ElapsedMilliseconds, messageCount);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error in server streaming call {Method} after {Duration}ms. Messages sent: {MessageCount}",
                method, stopwatch.ElapsedMilliseconds, messageCount);

            throw;
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Method;
        var peer = context.Peer;
        var messagesReceived = 0;
        var messagesSent = 0;

        _logger.LogInformation(
            "Starting bidirectional streaming call {Method} from {Peer}",
            method, peer);

        var countingRequestStream = new CountingAsyncStreamReader<TRequest>(
            requestStream,
            () => Interlocked.Increment(ref messagesReceived));

        var countingResponseStream = new CountingServerStreamWriter<TResponse>(
            responseStream,
            () => Interlocked.Increment(ref messagesSent));

        try
        {
            await continuation(countingRequestStream, countingResponseStream, context);
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed bidirectional streaming call {Method} in {Duration}ms. Messages received: {MessagesReceived}, Messages sent: {MessagesSent}",
                method, stopwatch.ElapsedMilliseconds, messagesReceived, messagesSent);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "Bidirectional streaming call {Method} cancelled after {Duration}ms. Messages received: {MessagesReceived}, Messages sent: {MessagesSent}",
                method, stopwatch.ElapsedMilliseconds, messagesReceived, messagesSent);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error in bidirectional streaming call {Method} after {Duration}ms. Messages received: {MessagesReceived}, Messages sent: {MessagesSent}",
                method, stopwatch.ElapsedMilliseconds, messagesReceived, messagesSent);

            throw;
        }
    }
}
