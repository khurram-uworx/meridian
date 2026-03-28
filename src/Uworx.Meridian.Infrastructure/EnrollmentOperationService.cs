using Microsoft.EntityFrameworkCore;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Uworx.Meridian.Infrastructure;

public class EnrollmentOperationService : IEnrollmentOperationService
{
    readonly MeridianDbContext dbContext;

    public EnrollmentOperationService(MeridianDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<EnrollmentOperationSnapshot> CreateQueuedAsync(string learnerEmail, CourseSourceLocator source)
    {
        var now = DateTime.UtcNow;
        var operation = new EnrollmentOperation
        {
            Id = Guid.NewGuid(),
            LearnerEmail = learnerEmail,
            SourceType = source.SourceType.ToString(),
            SourceUri = source.Uri,
            SubPath = source.SubPath,
            Status = "queued",
            CurrentStage = "queued",
            CurrentMessage = "Enrollment request accepted.",
            ProgressPercent = 2,
            IsWarning = false,
            StartedAtUtc = now,
            UpdatedAtUtc = now
        };

        operation.Events.Add(new EnrollmentOperationEvent
        {
            CreatedAtUtc = now,
            Stage = operation.CurrentStage,
            Message = operation.CurrentMessage,
            IsWarning = operation.IsWarning,
            ProgressPercent = operation.ProgressPercent
        });

        dbContext.EnrollmentOperations.Add(operation);
        await dbContext.SaveChangesAsync();

        return Map(operation);
    }

    public async Task<EnrollmentOperationSnapshot?> GetSnapshotAsync(Guid operationId)
    {
        var operation = await dbContext.EnrollmentOperations
            .Include(e => e.Events)
            .SingleOrDefaultAsync(e => e.Id == operationId);

        return operation == null ? null : Map(operation);
    }

    public Task MarkInProgressAsync(Guid operationId, EnrollmentProgressUpdate update) =>
        SetStateAsync(operationId, "in_progress", update, null, null, null);

    public Task AppendProgressAsync(Guid operationId, EnrollmentProgressUpdate update) =>
        SetStateAsync(operationId, "in_progress", update, null, null, null);

    public Task MarkCompletedAsync(Guid operationId, int enrollmentId, string jiraEpicKey) =>
        SetStateAsync(
            operationId,
            "completed",
            new EnrollmentProgressUpdate("completed", "Enrollment completed successfully.", 100),
            enrollmentId,
            jiraEpicKey,
            null);

    public Task MarkFailedAsync(Guid operationId, string errorMessage) =>
        SetStateAsync(
            operationId,
            "failed",
            new EnrollmentProgressUpdate("failed", "Enrollment failed. Review error details and retry.", 100, true),
            null,
            null,
            errorMessage);

    async Task SetStateAsync(
        Guid operationId,
        string status,
        EnrollmentProgressUpdate update,
        int? enrollmentId,
        string? jiraEpicKey,
        string? errorMessage)
    {
        var operation = await dbContext.EnrollmentOperations
            .Include(e => e.Events)
            .SingleOrDefaultAsync(e => e.Id == operationId);

        if (operation == null)
            return;

        var now = DateTime.UtcNow;
        operation.Status = status;
        operation.CurrentStage = update.Stage;
        operation.CurrentMessage = update.Message;
        operation.ProgressPercent = update.ProgressPercent;
        operation.IsWarning = update.IsWarning;
        operation.UpdatedAtUtc = now;

        if (enrollmentId.HasValue)
            operation.EnrollmentId = enrollmentId.Value;

        if (!string.IsNullOrWhiteSpace(jiraEpicKey))
            operation.JiraEpicKey = jiraEpicKey;

        if (!string.IsNullOrWhiteSpace(errorMessage))
            operation.ErrorMessage = errorMessage;

        if (status is "completed" or "failed")
            operation.CompletedAtUtc = now;

        operation.Events.Add(new EnrollmentOperationEvent
        {
            CreatedAtUtc = now,
            Stage = update.Stage,
            Message = update.Message,
            IsWarning = update.IsWarning,
            ProgressPercent = update.ProgressPercent
        });

        await dbContext.SaveChangesAsync();
    }

    static EnrollmentOperationSnapshot Map(EnrollmentOperation operation) =>
        new(
            operation.Id,
            operation.LearnerEmail,
            operation.SourceType,
            operation.SourceUri,
            operation.SubPath,
            operation.Status,
            operation.CurrentStage,
            operation.CurrentMessage,
            operation.IsWarning,
            operation.ProgressPercent,
            operation.EnrollmentId,
            operation.JiraEpicKey,
            operation.ErrorMessage,
            operation.StartedAtUtc,
            operation.UpdatedAtUtc,
            operation.CompletedAtUtc,
            operation.Events
                .OrderBy(e => e.CreatedAtUtc)
                .Select(e => new EnrollmentOperationEventSnapshot(
                    e.CreatedAtUtc,
                    e.Stage,
                    e.Message,
                    e.IsWarning,
                    e.ProgressPercent))
                .ToList());
}
