using System.ComponentModel.DataAnnotations;

namespace LibraryTemplate.Api.Options;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool EnableOverdueReminders { get; init; }

    [Range(1, int.MaxValue)]
    public int OverdueReminderIntervalSeconds { get; init; } = 300;

    [Range(1, 500)]
    public int OverdueReminderBatchSize { get; init; } = 50;
}
