namespace ZWave;

// Taken from this article, with support for disposable:
// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
internal sealed class AsyncAutoResetEvent : IDisposable
{
    private readonly Queue<TaskCompletionSource> _waits = new Queue<TaskCompletionSource>();

    private bool _signaled;

    public void Dispose()
    {
        while (_waits.Count > 0)
        {
            var tcs = _waits.Dequeue();
            tcs.SetCanceled();
        }
    }

    public Task WaitAsync()
    {
        lock (_waits)
        {
            if (_signaled)
            {
                _signaled = false;
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource();
                _waits.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }

    public void Set()
    {
        TaskCompletionSource? toRelease = null;
        lock (_waits)
        {
            if (_waits.Count > 0)
            {
                toRelease = _waits.Dequeue();
            }
            else if (!_signaled)
            {
                _signaled = true;
            }
        }

        if (toRelease != null)
        {
            toRelease.SetResult();
        }
    }
}
