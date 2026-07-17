using dc;
using dc.en;
using dc.tool;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Guest.WorldX;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Utils;
using Serilog;
using StreamJsonRpc;
using System.Diagnostics;


namespace DeadCellsMultiplayerX.Client.Guest
{
    internal class GuestClient(BaseNetworkConnection remote) : ClientBase,
    IOnGuestHeroInitDone
    {
        private JsonRpc? rpc;
        private IHostClientRPC? hostInterfact;
        private GuestClientSession? session;
        private TXGuestHeroManager? guestHeroManager;

        public CancellationTokenSource DisconnectToken { get; } = new();

        public LobbyInfo? LobbyInfo { get; set; }
        public GameSessionInfo? gameSessionInfo { get; set; }

        public string Guid { get; set; } = "";

        public async Task Init(string name)
        {

            rpc = remote.Stream.CreateJsonRpc();

            hostInterfact = rpc.Attach<IHostClientRPC>();

            rpc.Disconnected += Rpc_Disconnected;
            rpc.StartListening();

            if (!await hostInterfact.CheckVersion(
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
            gameSessionInfo = await hostInterfact.GetGameSessionInfo();
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
                gameSessionInfo = await hostInterfact.GetGameSessionInfo();

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
            if (e.Reason == DisconnectedReason.LocallyDisposed)
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

        public Task HeroInitDone(bool initdone)
        {
            Debug.Assert(hostInterfact != null);
            hostInterfact.HeroInitDone(true);
            return Task.CompletedTask;
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

        public async Task RefreshLobbyInfo()
        {
            Debug.Assert(hostInterfact != null);
            LobbyInfo = await hostInterfact.GetLobbyInfo();
        }

        public async Task RefreshGameSessionInfo()
        {
            Debug.Assert(hostInterfact != null);
            gameSessionInfo = await hostInterfact.GetGameSessionInfo();
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

        async void IOnGuestHeroInitDone.OnHeroInitDone(Hero hero)
        {
            Debug.Assert(session != null);

            guestHeroManager = await TXGuestHeroManager.CreateAsync(session, Log.ForContext<TXGuestHeroManager>(), hero);
        }
    }
}
