using System.Threading.Channels;
using Aimy.Core.Application.DTOs.Upload;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Infrastructure.Messaging;

public class InMemoryUploadChannel : IUploadQueueReader, IUploadQueueWriter
{
    private readonly Channel<UploadToProcess> _channel = Channel.CreateBounded<UploadToProcess>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    public IAsyncEnumerable<UploadToProcess> ReadAllAsync(CancellationToken ct = default) => _channel.Reader.ReadAllAsync(ct);

    public async Task WriteAsync(UploadToProcess upload, CancellationToken ct = default) => await _channel.Writer.WriteAsync(upload, ct);
}