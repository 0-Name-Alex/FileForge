using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FileForge.Platform.Windows.SingleInstance;

/// <summary>
/// Enforces a single FileForge instance per user session and forwards
/// startup arguments (e.g., from Explorer context menu) over a named pipe.
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private bool _ownsMutex;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public SingleInstanceService(string appName = "FileForge")
    {
        var identity = $"{Environment.UserDomainName}\\{Environment.UserName}|{GetSessionId()}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        var key = $"{appName}.{hash}";
        _mutexName = @"Local\" + key;
        _pipeName = key;
    }

    private static int GetSessionId()
    {
        try { return System.Diagnostics.Process.GetCurrentProcess().SessionId; }
        catch { return 0; }
    }

    public bool TryAcquire()
    {
        if (_ownsMutex) return true;

        if (_mutex is null)
        {
            _mutex = new Mutex(initiallyOwned: true, _mutexName, out var createdNew);
            if (createdNew)
            {
                _ownsMutex = true;
                return true;
            }
        }

        try
        {
            _ownsMutex = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
        }

        return _ownsMutex;
    }

    public void StartListening(Action<string[]> onActivated)
    {
        if (!_ownsMutex || _serverTask is not null) return;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _serverTask = Task.Run(() => ServeLoopAsync(onActivated, ct), ct);
    }

    private async Task ServeLoopAsync(Action<string[]> onActivated, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
                await using var writer = new StreamWriter(server, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(TimeSpan.FromSeconds(5));
                var line = await reader.ReadLineAsync(readCts.Token).ConfigureAwait(false);

                var msg = string.IsNullOrWhiteSpace(line)
                    ? null
                    : JsonSerializer.Deserialize<ActivationPayload>(line);

                onActivated(msg?.Args ?? []);
                await writer.WriteLineAsync("ok").ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> SendArgsToPrimaryAsync(string[] args, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                await using var client = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                var remaining = deadline - DateTime.UtcNow;
                await client.ConnectAsync(
                    TimeSpan.FromMilliseconds(Math.Clamp(remaining.TotalMilliseconds, 50, 1000)),
                    CancellationToken.None).ConfigureAwait(false);

                await using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
                using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);

                var payload = JsonSerializer.Serialize(new ActivationPayload(args));
                await writer.WriteLineAsync(payload).ConfigureAwait(false);
                var reply = await reader.ReadLineAsync().ConfigureAwait(false);
                return reply == "ok";
            }
            catch (Exception ex) when (ex is TimeoutException or IOException)
            {
                if (DateTime.UtcNow >= deadline) return false;
                await Task.Delay(120).ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _serverTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch { }

        _cts?.Dispose();
        if (_ownsMutex)
        {
            try { _mutex?.ReleaseMutex(); }
            catch (ApplicationException) { }
            _ownsMutex = false;
        }
        _mutex?.Dispose();
    }

    private sealed record ActivationPayload(string[] Args);
}
