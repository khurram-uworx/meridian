using Uworx.Meridian.CourseSource;

namespace Uworx.Meridian;

public sealed record EnrollmentOperationEventSnapshot(
    DateTime CreatedAtUtc,
    string Stage,
    string Message,
    bool IsWarning,
    int? ProgressPercent);

public sealed record EnrollmentOperationSnapshot(
    Guid Id,
    string LearnerEmail,
    string SourceType,
    string SourceUri,
    string? SubPath,
    string Status,
    string CurrentStage,
    string CurrentMessage,
    bool IsWarning,
    int? ProgressPercent,
    int? EnrollmentId,
    string? JiraEpicKey,
    string? ErrorMessage,
    DateTime StartedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<EnrollmentOperationEventSnapshot> Events);

public sealed record EnrollmentProgressUpdate(
    string Stage,
    string Message,
    int? ProgressPercent = null,
    bool IsWarning = false);

public interface IEnrollmentOperationService
{
    Task<EnrollmentOperationSnapshot> CreateQueuedAsync(string learnerEmail, CourseSourceLocator source);
    Task<EnrollmentOperationSnapshot?> GetSnapshotAsync(Guid operationId);
    Task MarkInProgressAsync(Guid operationId, EnrollmentProgressUpdate update);
    Task AppendProgressAsync(Guid operationId, EnrollmentProgressUpdate update);
    Task MarkCompletedAsync(Guid operationId, int enrollmentId, string jiraEpicKey);
    Task MarkFailedAsync(Guid operationId, string errorMessage);
}
