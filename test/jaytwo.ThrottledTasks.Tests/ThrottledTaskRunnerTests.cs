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
            await ThrottledTaskRunner.RunInParallel(enumerableTasks, maxConcurrentTasks);

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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ThrottledTaskRunner.RunInParallel(enumerableTasks, maxConcurrentTasks));
            Assert.NotEqual(iterations, counter);
        }

        [Theory]
        [InlineData(100, 1)]
        [InlineData(1000, 100)]
        public async Task ThrottledTaskRunnerDoesStopsProcessingOnExceptions(int desiredIterations, int maxConcurrentTasks)
        {
            // arrange
            var iteratorCounter = 0;
            int taskCounter = 0;
            var semaphoreSlim = new SemaphoreSlim(1);
            var unluckyNumber = new Random().Next(0, desiredIterations / 2);

            var enumerableTasks = Enumerable.Range(0, desiredIterations)
                .Select(x => new Func<Task>(async () =>
                {
                    iteratorCounter++;
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        await Task.Delay(new Random().Next(2, 10));
                        if (taskCounter >= unluckyNumber)
                        {
                            throw new InvalidOperationException(Guid.NewGuid().ToString());
                        }

                        taskCounter++;
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }));

            // act && assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ThrottledTaskRunner.RunInParallel(enumerableTasks, maxConcurrentTasks));
            Assert.Equal(unluckyNumber, taskCounter);
            Assert.True(taskCounter < iteratorCounter);
            Assert.True(iteratorCounter < desiredIterations);
        }
    }
}
