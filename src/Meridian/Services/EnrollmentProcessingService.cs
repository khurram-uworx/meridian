using Meridian.Hubs;
using Microsoft.AspNetCore.SignalR;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;

namespace Meridian.Services;

public class EnrollmentProcessingService : BackgroundService
{
    readonly IEnrollmentQueue queue;
    readonly IServiceScopeFactory scopeFactory;
    readonly IHubContext<EnrollmentHub> hubContext;
    readonly ILogger<EnrollmentProcessingService> logger;

    public EnrollmentProcessingService(
        IEnrollmentQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<EnrollmentHub> hubContext,
        ILogger<EnrollmentProcessingService> logger)
    {
        this.queue = queue;
        this.scopeFactory = scopeFactory;
        this.hubContext = hubContext;
        this.logger = logger;
    }

    async Task PublishSnapshotAsync(
        IEnrollmentOperationService operationService,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await operationService.GetSnapshotAsync(operationId);
        if (snapshot == null)
            return;

        await hubContext.Clients.Group(operationId.ToString())
            .SendAsync("EnrollmentProgress", snapshot, cancellationToken);
    }

    async Task<EnrollmentOperationSnapshot?> getOperationSnapshotAsync(Guid operationId)
    {
        using var scope = scopeFactory.CreateScope();
        var operationService = scope.ServiceProvider.GetRequiredService<IEnrollmentOperationService>();
        return await operationService.GetSnapshotAsync(operationId);
    }

    async Task updateAndPublishAsync(
        Guid operationId,
        Func<IEnrollmentOperationService, Task> updateAction,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var operationService = scope.ServiceProvider.GetRequiredService<IEnrollmentOperationService>();
        await updateAction(operationService);
        await PublishSnapshotAsync(operationService, operationId, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var operationId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                var operation = await getOperationSnapshotAsync(operationId);
                if (operation == null)
                {
                    logger.LogWarning("Enrollment operation {OperationId} not found.", operationId);
                    continue;
                }

                await updateAndPublishAsync(
                    operationId,
                    service => service.MarkInProgressAsync(
                        operationId,
                        new EnrollmentProgressUpdate(
                            "in_progress",
                            "Enrollment processing started. Keep this page open until completion.",
                            6,
                            true)),
                    stoppingToken);

                var source = new CourseSourceLocator(
                    Enum.Parse<CourseSourceType>(operation.SourceType, ignoreCase: true),
                    operation.SourceUri,
                    operation.SubPath);

                using var enrollmentScope = scopeFactory.CreateScope();
                var enrollmentService = enrollmentScope.ServiceProvider.GetRequiredService<IEnrollmentService>();

                var enrollment = await enrollmentService.EnrollAsync(
                    operation.LearnerEmail,
                    source,
                    async progress =>
                    {
                        await updateAndPublishAsync(
                            operationId,
                            service => service.AppendProgressAsync(operationId, progress),
                            stoppingToken);
                    });

                await updateAndPublishAsync(
                    operationId,
                    service => service.MarkCompletedAsync(operationId, enrollment.Id, enrollment.JiraEpicKey),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Enrollment operation {OperationId} failed.", operationId);

                try
                {
                    await updateAndPublishAsync(
                        operationId,
                        service => service.MarkFailedAsync(operationId, ex.Message),
                        stoppingToken);
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, "Failed to update failed status for operation {OperationId}.", operationId);
                }
            }
        }
    }
}
