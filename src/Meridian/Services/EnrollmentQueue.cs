using System.Threading.Channels;
using Uworx.Meridian;

namespace Meridian.Services;

public class EnrollmentQueue : IEnrollmentQueue
{
    readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        channel.Writer.WriteAsync(operationId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
