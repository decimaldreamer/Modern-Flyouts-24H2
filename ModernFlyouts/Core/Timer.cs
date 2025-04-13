using System;
using System.Threading;
using System.Threading.Tasks;

namespace ModernFlyouts.Core
{
    public class Timer
    {
        private readonly int interval;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning;

        public event EventHandler Tick;

        public Timer(int interval)
        {
            this.interval = interval;
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(interval, cancellationTokenSource.Token);
                    if (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Tick?.Invoke(this, EventArgs.Empty);
                    }
                }
            }, cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
} 