﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.TaskManagement
{
    public class TaskHosting
    {
        private ITaskConsumer _consumer;
        private ITaskFactory _taskFactory;
        private ILogger<TaskHosting> _logger;
        private ConcurrentDictionary<string, ITask> _activeTaskRecordsForKeepAlive = new ConcurrentDictionary<string, ITask>();

        public TaskHosting(ITaskConsumer consumer, ITaskFactory taskFactory, ILogger<TaskHosting> logger)
        {
            _consumer = consumer;
            _taskFactory = taskFactory;
            _logger = logger;
        }

        public int PollingFrequencyInSeconds { get; set; } = Constants.DefaultPollingFrequencyInSeconds;

        public short MaxRunningTaskCount { get; set; } = Constants.DefaultMaxRunningTaskCount;

        public short MaxRetryCount { get; set; } = Constants.DefaultMaxRetryCount;

        public int TaskHeartbeatTimeoutThresholdInSeconds { get; set; } = Constants.DefaultTaskHeartbeatTimeoutThresholdInSeconds;

        public int TaskHeartbeatIntervalInSeconds { get; set; } = Constants.DefaultTaskHeartbeatIntervalInSeconds;

        public async Task StartAsync(CancellationTokenSource cancellationToken)
        {
            CancellationTokenSource keepAliveCancellationToken = new CancellationTokenSource();
            Task keepAliveTask = KeepAliveTasksAsync(keepAliveCancellationToken.Token);

            await PullAndProcessTasksAsync(cancellationToken.Token);

            keepAliveCancellationToken.Cancel();
            await keepAliveTask;
        }

        private async Task PullAndProcessTasksAsync(CancellationToken cancellationToken)
        {
            List<Task> runningTasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested)
            {
                Task intervalDelayTask = Task.Delay(TimeSpan.FromSeconds(PollingFrequencyInSeconds));

                if (runningTasks.Count >= MaxRunningTaskCount)
                {
                    _ = await Task.WhenAny(runningTasks.ToArray());
                    runningTasks.RemoveAll(t => t.IsCompleted);
                }

                IReadOnlyCollection<TaskInfo> nextTasks = null;
                try
                {
                    nextTasks = await _consumer.GetNextMessagesAsync((short)(MaxRunningTaskCount - runningTasks.Count), TaskHeartbeatTimeoutThresholdInSeconds, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to pull new tasks.");
                }

                if (nextTasks != null && nextTasks.Count > 0)
                {
                    foreach (TaskInfo taskInfo in nextTasks)
                    {
                        runningTasks.Add(ExecuteTaskAsync(taskInfo, cancellationToken));
                    }
                }

                await intervalDelayTask;
            }

            try
            {
                Task.WaitAll(runningTasks.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task failed to execute");
            }
        }

        private async Task ExecuteTaskAsync(TaskInfo taskInfo, CancellationToken cancellationToken)
        {
            using ITask task = _taskFactory.Create(taskInfo);
            task.RunId = task.RunId ?? taskInfo.RunId;

            if (task == null)
            {
                _logger.LogWarning("Not supported task type: {0}", taskInfo.TaskTypeId);
                return;
            }

            task.RunId = taskInfo.RunId;
            TaskResultData result = null;
            try
            {
                try
                {
                    Task<TaskResultData> runningTask = task.ExecuteAsync();
                    _activeTaskRecordsForKeepAlive[taskInfo.TaskId] = task;

                    result = await runningTask;
                }
                catch (RetriableTaskException ex)
                {
                    _logger.LogError(ex, "Task {0} failed with retriable exception.", taskInfo.TaskId);

                    try
                    {
                        await _consumer.ResetAsync(taskInfo.TaskId, new TaskResultData(TaskResult.Fail, ex.Message), taskInfo.RunId, MaxRetryCount, cancellationToken);
                    }
                    catch (Exception resetEx)
                    {
                        _logger.LogError(resetEx, "Task {0} failed to reset.", taskInfo.TaskId);
                    }

                    // Not complete the task for retriable exception.
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Task {0} failed.", taskInfo.TaskId);
                    result = new TaskResultData(TaskResult.Fail, ex.Message);
                }

                try
                {
                    await _consumer.CompleteAsync(taskInfo.TaskId, result, task.RunId, cancellationToken);
                    _logger.LogInformation("Task {0} completed.", taskInfo.TaskId);
                }
                catch (Exception completeEx)
                {
                    _logger.LogError(completeEx, "Task {0} failed to complete.", taskInfo.TaskId);
                }
            }
            finally
            {
                _activeTaskRecordsForKeepAlive.Remove(taskInfo.TaskId, out _);
            }
        }

        private async Task KeepAliveTasksAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start to keep alive task message.");

            while (!cancellationToken.IsCancellationRequested)
            {
                Task intervalDelayTask = Task.Delay(TaskHeartbeatIntervalInSeconds);
                KeyValuePair<string, ITask>[] activeTaskRecords = _activeTaskRecordsForKeepAlive.ToArray();

                foreach ((string taskId, ITask task) in activeTaskRecords)
                {
                    try
                    {
                        if (task.IsCancelling())
                        {
                            continue;
                        }

                        bool shouldCancel = false;
                        try
                        {
                            TaskInfo taskInfo = await _consumer.KeepAliveAsync(taskId, task.RunId, cancellationToken);
                            shouldCancel |= taskInfo.IsCanceled;
                        }
                        catch (TaskNotExistException notExistEx)
                        {
                            _logger.LogError(notExistEx, "Task {0} not exist or runid not match.", taskId);
                            shouldCancel = true;
                        }

                        if (shouldCancel)
                        {
                            task.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to keep alive on task {0}", taskId);
                    }
                }

                await intervalDelayTask;
            }

            _logger.LogInformation("Stop to keep alive task message.");
        }
    }
}
