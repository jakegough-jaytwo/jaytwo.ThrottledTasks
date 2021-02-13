using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.ThrottledTasks
{
    public class RateLimiter : IDisposable
    {
        private readonly Random _random;
        private readonly TimeSpan _minInterval;
        private readonly TimeSpan _maxInterval;
        private readonly SemaphoreSlim _fooSemaphore;

        public RateLimiter(TimeSpan interval)
            : this(interval, 1)
        {
        }

        public RateLimiter(TimeSpan interval, int capacity)
            : this(interval, interval, capacity)
        {
        }

        public RateLimiter(TimeSpan minInterval, TimeSpan maxInterval)
            : this(minInterval, maxInterval, 1)
        {
        }

        public RateLimiter(TimeSpan minInterval, TimeSpan maxInterval, int capacity)
        {
            if (minInterval == TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(minInterval)} must be greater than zero");
            }

            if (minInterval > maxInterval)
            {
                throw new ArgumentException($"{nameof(maxInterval)} must be greater than or equal to {nameof(minInterval)}");
            }

            if (capacity < 1)
            {
                throw new ArgumentException($"{nameof(capacity)} must be greater than or equal to 1");
            }

            _random = new Random();
            _minInterval = minInterval;
            _maxInterval = maxInterval;
            _fooSemaphore = new SemaphoreSlim(capacity, capacity);
        }

        public async Task WaitAsync()
        {
            var delay = GetDelay();
            await _fooSemaphore.WaitAsync();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await DelayAndReleaseSemaphore(delay));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void Dispose()
        {
            _fooSemaphore.Dispose();
        }

        private async Task DelayAndReleaseSemaphore(TimeSpan delay)
        {
            try
            {
                await Task.Delay(delay);
            }
            finally
            {
                try
                {
                    _fooSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // on the chance we've already been disposed
                }
            }
        }

        private TimeSpan GetDelay()
        {
            if (_minInterval == _maxInterval)
            {
                return _minInterval;
            }

            var randomDouble = _random.NextDouble();
            var splayMs = _maxInterval.TotalMilliseconds - _minInterval.TotalMilliseconds;
            var randomDelayMs = _minInterval.TotalMilliseconds + (randomDouble * splayMs);
            return TimeSpan.FromMilliseconds(randomDelayMs);
        }
    }
}
