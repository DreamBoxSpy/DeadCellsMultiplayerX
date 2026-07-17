using dc;
using dc.en;
using dc.tool;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Server.Events;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    internal class GuestClient(BaseNetworkConnection remote) : ClientBase,
    IOnGuestHeroInitDone
    {
        private JsonRpc? rpc;
        private IHostClientRPC? hostInterfact;
        private GuestClientSession? session;

        public CancellationTokenSource DisconnectToken { get; } = new();

        public LobbyInfo? LobbyInfo { get; set; }

        public string Guid { get; set; } = "";

        public async Task Init(string name)
        {

            rpc = remote.Stream.CreateJsonRpc();

            hostInterfact = rpc.Attach<IHostClientRPC>();

            rpc.Disconnected += Rpc_Disconnected;
            rpc.StartListening();

            if(!await hostInterfact.CheckVersion(
                VersionUtils.ModVersion.ToString()
                ))
            {
                Logger.Information("Failed to connect lobby. Dismatch version.");
                Dispose();
                return;
            }

            Guid = await hostInterfact.GetGUID();

            SetName(name);
            await SetSkinMould(Save.Class.tryLoad().heroSkin.ToString());
            SetReady(false);

            LobbyInfo = await hostInterfact.GetLobbyInfo();

            _ = MessageLoop();
        }

        private async Task MessageLoop()
        {
            Debug.Assert(hostInterfact != null);
            await Task.Delay(1).ConfigureAwait(false);

            while (!IsDisposed && session == null)
            {
                DisposeToken.ThrowIfCancellationRequested();

                LobbyInfo = await hostInterfact.GetLobbyInfo();

                if (LobbyInfo.CanConnectServer)
                {
                    session = new GuestClientSession(this, await hostInterfact.GetServerStream());
                    await session.Init();
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        }

        private void Rpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            if(e.Reason == DisconnectedReason.LocallyDisposed)
            {
                return;
            }
            Logger.Error(e.Exception, "Abort connection: {reason}: {desc}", e.Reason, e.Description);
            Dispose();
        }

        protected override void MyDispose()
        {
            base.MyDispose();
            DisconnectToken.Cancel();
            session?.Dispose();
            rpc?.Dispose();
        }

        public void SetName(string name)
        {
            Debug.Assert(hostInterfact != null);

            hostInterfact.SetName(name);
        }

        public void SetReady(bool ready)
        {
            Debug.Assert(hostInterfact != null);

            hostInterfact.SetReady(ready);
        }

        public Task SetSkinMould(string skinMould)
        {
            Debug.Assert(hostInterfact != null);
            return hostInterfact.SetSkinMould(skinMould);
        }

        public async Task<long> Ping()
        {
            if (IsDisposed || rpc == null || rpc.IsDisposed)
                return -1;

            Debug.Assert(hostInterfact != null);
            var sw = Stopwatch.StartNew();
            await hostInterfact.Ping();
            return sw.ElapsedMilliseconds;
        }

        public void Quit()
        {
            Debug.Assert(hostInterfact != null);

            if (!(rpc?.IsDisposed ?? true))
            {
                hostInterfact.Quit();
            }

            Dispose();
        }

        void IOnGuestHeroInitDone.OnHeroInitDone(Hero hero)
        {
            Debug.Assert(hostInterfact != null);
            
            hostInterfact.HeroInitDone(true);
        }
    }
}
