using Microsoft.AspNetCore.SignalR;

namespace Meridian.Hubs;

public class EnrollmentHub : Hub
{
    public Task Subscribe(string operationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, operationId);

    public Task Unsubscribe(string operationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
}
