using Microsoft.Extensions.Logging;
using SQLite;

namespace Sgr.Frontend.Mobile.Outbox;

public sealed class SqlitePendingOperationStore : IPendingOperationStore
{
    private readonly ILogger<SqlitePendingOperationStore> _logger;
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _conn;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SqlitePendingOperationStore(ILogger<SqlitePendingOperationStore> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "sgr-outbox.db");
    }

    public async Task InitializeAsync()
    {
        if (_conn is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_conn is not null) return;
            _conn = new SQLiteAsyncConnection(_dbPath,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
            await _conn.CreateTableAsync<PendingOperation>();
            _logger.LogInformation("Outbox SQLite ready at {Path}.", _dbPath);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> EnqueueAsync(string surveyId, string eventsJson, DateTime now)
    {
        await EnsureInitAsync();
        var op = new PendingOperation
        {
            Id = Guid.NewGuid().ToString("N"),
            SurveyId = surveyId,
            EventsJson = eventsJson,
            Status = OutboxStatus.Pending,
            Attempts = 0,
            LastError = null,
            NextRetryAt = now,            // listo para enviarse ya
            CreatedAt = now,
            LastAttemptAt = null,
            SentAt = null,
        };
        await _conn!.InsertAsync(op);
        return op.Id;
    }

    public async Task<IReadOnlyList<PendingOperation>> GetReadyAsync(DateTime now, int limit = 20)
    {
        await EnsureInitAsync();
        var rows = await _conn!.Table<PendingOperation>()
            .Where(p => (p.Status == OutboxStatus.Pending || p.Status == OutboxStatus.Error)
                     && p.NextRetryAt <= now)
            .OrderBy(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
        return rows;
    }

    public async Task<IReadOnlyList<PendingOperation>> GetAllAsync()
    {
        await EnsureInitAsync();
        var rows = await _conn!.Table<PendingOperation>()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return rows;
    }

    public async Task<PendingOperation?> GetByIdAsync(string id)
    {
        await EnsureInitAsync();
        return await _conn!.Table<PendingOperation>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task MarkInFlightAsync(string id, DateTime now)
    {
        await EnsureInitAsync();
        await _conn!.ExecuteAsync(
            "UPDATE PendingOperation SET Status = ?, LastAttemptAt = ? WHERE Id = ?",
            OutboxStatus.InFlight, now, id);
    }

    public async Task MarkSentAsync(string id, DateTime now)
    {
        await EnsureInitAsync();
        await _conn!.ExecuteAsync(
            "UPDATE PendingOperation SET Status = ?, SentAt = ?, LastError = NULL WHERE Id = ?",
            OutboxStatus.Sent, now, id);
    }

    public async Task MarkErrorAsync(string id, string error, int attempts, DateTime? nextRetryAt, bool terminal, DateTime now)
    {
        await EnsureInitAsync();
        var status = terminal ? OutboxStatus.TerminalError : OutboxStatus.Error;
        var nextRetry = nextRetryAt ?? DateTime.MaxValue;
        await _conn!.ExecuteAsync(
            "UPDATE PendingOperation SET Status = ?, Attempts = ?, LastError = ?, NextRetryAt = ?, LastAttemptAt = ? WHERE Id = ?",
            status, attempts, error, nextRetry, now, id);
    }

    public async Task<int> CountAsync(string status)
    {
        await EnsureInitAsync();
        return await _conn!.Table<PendingOperation>().CountAsync(p => p.Status == status);
    }

    public async Task RescheduleNowAsync(string id, DateTime now)
    {
        await EnsureInitAsync();
        await _conn!.ExecuteAsync(
            "UPDATE PendingOperation SET Status = ?, NextRetryAt = ? WHERE Id = ?",
            OutboxStatus.Pending, now, id);
    }

    private Task EnsureInitAsync() => _conn is null ? InitializeAsync() : Task.CompletedTask;
}
