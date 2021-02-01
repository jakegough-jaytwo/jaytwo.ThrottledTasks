using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace jaytwo.ThrottledTasks.Tests
{
    public class ThrottledTaskRunnerTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 10)]
        [InlineData(100000, 100)]
        [InlineData(1000000, 1000)]
        public async Task ThrottledTaskRunnerRunsAllTasks(int iterations, int maxConcurrentTasks)
        {
            // arrange
            int counter = 0;

            var enumerableTasks = Enumerable.Range(0, iterations)
                .Select(x => new Func<Task>(() =>
                {
                    Interlocked.Increment(ref counter);
                    return Task.CompletedTask;
                }));

            // act
            await ThrottledTaskRunner.RunInParallelAsync(enumerableTasks, maxConcurrentTasks);

            // assert
            Assert.Equal(iterations, counter);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 10)]
        [InlineData(100000, 100)]
        [InlineData(1000000, 1000)]
        public async Task ThrottledTaskRunnerThrowsExceptionOnTaskException(int iterations, int maxConcurrentTasks)
        {
            // arrange
            int counter = 0;
            var semaphoreSlim = new SemaphoreSlim(1);
            var unluckyNumber = new Random().Next(0, iterations - 1);
            var isThrown = false;

            var enumerableTasks = Enumerable.Range(0, iterations)
                .Select(x => new Func<Task>(async () =>
                {
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        if (!isThrown && counter == unluckyNumber)
                        {
                            isThrown = true;
                            throw new InvalidOperationException(Guid.NewGuid().ToString());
                        }

                        counter++;
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }));

            // act && assert
            // only a single exception here so it's the original InvalidOperationException
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ThrottledTaskRunner.RunInParallelAsync(enumerableTasks, maxConcurrentTasks));
            Assert.NotEqual(iterations, counter);
        }

        [Theory]
        [InlineData(100, 1)]
        [InlineData(1000, 100)]
        [InlineData(10000, 100)]
        public async Task ThrottledTaskRunnerStopsProcessingOnExceptions(int desiredIterations, int maxConcurrentTasks)
        {
            // arrange
            var iteratorCounter = 0;
            int taskCounter = 0;
            var unluckyNumber = new Random().Next(0, desiredIterations / 2);

            var enumerableTasks = Enumerable.Range(0, desiredIterations)
                .Select(x => new Func<Task>(async () =>
                {
                    Interlocked.Increment(ref iteratorCounter);
                    await Task.Delay(new Random().Next(1, 3));
                    if (taskCounter >= unluckyNumber)
                    {
                        throw new InvalidOperationException(Guid.NewGuid().ToString());
                    }

                    Interlocked.Increment(ref taskCounter);
                }));

            // act && assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ThrottledTaskRunner.RunInParallelAsync(enumerableTasks, maxConcurrentTasks));
            Assert.Equal(unluckyNumber, taskCounter);
            Assert.True(taskCounter < iteratorCounter);
            Assert.True(iteratorCounter < desiredIterations);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(100, 10)]
        [InlineData(1000, 100)]
        [InlineData(10000, 100)]
        public async Task ThrottledTaskRunnerRunInParallelWorksForIAsyncEnumerable(int desiredIterations, int maxConcurrentTasks)
        {
            // arrange
            var random = new Random();
            var range = Enumerable.Range(0, desiredIterations).ToList();
            var enumerableTasks = range.Select(i => new Func<Task<int>>(async () =>
            {
                await Task.Delay(random.Next(1, 3));
                return i;
            }));

            // act
            var results = await ThrottledTaskRunner.GetResultsInParallelAsync(enumerableTasks, maxConcurrentTasks).ToListAsync();

            // assert
            Assert.Equal(desiredIterations, results.Distinct().Count());

            if (desiredIterations > 1)
            {
                Assert.NotEqual(range, results);
            }
        }

        [Theory]
        [InlineData(100, 5)]
        [InlineData(10000, 100)]
        [InlineData(100000, 1000)]
        public async Task ThrottledTaskRunnerRunInParallelForIAsyncEnumerableStopsOnException(int desiredIterations, int maxConcurrentTasks)
        {
            // arrange
            int iteratorCounter = 0;
            var unluckyNumber = new Random().Next(0, desiredIterations / 2);

            var random = new Random();
            var range = Enumerable.Range(0, desiredIterations).ToList();
            var enumerableTasks = range.Select(i => new Func<Task<int>>(async () =>
            {
                if (i == unluckyNumber)
                {
                    throw new InvalidOperationException();
                }

                Interlocked.Increment(ref iteratorCounter);
                await Task.Delay(random.Next(1, 3));
                return i;
            }));

            // act & assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await ThrottledTaskRunner.GetResultsInParallelAsync(enumerableTasks, maxConcurrentTasks).ToListAsync());
            Assert.True(iteratorCounter < desiredIterations);
        }
    }
}
