﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Relay.Bridge
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Relay;
    using Microsoft.Win32;

    sealed class TcpRemoteForwardBridge : IDisposable
    {
        readonly RelayConnectionStringBuilder connectionString;
        static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
        readonly int targetPort;
        readonly string targetServer;
        HybridConnectionListener listener;
        CancellationTokenSource shuttingDown = new CancellationTokenSource();
        EventTraceActivity activity = BridgeEventSource.NewActivity("RemoteForwardBridge");

        internal TcpRemoteForwardBridge(RelayConnectionStringBuilder connectionString, string targetServer, int targetPort)
        {
            this.connectionString = connectionString;
            this.targetServer = targetServer;
            this.targetPort = targetPort;
        }

        public event EventHandler Connecting;
        public event EventHandler Offline;
        public event EventHandler Online;

        public bool IsOnline
        {
            get { return this.listener.IsOnline; }
        }

        public Exception LastError
        {
            get { return this.listener.LastError; }
        }

        bool IsOpen { get; set; }

        public async void Close()
        {
            if (!this.IsOpen)
            {
                throw new InvalidOperationException();
            }

            shuttingDown.Cancel(); ;

            this.IsOpen = false;
            await this.listener.CloseAsync(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            this.IsOpen = false;
            this.listener.CloseAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Opens this PortBridgeServerProxy instance and listens for new connections coming through Service Bus.
        /// </summary>
        /// <exception cref="System.Security.SecurityException">Throws a SecurityException if Group Policy prohibits Resource Publishing.</exception>
        public async Task Open()
        {
            if (this.IsOpen)
            {
                throw new InvalidOperationException();
            }

            this.listener = new HybridConnectionListener(connectionString.ToString());
            this.listener.Online += (s, e) => { Online?.Invoke(this, e); };
            this.listener.Offline += (s, e) => { Offline?.Invoke(this, e); };
            this.listener.Connecting += (s, e) => { Connecting?.Invoke(this, e); };

            await listener.OpenAsync(shuttingDown.Token);
            this.IsOpen = true;

            AcceptLoopAsync().Fork(this);
        }

        async Task AcceptLoopAsync()
        {
            while (!shuttingDown.IsCancellationRequested)
            {
                try
                {
                    var hybridConnectionStream = await listener.AcceptConnectionAsync();
                    if (hybridConnectionStream == null)
                    {
                        // we only get null if trhe listener is shutting down
                        break;
                    }
                    ConnectionPumpLoopAsync(hybridConnectionStream).Fork(this);
                }
                catch (Exception e)
                {
                    BridgeEventSource.Log.HandledExceptionAsWarning(activity, e);
                }
            }
        }

        async Task ConnectionPumpLoopAsync(Microsoft.Azure.Relay.HybridConnectionStream hybridConnectionStream)
        {
            try
            {
                // read and write 4-byte header
                byte[] rd=new byte[4];
                int read = 0;
                for(; read < 4; read +=hybridConnectionStream.Read(rd,read,4-read));
                hybridConnectionStream.Write(new byte[]{1,0,0,0},0,4);
                
                using (hybridConnectionStream)
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(targetServer, targetPort);
                        var tcpstream = client.GetStream();
                        await Task.WhenAll(
                            StreamPump.RunAsync(hybridConnectionStream, tcpstream, () => client.Client.Shutdown(SocketShutdown.Send), shuttingDown.Token),
                            StreamPump.RunAsync(tcpstream, hybridConnectionStream, () => hybridConnectionStream.Shutdown(), shuttingDown.Token));
                    }
                }
            }
            catch (Exception e)
            {
                BridgeEventSource.Log.HandledExceptionAsWarning(activity, e);
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            {
                await hybridConnectionStream.CloseAsync(cts.Token);
            }
        }

    }
}