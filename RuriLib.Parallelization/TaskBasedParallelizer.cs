using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RuriLib.Parallelization
{
    /// <summary>
    /// Parallelizer that uses Channel&lt;T&gt; for efficient producer-consumer item distribution
    /// with semaphore-based concurrency control.
    /// </summary>
    public class TaskBasedParallelizer<TInput, TOutput> : Parallelizer<TInput, TOutput>
    {
        #region Private Fields
        private int ChannelCapacity => MaxDegreeOfParallelism * 2;
        private SemaphoreSlim semaphore;
        private int savedDOP;
        private bool dopDecreaseRequested;
        #endregion

        #region Constructors
        /// <inheritdoc/>
        public TaskBasedParallelizer(IEnumerable<TInput> workItems, Func<TInput, CancellationToken, Task<TOutput>> workFunction,
            int degreeOfParallelism, long totalAmount, int skip = 0, int maxDegreeOfParallelism = 200)
            : base(workItems, workFunction, degreeOfParallelism, totalAmount, skip, maxDegreeOfParallelism)
        {

        }
        #endregion

        #region Public Methods
        /// <inheritdoc/>
        public async override Task Start()
        {
            await base.Start().ConfigureAwait(false);

            stopwatch.Restart();
            Status = ParallelizerStatus.Running;
            _ = Task.Run(() => Run()).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async override Task Pause()
        {
            await base.Pause().ConfigureAwait(false);

            Status = ParallelizerStatus.Pausing;
            savedDOP = degreeOfParallelism;
            await ChangeDegreeOfParallelism(0).ConfigureAwait(false);
            Status = ParallelizerStatus.Paused;
            stopwatch.Stop();
        }

        /// <inheritdoc/>
        public async override Task Resume()
        {
            await base.Resume().ConfigureAwait(false);

            Status = ParallelizerStatus.Resuming;
            await ChangeDegreeOfParallelism(savedDOP).ConfigureAwait(false);
            Status = ParallelizerStatus.Running;
            stopwatch.Start();
        }

        /// <inheritdoc/>
        public async override Task Stop()
        {
            await base.Stop().ConfigureAwait(false);

            Status = ParallelizerStatus.Stopping;
            softCTS.Cancel();
            await WaitCompletion().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async override Task Abort()
        {
            await base.Abort().ConfigureAwait(false);

            Status = ParallelizerStatus.Stopping;
            hardCTS.Cancel();
            softCTS.Cancel();
            await WaitCompletion().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async override Task ChangeDegreeOfParallelism(int newValue)
        {
            await base.ChangeDegreeOfParallelism(newValue);

            if (Status == ParallelizerStatus.Idle)
            {
                degreeOfParallelism = newValue;
                return;
            }
            else if (Status == ParallelizerStatus.Paused)
            {
                savedDOP = newValue;
                return;
            }

            if (newValue == degreeOfParallelism)
            {
                return;
            }
            else if (newValue > degreeOfParallelism)
            {
                semaphore.Release(newValue - degreeOfParallelism);
            }
            else
            {
                dopDecreaseRequested = true;
                for (var i = 0; i < degreeOfParallelism - newValue; ++i)
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                }
                dopDecreaseRequested = false;
            }

            degreeOfParallelism = newValue;
        }
        #endregion

        #region Private Methods
        // Run is executed in fire and forget mode (not awaited)
        private async void Run()
        {
            semaphore = new SemaphoreSlim(degreeOfParallelism, MaxDegreeOfParallelism);
            dopDecreaseRequested = false;

            // Create a bounded channel for producer-consumer item distribution
            var channel = Channel.CreateBounded<TInput>(new BoundedChannelOptions(ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            // Producer task: enumerate work items and write to the channel
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var item in workItems.Skip(skip))
                    {
                        if (softCTS.IsCancellationRequested)
                            break;

                        await channel.Writer.WriteAsync(item, softCTS.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    channel.Writer.Complete();
                }
            });

            try
            {
                // Consumer: read items from the channel and process them with semaphore-based concurrency
                while (!softCTS.IsCancellationRequested)
                {
                    TInput item;
                    try
                    {
                        item = await channel.Reader.ReadAsync(softCTS.Token).ConfigureAwait(false);
                    }
                    catch (ChannelClosedException)
                    {
                        break; // No more items
                    }

                    WAIT:
                    await semaphore.WaitAsync(softCTS.Token).ConfigureAwait(false);

                    if (softCTS.IsCancellationRequested)
                    {
                        semaphore?.Release();
                        break;
                    }

                    if (dopDecreaseRequested || IsCPMLimited())
                    {
                        UpdateCPM();
                        semaphore?.Release();
                        goto WAIT;
                    }

                    // Fire and forget the task; it will release the semaphore slot when done
                    _ = taskFunction.Invoke(item)
                        .ContinueWith(_ => semaphore?.Release())
                        .ConfigureAwait(false);
                }

                // Wait for every remaining task from the last batch to finish unless aborted
                while (Progress < 1 && !hardCTS.IsCancellationRequested)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Wait for current tasks to finish unless aborted
                while (semaphore.CurrentCount < degreeOfParallelism && !hardCTS.IsCancellationRequested)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                OnCompleted();
                Status = ParallelizerStatus.Idle;
                hardCTS?.Dispose();
                softCTS?.Dispose();
                semaphore?.Dispose();
                semaphore = null;
                stopwatch?.Stop();
            }
        }
        #endregion
    }
}