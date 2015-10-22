﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TAS.Common;
using TAS.Data;

namespace TAS.Server
{
    public class AnimationDirectory: MediaDirectory
    {
        public readonly PlayoutServer Server;
        public AnimationDirectory(PlayoutServer server)
        {
            Server = server;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            _isInitialized = false; // to avoid subsequent reinitializations
            DirectoryName = "Animacje";
            this.Load();
            Debug.WriteLine(Server.MediaFolder, "AnimationDirectory initialized");
        }

        protected override void Reinitialize()
        {

        }

        public override void Refresh()
        {
            
        }

        protected override Media CreateMedia(string fileNameOnly)
        {
            return new ServerMedia(this) { MediaType = TMediaType.AnimationFlash, FileName = fileNameOnly, };
        }

        protected override Media CreateMedia(string fileNameOnly, Guid guid)
        {
            return new ServerMedia(this, guid) { MediaType = TMediaType.AnimationFlash, FileName = fileNameOnly, };
        }

        public override void MediaRemove(Media media)
        {
            if (media is ServerMedia)
            {
                ((ServerMedia)media).MediaStatus = TMediaStatus.Deleted;
                ((ServerMedia)media).Verified = false;
                ((ServerMedia)media).Save();
            }
            base.MediaRemove(media);
        }

        public override bool DeleteMedia(Media media)
        {
            if (base.DeleteMedia(media))
            {
                MediaRemove(media);
                return true;
            }
            return false;
        }

        public override void SweepStaleMedia() { }

    }
}
