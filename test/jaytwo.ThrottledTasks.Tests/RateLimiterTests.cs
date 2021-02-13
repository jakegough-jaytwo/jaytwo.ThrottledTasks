using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace jaytwo.ThrottledTasks.Tests
{
    public class RateLimiterTests
    {
        [Theory]
        [InlineData(1, 100)]
        [InlineData(2, 100)]
        [InlineData(5, 100)]
        [InlineData(10, 50)]
        [InlineData(50, 100)]
        public async Task WaitAsyncProducesExpectedDelays(int count, int intervalMs)
        {
            // arrange
            var outerTimer = Stopwatch.StartNew();

            using (var rateLimiter = new RateLimiter(TimeSpan.FromMilliseconds(intervalMs)))
            {
                // act
                for (int i = 0; i < count; i++)
                {
                    await rateLimiter.WaitAsync();
                }
            }

            outerTimer.Stop();

            // assert
            Assert.InRange(
                actual: outerTimer.Elapsed.TotalMilliseconds,
                low: (count - 1) * intervalMs,
                high: ((count - 1) * intervalMs) + (intervalMs / 2 * count));
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(2, 100, 1)]
        [InlineData(5, 100, 1)]
        [InlineData(1, 100, 2)]
        [InlineData(2, 100, 2)]
        [InlineData(5, 100, 2)]
        [InlineData(1, 100, 5)]
        [InlineData(2, 100, 5)]
        [InlineData(5, 100, 5)]
        [InlineData(10, 100, 5)]
        [InlineData(20, 100, 5)]
        [InlineData(100, 100, 5)]
        [InlineData(5, 100, 100)]
        [InlineData(100, 100, 10)]
        [InlineData(100, 100, 100)]
        public async Task WaitAsyncWithCapacityProducesExpectedDelays(int count, int intervalMs, int capacity)
        {
            // arrange
            var outerTimer = Stopwatch.StartNew();

            using (var rateLimiter = new RateLimiter(TimeSpan.FromMilliseconds(intervalMs), capacity))
            {
                // act
                for (int i = 0; i < count; i++)
                {
                    await rateLimiter.WaitAsync();
                }
            }

            outerTimer.Stop();

            // assert
            Assert.InRange(
                actual: outerTimer.Elapsed.TotalMilliseconds,
                low: (Math.Max(0, count - capacity) * intervalMs) / capacity,
                high: ((Math.Max(1, count - capacity) * intervalMs) / capacity) + (intervalMs / 2 * count));
        }

        [Theory]
        [InlineData(5, 100, 1000)]
        public async Task WaitAsyncWithRandomInvervalProducesExpectedDelays(int count, int intervalMsMin, int intervalMsMax)
        {
            // arrange
            var outerTimer = Stopwatch.StartNew();
            var lastIteration = TimeSpan.Zero;
            var sinceLastIteration = TimeSpan.Zero;
            var intervals = new List<TimeSpan>();

            using (var rateLimiter = new RateLimiter(TimeSpan.FromMilliseconds(intervalMsMin), TimeSpan.FromMilliseconds(intervalMsMax)))
            {
                // act
                for (int i = 0; i < count; i++)
                {
                    await rateLimiter.WaitAsync();
                    var elapsed = outerTimer.Elapsed;

                    // assert
                    sinceLastIteration = elapsed - lastIteration;
                    lastIteration = elapsed;

                    if (i == 0)
                    {
                        Assert.InRange(sinceLastIteration.TotalMilliseconds, 0, 5);
                    }
                    else
                    {
                        intervals.Add(elapsed);
                        Assert.InRange(sinceLastIteration.TotalMilliseconds, intervalMsMin, intervalMsMax + 50);
                    }
                }
            }

            Assert.Contains(intervals, x => x.TotalMilliseconds < intervalMsMax);
        }
    }
}
