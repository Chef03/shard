using System;
using System.Collections.Generic;
using System.Linq;

namespace Shard;

public class ScoreManager
{
    private readonly object syncRoot = new();
    private readonly ScoreFileStore fileStore;
    private readonly List<WinningTimeEntry> winningTimes = new();
    private bool loaded;

    internal ScoreManager(string rootDirectory)
    {
        fileStore = new ScoreFileStore(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)));
    }

    public void Load()
    {
        lock (syncRoot)
        {
            winningTimes.Clear();
            winningTimes.AddRange(fileStore.Load());
            loaded = true;
        }
    }

    public void Save()
    {
        lock (syncRoot)
        {
            EnsureLoaded();
            fileStore.Save(winningTimes);
        }
    }

    public void RecordWinningTime(ScoreBoardKey board, TimeSpan winningTime)
    {
        lock (syncRoot)
        {
            EnsureLoaded();
            winningTimes.Add(new WinningTimeEntry(
                board.GameId,
                board.ModeId,
                Math.Max(0L, (long)winningTime.TotalMilliseconds),
                DateTimeOffset.UtcNow));
        }
    }

    public IReadOnlyList<WinningTimeEntry> GetWinningTimes(ScoreBoardKey board, int limit = 10)
    {
        lock (syncRoot)
        {
            EnsureLoaded();
            if (limit <= 0)
            {
                return Array.Empty<WinningTimeEntry>();
            }

            return winningTimes
                .Where(entry => entry.BoardKey == board)
                .OrderBy(entry => entry.DurationMs)
                .ThenBy(entry => entry.RecordedAtUtc)
                .Take(limit)
                .ToList();
        }
    }

    private void EnsureLoaded()
    {
        if (!loaded)
        {
            Load();
        }
    }
}
