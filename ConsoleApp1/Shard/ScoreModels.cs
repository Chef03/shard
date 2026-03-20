using System;

namespace Shard;

public readonly struct ScoreBoardKey : IEquatable<ScoreBoardKey>
{
    public string GameId { get; }
    public string ModeId { get; }

    public ScoreBoardKey(string gameId, string modeId)
    {
        GameId = ScoreTextUtility.NormalizeRequiredValue(gameId, nameof(gameId));
        ModeId = ScoreTextUtility.NormalizeRequiredValue(modeId, nameof(modeId));
    }

    public bool Equals(ScoreBoardKey other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(GameId, other.GameId)
            && StringComparer.OrdinalIgnoreCase.Equals(ModeId, other.ModeId);
    }

    public override bool Equals(object obj)
    {
        return obj is ScoreBoardKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(GameId),
            StringComparer.OrdinalIgnoreCase.GetHashCode(ModeId));
    }

    public static bool operator ==(ScoreBoardKey left, ScoreBoardKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScoreBoardKey left, ScoreBoardKey right)
    {
        return !left.Equals(right);
    }
}

public sealed class WinningTimeEntry
{
    public string GameId { get; }
    public string ModeId { get; }
    public long DurationMs { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    public ScoreBoardKey BoardKey => new(GameId, ModeId);

    public WinningTimeEntry(string gameId, string modeId, long durationMs, DateTimeOffset recordedAtUtc)
    {
        GameId = ScoreTextUtility.NormalizeRequiredValue(gameId, nameof(gameId));
        ModeId = ScoreTextUtility.NormalizeRequiredValue(modeId, nameof(modeId));
        DurationMs = Math.Max(0, durationMs);
        RecordedAtUtc = recordedAtUtc.ToUniversalTime();
    }
}
