namespace EasySave.Core.Services;

using EasySave.Core.Interfaces;
using EasySave.Core.Models;
using System.Collections.Generic;

// Format backup job lists from input
public class BackupListFormatter
{
    private static ILocalizationService _localization = null!;

    public BackupListFormatter()
    {
    }

    // Format and validate job list from arguments
    // Returns (success, message, list of jobs)
    public (bool, string, List<IJob>) FormatJobList(string arguments, IJobManager manager)
    {
        _localization = new LocalizationService();
        var indexes = new HashSet<int>();
        var parts = arguments.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            // Range: X-Y
            if (trimmed.Contains('-'))
            {
                var bounds = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (bounds.Length != 2 ||
                    !int.TryParse(bounds[0], out int start) ||
                    !int.TryParse(bounds[1], out int end))
                {
                    return (false, _localization.GetString("invalid_choice"), new List<IJob>());
                }
                if (start > end)
                    (start, end) = (end, start);
                for (int i = start; i <= end; i++)
                    indexes.Add(i);
            }
            // Single index
            else
            {
                if (!int.TryParse(trimmed, out int index))
                {
                    return (false, _localization.GetString("invalid_choice"), new List<IJob>());
                }
                indexes.Add(index);
            }
        }

        // Validate bounds
        foreach (var index in indexes)
        {
            if (index < 1 || index > manager.Jobs.Count || index > manager.MaxJobs)
            {
                return (false, _localization.GetString("error_not_found"), new List<IJob>());
            }
        }

        // Get jobs
        var jobsToExecute = indexes
            .OrderBy(i => i)
            .Select(i => manager.GetJob(i))
            .ToList();

        return (true, "", jobsToExecute);
    }
}