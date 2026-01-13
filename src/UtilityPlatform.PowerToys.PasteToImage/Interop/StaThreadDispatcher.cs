using System.Windows.Forms;

namespace UtilityPlatform.PowerToys.PasteToImage.Interop;

public sealed class StaThreadDispatcher : IDisposable
{
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);

    private Control? _invoker;
    private bool _isDisposed;

    public StaThreadDispatcher(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        _thread = new(RunMessageLoop)
        {
            IsBackground = true,
            Name = name
        };

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        _ready.Wait();
    }

    public Task RunAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(StaThreadDispatcher));
        }

        if (_invoker is null)
        {
            throw new InvalidOperationException("STA dispatcher is not ready.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _invoker.BeginInvoke((MethodInvoker)(() =>
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
            }));
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        try
        {
            if (_invoker is not null)
            {
                _invoker.BeginInvoke((MethodInvoker)Application.ExitThread);
            }
        }
        catch
        {
        }

        _ready.Dispose();
    }

    private void RunMessageLoop()
    {
        try
        {
            _invoker = new Control();
            _ = _invoker.Handle;
        }
        finally
        {
            _ready.Set();
        }

        Application.Run(new ApplicationContext());

        _invoker?.Dispose();
        _invoker = null;
    }
}

