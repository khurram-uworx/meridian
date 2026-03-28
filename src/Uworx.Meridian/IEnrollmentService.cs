using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;

namespace Uworx.Meridian;

public interface IEnrollmentQueue
{
    ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}

public interface IEnrollmentService
{
    Task<Enrollment> EnrollAsync(
        string learnerEmail,
        CourseSourceLocator source,
        Func<EnrollmentProgressUpdate, Task>? onProgress = null);

    /// <summary>
    /// Gets all enrollments for a specific learner.
    /// </summary>
    /// <param name="learnerId">The ID of the learner.</param>
    /// <returns>A list of enrollments including the associated course data.</returns>
    Task<IEnumerable<Enrollment>> GetEnrollmentsByLearnerIdAsync(int learnerId);
}
