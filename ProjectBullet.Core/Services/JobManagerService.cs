using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Models.Data;
using ProjectBullet.Core.Models.Jobs;
using ProjectBullet.Core.Repositories;
using RuriLib.Models.Data.DataPools;
using RuriLib.Models.Jobs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectBullet.Core.Services;

/// <summary>
/// Manages multiple jobs.
/// </summary>
public class JobManagerService : IDisposable
{
    /// <summary>
    /// The list of all created jobs.
    /// </summary>
    public IEnumerable<Job> Jobs => _jobs;
    private readonly List<Job> _jobs = new();

    private readonly SemaphoreSlim _jobSemaphore = new(1, 1);
    private readonly SemaphoreSlim _recordSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;

    // Auto-restart and stale detection tracking
    private readonly ConcurrentDictionary<int, float> _lastProgress = new();
    private readonly ConcurrentDictionary<int, DateTime> _lastProgressChangeTime = new();
    private readonly ConcurrentDictionary<int, Timer> _restartTimers = new();

    public JobManagerService(IServiceScopeFactory scopeFactory, JobFactoryService jobFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        // Restore jobs from the database
        var entities = jobRepo.GetAll().Include(j => j.Owner).ToList();
        var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new RuriLib.Helpers.SafeSerializationBinder() };

        foreach (var entity in entities)
        {
            // Convert old namespaces to support old databases
            if (entity.JobOptions.Contains("OpenBullet2.Models") || entity.JobOptions.Contains(", OpenBullet2\"")
                || entity.JobOptions.Contains("OpenBullet2.Core"))
            {
                entity.JobOptions = entity.JobOptions
                    .Replace("OpenBullet2.Core.Models", "ProjectBullet.Core.Models")
                    .Replace("OpenBullet2.Models", "ProjectBullet.Core.Models")
                    .Replace(", OpenBullet2.Core\"", ", ProjectBullet.Core\"")
                    .Replace(", OpenBullet2\"", ", ProjectBullet.Core\"");

                jobRepo.UpdateAsync(entity).Wait();
            }

            var options = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, jsonSettings).Options;
            var job = jobFactory.FromOptions(entity.Id, entity.Owner == null ? 0 : entity.Owner.Id, options);
            AddJob(job);
        }

        _scopeFactory = scopeFactory;
    }

    public void AddJob(Job job)
    {
        _jobs.Add(job);

        if (job is MultiRunJob mrj)
        {
            mrj.OnCompleted += SaveRecord;
            mrj.OnTimerTick += SaveRecord;
            mrj.OnCompleted += SaveMultiRunJobOptionsAsync;
            mrj.OnTimerTick += SaveMultiRunJobOptionsAsync;
            mrj.OnBotsChanged += SaveMultiRunJobOptionsAsync;
            mrj.OnCompleted += HandleAutoRestart;
            mrj.OnTimerTick += HandleStaleDetection;
        }
    }

    public void RemoveJob(Job job)
    {
        _jobs.Remove(job);

        if (job is MultiRunJob mrj)
        {
            try
            {
                mrj.OnCompleted -= SaveRecord;
                mrj.OnTimerTick -= SaveRecord;
                mrj.OnCompleted -= SaveMultiRunJobOptionsAsync;
                mrj.OnTimerTick -= SaveMultiRunJobOptionsAsync;
                mrj.OnBotsChanged -= SaveMultiRunJobOptionsAsync;
                mrj.OnCompleted -= HandleAutoRestart;
                mrj.OnTimerTick -= HandleStaleDetection;
            }
            catch
            {

            }

            _lastProgress.TryRemove(mrj.Id, out _);
            _lastProgressChangeTime.TryRemove(mrj.Id, out _);

            if (_restartTimers.TryRemove(mrj.Id, out var timer))
            {
                timer.Dispose();
            }
        }
    }

    public void Clear()
    {
        UnbindAllEvents();
        _jobs.Clear();
    }

    // Saves the record for a MultiRunJob in the IRecordRepository. Thread safe.
    private async void SaveRecord(object sender, EventArgs e)
    {
        using var scope = _scopeFactory.CreateScope();
        var recordRepo = scope.ServiceProvider.GetRequiredService<IRecordRepository>();

        if (sender is not MultiRunJob job || job.DataPool is not WordlistDataPool pool)
        {
            return;
        }

        await _recordSemaphore.WaitAsync();

        try
        {
            var record = await recordRepo.GetAll()
                    .FirstOrDefaultAsync(r => r.ConfigId == job.Config.Id && r.WordlistId == pool.Wordlist.Id);

            var checkpoint = job.Status == JobStatus.Idle
                ? job.Skip
                : job.Skip + job.DataTested;

            if (record == null)
            {
                await recordRepo.AddAsync(new RecordEntity
                {
                    ConfigId = job.Config.Id,
                    WordlistId = pool.Wordlist.Id,
                    Checkpoint = checkpoint
                });
            }
            else
            {
                record.Checkpoint = checkpoint;
                await recordRepo.UpdateAsync(record);
            }
        }
        catch
        {

        }
        finally
        {
            _recordSemaphore.Release();
        }
    }

    private async void SaveMultiRunJobOptionsAsync(object sender, EventArgs e)
    {
        if (sender is not MultiRunJob job)
        {
            return;
        }

        await SaveMultiRunJobOptionsAsync(job);
    }

    // Saves the options for a MultiRunJob in the IJobRepository. Thread safe.
    public async Task SaveMultiRunJobOptionsAsync(MultiRunJob job)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        await _jobSemaphore.WaitAsync();

        try
        {
            var entity = await jobRepo.GetAsync(job.Id);

            if (entity == null || entity.JobOptions == null)
            {
                Console.WriteLine("Skipped job options save because Job (or JobOptions) was null");
                return;
            }

            // Deserialize and unwrap the job options
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new RuriLib.Helpers.SafeSerializationBinder() };
            var wrapper = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, settings);
            var options = (MultiRunJobOptions)wrapper.Options;

            // Check if it's valid
            if (string.IsNullOrEmpty(options.ConfigId))
            {
                Console.WriteLine("Skipped job options save because ConfigId was null");
                return;
            }

            if (options.DataPool is WordlistDataPoolOptions x && x.WordlistId == -1)
            {
                Console.WriteLine("Skipped job options save because WordlistId was -1");
                return;
            }

            // Update the skip (if not idle, also add the currently tested ones) and the bots
            options.Skip = job.Status == JobStatus.Idle
                ? job.Skip
                : job.Skip + job.DataTested;

            options.Bots = job.Bots;

            // Wrap and serialize again
            var newWrapper = new JobOptionsWrapper { Options = options };
            entity.JobOptions = JsonConvert.SerializeObject(newWrapper, settings);

            // Update the job
            await jobRepo.UpdateAsync(entity);
        }
        catch
        {

        }
        finally
        {
            _jobSemaphore.Release();
        }
    }

    #region Auto-Restart and Stale Detection

    private async void HandleAutoRestart(object sender, EventArgs e)
    {
        if (sender is not MultiRunJob job)
            return;

        var options = await GetMultiRunJobOptionsAsync(job.Id);
        if (options == null || !options.AutoRestartEnabled)
            return;

        var delayMs = options.AutoRestartDelayMinutes * 60 * 1000;

        if (delayMs <= 0)
        {
            _ = Task.Run(() => RestartJobAsync(job));
        }
        else
        {
            var timer = new Timer(_ => _ = Task.Run(() => RestartJobAsync(job)), null, delayMs, Timeout.Infinite);
            _restartTimers.AddOrUpdate(job.Id, timer, (_, old) => { old.Dispose(); return timer; });
        }

        Console.WriteLine($"[AutoRestart] Job {job.Id} scheduled for restart (delay: {options.AutoRestartDelayMinutes}m)");
    }

    private async void HandleStaleDetection(object sender, EventArgs e)
    {
        if (sender is not MultiRunJob job || job.Status != JobStatus.Running)
            return;

        var options = await GetMultiRunJobOptionsAsync(job.Id);
        if (options == null || !options.StaleDetectionEnabled)
            return;

        var currentProgress = job.Progress * 100;
        var jobId = job.Id;

        if (!_lastProgress.TryGetValue(jobId, out var lastProg) || Math.Abs(currentProgress - lastProg) > 0.001f)
        {
            _lastProgress[jobId] = currentProgress;
            _lastProgressChangeTime[jobId] = DateTime.UtcNow;
            return;
        }

        if (currentProgress < options.StaleThresholdPercent)
            return;

        if (!_lastProgressChangeTime.TryGetValue(jobId, out var lastChangeTime))
            return;

        var elapsed = DateTime.UtcNow - lastChangeTime;
        if (elapsed.TotalMinutes < options.StaleTimeoutMinutes)
            return;

        Console.WriteLine($"[StaleDetection] Job {jobId} stale at {currentProgress:F1}% for {elapsed.TotalMinutes:F1}m, restarting...");

        _lastProgress.TryRemove(jobId, out _);
        _lastProgressChangeTime.TryRemove(jobId, out _);

        _ = Task.Run(async () =>
        {
            try
            {
                await job.Abort();
                await Task.Delay(2000);
                await RestartJobAsync(job);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StaleDetection] Failed to restart job {jobId}: {ex.Message}");
            }
        });
    }

    private async Task RestartJobAsync(MultiRunJob job)
    {
        try
        {
            // Wait for the job to become idle
            var maxWait = 30;
            while (job.Status != JobStatus.Idle && maxWait-- > 0)
            {
                await Task.Delay(1000);
            }

            if (job.Status != JobStatus.Idle)
            {
                Console.WriteLine($"[AutoRestart] Job {job.Id} not idle after waiting, skipping restart");
                return;
            }

            // Reset skip to 0 in the database
            await ResetJobSkipAsync(job.Id);
            job.Skip = 0;

            await job.Start(CancellationToken.None);
            Console.WriteLine($"[AutoRestart] Job {job.Id} restarted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoRestart] Failed to restart job {job.Id}: {ex.Message}");
        }
    }

    private async Task ResetJobSkipAsync(int jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        await _jobSemaphore.WaitAsync();

        try
        {
            var entity = await jobRepo.GetAsync(jobId);
            if (entity?.JobOptions == null) return;

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new RuriLib.Helpers.SafeSerializationBinder() };
            var wrapper = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, settings);
            var options = (MultiRunJobOptions)wrapper.Options;

            options.Skip = 0;

            var newWrapper = new JobOptionsWrapper { Options = options };
            entity.JobOptions = JsonConvert.SerializeObject(newWrapper, settings);
            await jobRepo.UpdateAsync(entity);
        }
        finally
        {
            _jobSemaphore.Release();
        }
    }

    private async Task<MultiRunJobOptions> GetMultiRunJobOptionsAsync(int jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        await _jobSemaphore.WaitAsync();

        try
        {
            var entity = await jobRepo.GetAsync(jobId);
            if (entity?.JobOptions == null) return null;

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new RuriLib.Helpers.SafeSerializationBinder() };
            var wrapper = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, settings);
            return wrapper.Options as MultiRunJobOptions;
        }
        catch
        {
            return null;
        }
        finally
        {
            _jobSemaphore.Release();
        }
    }

    #endregion

    private void UnbindAllEvents()
    {
        foreach (var job in _jobs)
        {
            if (job is MultiRunJob mrj)
            {
                try
                {
                    mrj.OnCompleted -= SaveRecord;
                    mrj.OnTimerTick -= SaveRecord;
                    mrj.OnCompleted -= SaveMultiRunJobOptionsAsync;
                    mrj.OnTimerTick -= SaveMultiRunJobOptionsAsync;
                    mrj.OnBotsChanged -= SaveMultiRunJobOptionsAsync;
                    mrj.OnCompleted -= HandleAutoRestart;
                    mrj.OnTimerTick -= HandleStaleDetection;
                }
                catch
                {

                }
            }
        }
    }

    public void Dispose()
    {
        UnbindAllEvents();

        foreach (var timer in _restartTimers.Values)
        {
            timer.Dispose();
        }

        _restartTimers.Clear();
    }
}
