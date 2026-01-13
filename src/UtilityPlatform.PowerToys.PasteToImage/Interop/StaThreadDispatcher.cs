using System.Collections.Concurrent;

namespace UtilityPlatform.PowerToys.PasteToImage.Interop;

public sealed class StaThreadDispatcher : IDisposable
{
    private readonly BlockingCollection<Action> _workItems = [];
    private readonly Thread _thread;

    public StaThreadDispatcher(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        _thread = new(RunLoop)
        {
            IsBackground = true,
            Name = name
        };

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public Task RunAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _workItems.Add(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        _workItems.CompleteAdding();
        _workItems.Dispose();
    }

    private void RunLoop()
    {
        foreach (var item in _workItems.GetConsumingEnumerable())
        {
            item();
        }
    }
}

