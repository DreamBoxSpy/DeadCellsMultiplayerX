using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Utils;
using ModCore.Events;
using ModCore.Modules;
using Nerdbank.Streams;
using Serilog;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Host
{
    internal class GuestConnection : Disposable, IHostClientRPC, IDisposable
    {
        private readonly HostClient host;
        private readonly BaseNetworkConnection connection;

        private JsonRpc? rpc;
        public GuestInfo guestInfo = new();
        public PlyerGameSessionInfo plyerGameinfo = new();
        public override ILogger Logger { get; }

        public GuestConnection(HostClient host, BaseNetworkConnection connection)
        {
            this.host = host;
            this.connection = connection;

            Logger = Log.ForContext("SourceContext", "Guest-" + guestInfo.Guid);
        }

        protected override void MyDispose()
        {
            EventSystem.BroadcastEvent<IOnGuestQuit, GuestInfo>(guestInfo);

            host.LobbyInfo.Guests.Remove(guestInfo.Guid);
            host.GameSessionInfo.PlyerGameSession.Remove(guestInfo.Guid);
            rpc?.Dispose();
        }

        public void Init()
        {

            host.LobbyInfo.Guests.Add(guestInfo.Guid, guestInfo);
            host.GameSessionInfo.PlyerGameSession.Add(guestInfo.Guid, plyerGameinfo);

            rpc = connection.Stream.CreateJsonRpc();

            rpc.AddLocalRpcTarget(this);
            rpc.Disconnected += Rpc_Disconnected;
            rpc.SynchronizationContext = Game.SynchronizationContext;
            rpc.StartListening();
        }

        private void Rpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            if (e.Reason == DisconnectedReason.LocallyDisposed)
            {
                return;
            }
            Logger.Error(e.Exception, "Abort connection: {reason}: {desc}", e.Reason, e.Description);

            Dispose();
        }

        // IHostClientRPC
        public Task<string> GetGUID()
        {
            return Task.FromResult(guestInfo.Guid);
        }

        public Task<LobbyInfo> GetLobbyInfo()
        {
            return Task.FromResult(host.LobbyInfo);
        }

        public Task<GameSessionInfo> GetGameSessionInfo()
        {
            return Task.FromResult(host.GameSessionInfo);
        }

        public void SetName(string name)
        {
            Logger.Information("Set name as '{name}'", guestInfo.Guid, name);
            guestInfo.Name = name;
        }

        public Task SetSkinMould(string skinMould)
        {
            guestInfo.SkinMould = skinMould;
            return Task.CompletedTask;
        }

        public void Quit()
        {
            Logger.Information("Quit.", guestInfo.Guid);

            Dispose();
        }

        public Task<bool> CheckVersion(string version)
        {
            if (Version.Parse(version) != VersionUtils.ModVersion)
            {
                Logger.Error("({guid}) Dismatch version: {ver}", guestInfo.Guid, version);
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                {
                    if (rpc == null)
                    {
                        return;
                    }
                    if (!rpc.IsDisposed)
                    {
                        Dispose();
                    }
                });
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public void SetReady(bool ready)
        {
            Logger.Information("Set Ready: {val}", guestInfo.Guid, ready);
            guestInfo.IsReady = ready;
        }

        public Task Ping() => Task.CompletedTask;

        public void HeroInitDone(bool InitDone)
        {
            Logger.Information("Hero Init Done as '{F1}'", guestInfo.Guid);
            plyerGameinfo.HeroInitDone = InitDone;
        }

        public Task<Stream> GetServerStream()
        {
            if (!host.LobbyInfo.CanConnectServer ||
                host.session == null)
            {
                throw new InvalidOperationException();
            }

            Logger.Information("Connected server");
            channel = host.session.AllocNewChannel();

            Debug.Assert(IsConnectedServer);
            return Task.FromResult(channel.AsStream());
        }

        //

        MultiplexingStream.Channel? channel;

        /// <summary>
        /// 是否连接到服务器
        /// </summary>
        public bool IsConnectedServer => channel != null;

        public void StartConnectServer()
        {
            Task.Delay(TimeSpan.FromSeconds(
                ModConfig.Config.Value.StartTimeout
                ), DisposeToken
                ).ContinueWith(_ =>
                {
                    if (!IsConnectedServer)
                    {
                        Logger.Error("Failed to connect server. Timeout.");
                        Dispose();
                    }
                });
        }

    }
}
