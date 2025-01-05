using DSharpPlus;
using DSharpPlus.Clients;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotReproduction;

public class CatchingEventDispatcher(IServiceProvider serviceProvider, IOptions<EventHandlerCollection> handlers, 
    IClientErrorHandler errorHandler, ILogger<CatchingEventDispatcher> logger) : IEventDispatcher
{
    private bool _disposed;
    private readonly EventHandlerCollection _handlers = handlers.Value;
    
    public ValueTask DispatchAsync<T>(DiscordClient client, T eventArgs) where T : DiscordEventArgs
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        var general = _handlers[typeof(DiscordEventArgs)];
        var specific = _handlers[typeof(T)];
        if (general.Count == 0 && specific.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            var scope = serviceProvider.CreateScope();
            _ = Task.WhenAll(general.Concat(specific).Select(async handler =>
                {
                    try
                    {
                        await ((Func<DiscordClient, T, IServiceProvider, Task>)handler)(client, eventArgs,
                            scope.ServiceProvider);
                    }
                    catch (Exception e)
                    {
                        await errorHandler.HandleEventHandlerError(typeof(T).ToString(), e, (Delegate)handler, client,
                            eventArgs);
                    }
                }))
                .ContinueWith(_ => scope.Dispose());
        }
        catch (ObjectDisposedException ex)
        {
            // Ignore this
            logger.LogDebug(ex, "DisposedException in EventDispatcher");
        }
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        logger.LogDebug("EventDispatcher disposed. Any new event will not dispatch anymore.");
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}