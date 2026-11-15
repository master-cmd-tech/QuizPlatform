using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuizPlatform.Core.Content;
using QuizPlatform.Core.Domain;

namespace QuizPlatform.Web.Services;

public interface IDataStore
{
    /// <summary>The latest immutable snapshot (lock-free reads).</summary>
    AppData Current { get; }

    /// <summary>Applies a pure function to the snapshot, persists the result, then publishes it.</summary>
    Task<AppData> UpdateAsync(Func<AppData, AppData> change, CancellationToken ct = default);
}

/// <summary>
/// File-backed store. Reads never block (immutable snapshot); writes are serialized with a semaphore,
/// written atomically (temp file + move) and published only after they succeed.
/// A corrupt file is quarantined and replaced by the seed data instead of crashing the app.
/// </summary>
public sealed class JsonDataStore : IDataStore, IDisposable
{
    private readonly string _path;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppData _current;

    public JsonDataStore(string path, ILogger? logger = null)
    {
        _path = path;
        _logger = logger ?? NullLogger.Instance;
        _current = Load();
    }

    public AppData Current => Volatile.Read(ref _current);

    public async Task<AppData> UpdateAsync(Func<AppData, AppData> change, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var next = change(_current);
            await WriteAsync(next, ct);
            Volatile.Write(ref _current, next);
            return next;
        }
        finally
        {
            _gate.Release();
        }
    }

    private AppData Load()
    {
        if (!File.Exists(_path))
        {
            var seed = SeedData.Initial();
            TryWriteSync(seed);
            return seed;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppData>(json, JsonDefaults.Options)
                   ?? throw new JsonException("The data file is empty.");
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogError(ex, "Data file {Path} is unreadable; quarantining it and re-seeding.", _path);
            try { File.Move(_path, $"{_path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}"); }
            catch (IOException) { /* best effort */ }
            var seed = SeedData.Initial();
            TryWriteSync(seed);
            return seed;
        }
    }

    private void TryWriteSync(AppData data)
    {
        try
        {
            EnsureDirectory();
            File.WriteAllText(_path, JsonSerializer.Serialize(data, JsonDefaults.Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not write the initial data file {Path}; running in memory.", _path);
        }
    }

    private async Task WriteAsync(AppData data, CancellationToken ct)
    {
        EnsureDirectory();
        var tmp = _path + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(data, JsonDefaults.Options), ct);
        File.Move(tmp, _path, overwrite: true);
    }

    private void EnsureDirectory()
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    public void Dispose() => _gate.Dispose();
}
