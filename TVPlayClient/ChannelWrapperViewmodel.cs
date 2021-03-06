﻿using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using jNet.RPC.Client;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Remoting;
using TAS.Remoting.Model;

namespace TVPlayClient
{
    public class ChannelWrapperViewmodel : ViewModelBase
    {

        private readonly ChannelConfiguration _channelConfiguration;
        private RemoteClient _client;
        private ChannelViewmodel _channel;
        private bool _isLoading = true;
        private bool _disposed;
        private string _tabName;

        public ChannelWrapperViewmodel(ChannelConfiguration channel)
        {
            _channelConfiguration = channel;
        }

        public void Initialize()
        {
            _createView();
        }

        public string TabName
        {
            get => _tabName;
            private set => SetField(ref _tabName, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public ChannelViewmodel Channel
        {
            get => _channel;
            private set => SetField(ref _channel, value);
        }

        protected override void OnDispose()
        {
            _channel?.Dispose();
            _client?.Dispose();
            _disposed = true;
            Debug.WriteLine(this, "Disposed");
        }

        private void ClientDisconnected(object sender, EventArgs e)
        {
            if (!(sender is RemoteClient client))
                return;
            client.Disconnected -= ClientDisconnected;
            client.Dispose();
            var channel = Channel;
            OnUiThread(() =>
            {
                Channel = null;
                IsLoading = true;
                channel?.Dispose();
                _createView();
            });
        }

        private void _createView()
        {
            Task.Run(() =>
            {
#if DEBUG
                Thread.Sleep(5000);
#endif
                try
                {
                    _client = new RemoteClient(_channelConfiguration.Address)
                    {
                        Binder = ClientTypeNameBinder.Current
                    };
                    _client.Disconnected += ClientDisconnected;
                    var engine = _client.GetRootObject<Engine>();
                    if (engine == null)
                    {
                        Thread.Sleep(1000);
                        return;
                    }
                    OnUiThread(() =>
                    {
                        Channel = new ChannelViewmodel(engine, _channelConfiguration.ShowEngine, _channelConfiguration.ShowMedia);
                        TabName = Channel.DisplayName;
                        IsLoading = false;
                    });
                }
                catch (SocketException)
                {
                    Thread.Sleep(1000);
                }
            });
        }

    }

}
