﻿using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;

namespace TAS.Server.MediaOperation
{
    public class CopyOperation : FileOperationBase, ICopyOperation
    {
        private readonly object _destMediaLock = new object();

        private IMedia _sourceMedia;
        private IMediaProperties _destMediaProperties;

        internal CopyOperation(FileManager ownerFileManager) : base(ownerFileManager)
        {
        }
       
        [JsonProperty]
        public IMediaProperties DestProperties { get => _destMediaProperties; set => SetField(ref _destMediaProperties, value); }

        [JsonProperty]
        public IMediaDirectory DestDirectory { get; set; }

        [JsonProperty]
        public IMedia Source { get => _sourceMedia; set => SetField(ref _sourceMedia, value); }

        internal MediaBase Dest { get; set; }

        // utility methods

        protected virtual void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (Dest != null)
                    return;
                if (!(DestDirectory is MediaDirectoryBase mediaDirectory))
                    throw new ApplicationException($"Cannot create destination media on {DestDirectory}");
                Dest = (MediaBase) mediaDirectory.CreateMedia(DestProperties ?? Source);
            }
        }

        protected override void OnOperationStatusChanged()
        {
            TIngestStatus newIngestStatus;
            switch (OperationStatus)
            {
                case FileOperationStatus.Finished:
                    newIngestStatus = TIngestStatus.Ready;
                    break;
                case FileOperationStatus.Waiting:
                case FileOperationStatus.InProgress:
                    newIngestStatus = TIngestStatus.InProgress;
                    break;
                default:
                    newIngestStatus = TIngestStatus.Unknown;
                    break;
            }
            if (_sourceMedia is IngestMedia im)
                im.IngestStatus = newIngestStatus;
            if (_sourceMedia is ArchiveMedia am)
                am.IngestStatus = newIngestStatus;
        }

        public override void Abort()
        {
            base.Abort();
            lock (_destMediaLock)
            {
                if (Dest != null && Dest.FileExists())
                    Dest.Delete();
            }
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            if (!(Source is MediaBase source))
                return false;
            if (!File.Exists(source.FullPath) || !Directory.Exists(DestDirectory.Folder))
                return false;
            CreateDestMediaIfNotExists();
            FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(Dest.FullPath));
            if (!(Dest.FileExists()
                  && File.GetLastWriteTimeUtc(source.FullPath)
                      .Equals(File.GetLastWriteTimeUtc(Dest.FullPath))
                  && File.GetCreationTimeUtc(source.FullPath).Equals(File.GetCreationTimeUtc(Dest.FullPath))
                  && Source.FileSize.Equals(Dest.FileSize)))
            {
                Dest.MediaStatus = TMediaStatus.Copying;
                IsIndeterminate = true;
                if (!await source.CopyMediaTo(Dest, CancellationTokenSource.Token))
                    return false;
            }
            Dest.MediaStatus = TMediaStatus.Copied;
            await Task.Run(() => Dest.Verify(false));
            ((MediaDirectoryBase) DestDirectory).RefreshVolumeInfo();
            return true;
        }

    }
}
