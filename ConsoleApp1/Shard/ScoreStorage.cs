using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Shard;

internal sealed class ScoreFileStore
{
    private const string WinningTimesFileName = "winning_times.csv";
    private readonly string rootDirectory;

    public ScoreFileStore(string rootDirectory)
    {
        this.rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
    }

    public List<WinningTimeEntry> Load()
    {
        var entries = new List<WinningTimeEntry>();
        var path = Path.Combine(rootDirectory, WinningTimesFileName);
        if (!File.Exists(path))
        {
            return entries;
        }

        using var reader = new StreamReader(path);
        var header = reader.ReadLine();
        if (!string.Equals(header, "game_id,mode_id,duration_ms,recorded_at_utc", StringComparison.OrdinalIgnoreCase))
        {
            Debug.getInstance().log("Skipping winning_times.csv due to header mismatch.", Debug.DEBUG_LEVEL_WARNING);
            return entries;
        }

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',');
            if (columns.Length != 4
                || !long.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationMs)
                || !DateTimeOffset.TryParse(columns[3], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var recordedAtUtc))
            {
                Debug.getInstance().log($"Skipping malformed winning time row: {line}", Debug.DEBUG_LEVEL_WARNING);
                continue;
            }

            try
            {
                entries.Add(new WinningTimeEntry(columns[0], columns[1], durationMs, recordedAtUtc));
            }
            catch (ArgumentException)
            {
                Debug.getInstance().log($"Skipping malformed winning time row: {line}", Debug.DEBUG_LEVEL_WARNING);
            }
        }

        return entries;
    }

    public void Save(IReadOnlyList<WinningTimeEntry> entries)
    {
        Directory.CreateDirectory(rootDirectory);

        var targetPath = Path.Combine(rootDirectory, WinningTimesFileName);
        var tempPath = $"{targetPath}.tmp";
        var builder = new StringBuilder();
        builder.AppendLine("game_id,mode_id,duration_ms,recorded_at_utc");

        foreach (var entry in entries
            .OrderBy(entry => entry.GameId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.ModeId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.DurationMs)
            .ThenBy(entry => entry.RecordedAtUtc))
        {
            builder.Append(entry.GameId);
            builder.Append(',');
            builder.Append(entry.ModeId);
            builder.Append(',');
            builder.Append(entry.DurationMs.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.RecordedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine();
        }

        File.WriteAllText(tempPath, builder.ToString(), Encoding.UTF8);
        File.Move(tempPath, targetPath, true);
    }
}
