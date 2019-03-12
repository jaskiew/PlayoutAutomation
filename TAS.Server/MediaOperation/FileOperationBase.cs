﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Server;

namespace TAS.Server.MediaOperation
{
    public abstract class FileOperationBase : DtoBase, IFileOperationBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private int _tryCount = 15;
        private DateTime _scheduledTime;
        private DateTime _startTime;
        private DateTime _finishedTime;
        private FileOperationStatus _operationStatus;
        private int _progress;
        private bool _isIndeterminate;
        private readonly List<string> _operationOutput = new List<string>();
        private readonly List<string> _operationWarning = new List<string>();
        protected FileManager OwnerFileManager;
        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private bool _isAborted;

        internal FileOperationBase(FileManager ownerFileManager)
        {
            OwnerFileManager = ownerFileManager;
        }

        [JsonProperty]
        public int TryCount
        {
            get => _tryCount;
            internal set => SetField(ref _tryCount, value);
        }

        [JsonProperty]
        public int Progress
        {
            get => _progress;
            internal set
            {
                if (value > 0 && value <= 100)
                    SetField(ref _progress, value);
                IsIndeterminate = false;
            }
        }

        [JsonProperty]
        public DateTime ScheduledTime
        {
            get => _scheduledTime;
            internal set
            {
                if (SetField(ref _scheduledTime, value))
                    AddOutputMessage("Operation scheduled");
            }
        }

        [JsonProperty]
        public DateTime StartTime
        {
            get => _startTime;
            protected set => SetField(ref _startTime, value);
        }

        [JsonProperty]
        public DateTime FinishedTime 
        {
            get => _finishedTime;
            protected set => SetField(ref _finishedTime, value);
        }

        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get => _operationStatus;
            set
            {
                if (!SetField(ref _operationStatus, value))
                    return;
                OnOperationStatusChanged();
                EventHandler h;
                if (value == FileOperationStatus.Finished)
                {
                    Progress = 100;
                    FinishedTime = DateTime.UtcNow;
                    h = Success;
                    h?.Invoke(this, EventArgs.Empty);
                    h = Finished;
                    h?.Invoke(this, EventArgs.Empty);
                }
                if (value == FileOperationStatus.Failed)
                {
                    Progress = 0;
                    h = Failure;
                    h?.Invoke(this, EventArgs.Empty);
                    h = Finished;
                    h?.Invoke(this, EventArgs.Empty);
                }
                if (value == FileOperationStatus.Aborted)
                {
                    IsIndeterminate = false;
                    h = Failure;
                    h?.Invoke(this, EventArgs.Empty);
                    h = Finished;
                    h?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        [JsonProperty]
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetField(ref _isIndeterminate, value);
        }

        [JsonProperty]
        public bool IsAborted
        {
            get => _isAborted;
            private set => SetField(ref _isAborted, value);
        }

        [JsonProperty]
        public List<string> OperationWarning
        {
            get
            {
                lock (((IList) _operationWarning).SyncRoot)
                {
                    return _operationWarning.ToList();
                }
            }
        }

        [JsonProperty]
        public List<string> OperationOutput
        {
            get
            {
                lock (((IList)_operationOutput).SyncRoot)
                {
                    return _operationOutput.ToList();
                }
            }
        }

        public virtual void Abort()
        {
            if (IsAborted)
                return;
            IsAborted = true;
            CancellationTokenSource.Cancel();
            IsIndeterminate = false;
            OperationStatus = FileOperationStatus.Aborted;
        }

        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;

        internal async Task<bool> Execute()
        {
            try
            {
                AddOutputMessage("Operation started");
                OperationStatus = FileOperationStatus.InProgress;
                if (await InternalExecute())
                {
                    OperationStatus = FileOperationStatus.Finished;
                    AddOutputMessage("Operation completed successfully.");
                    return true;
                }
            }
            catch (Exception e)
            {
                AddOutputMessage(e.Message);
            }
            TryCount--;
            if (!IsAborted)
                OperationStatus = TryCount > 0 ? FileOperationStatus.Waiting : FileOperationStatus.Failed;
            return false;
        }

        protected abstract void OnOperationStatusChanged();
        
        internal void AddOutputMessage(string message)
        {
            lock (((IList)_operationOutput).SyncRoot)
                _operationOutput.Add($"{DateTime.UtcNow} {message}");
            NotifyPropertyChanged(nameof(OperationOutput));
            Logger.Trace(message);
        }

        internal void AddWarningMessage(string message)
        {
            lock (((IList)_operationWarning).SyncRoot)
                _operationWarning.Add(message);
            Logger.Warn(message);
            NotifyPropertyChanged(nameof(OperationWarning));
        }

        protected abstract Task<bool> InternalExecute();
    }
}