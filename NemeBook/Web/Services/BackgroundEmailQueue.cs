using System.Threading.Channels;

namespace Web.Services;

public interface IBackgroundEmailQueue
{
    void Queue(Func<IServiceProvider, CancellationToken, Task> workItem);
}

public class BackgroundEmailQueue : BackgroundService, IBackgroundEmailQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue = Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundEmailQueue> _logger;

    public BackgroundEmailQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundEmailQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Queue(Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        _queue.Writer.TryWrite(workItem);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email work item failed.");
            }
        }
    }
}
