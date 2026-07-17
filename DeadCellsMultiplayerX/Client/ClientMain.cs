using dc.shader;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks.Quic;
using DeadCellsMultiplayerX.Server;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore;
using ModCore.Mods;
using ModCore.Utilities;
using DeadCellsMultiplayerX.Client.UI;
using dc;
using ModCore.Modules;
using DeadCellsMultiplayerX.Client.UI.Modes;
using ModCore.Events;
using DeadCellsMultiplayerX.Server.Events;
using dc.en;

namespace DeadCellsMultiplayerX.Client
{
    internal class ClientMain : Module<ClientMain>
    {
        /// <summary>
        /// 当前的 Guest Client 实例
        /// </summary>
        public GuestClient? CurrentGuestClient { get; internal set; }

        /// <summary>
        /// 当前的 Host Client 实例
        /// </summary>
        public HostClient? CurrentHostClient { get; internal set; }

        /// <summary>
        /// 连接大厅
        /// </summary>
        public LobbyMenu? lobby { get; internal set; }

        //初始化客户端
        public void Init()
        {
            Hook_GlowKey.applyGlowData += Hook_GlowKey_applyGlowData;

            Hook_TitleScreen.mainMenu += Hook_TitleScreen_mainMenu;
            Hook__TitleScreen.__constructor__ += Hook__TitleScreen__constructor__;
            Hook_TitleScreen.onDispose += (orig, self) =>
            {
                orig(self);
                EventSystem.BroadcastEvent<IOnLobbyMenuDisposed>();
                lobby?.Hide();
                lobby?.destroy();
            };

            Hook_Game.init += Hook_Game_init;
        }

        private void Hook_Game_init(Hook_Game.orig_init orig, dc.pr.Game self)
        {
            orig(self);
            self.gameSignals.heroInitDone.add(new HlAction<Hero>((hero) => EventSystem.BroadcastEvent<IOnGuestHeroInitDone, Hero>(hero)),
            null, null, null);
        }


        private void Hook_GlowKey_applyGlowData(Hook_GlowKey.orig_applyGlowData orig, GlowKey self,
            int i, Hashlink.Virtuals.virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_ glowData)
        {
            if (self.colorsCount__ <= i)
            {
                self.colorsCount__ = i + 1;
                self.constModified = true;
            }

            orig(self, i, glowData);
        }

        /// <summary>
        /// 清理 Client 实例
        /// </summary>
        public void CleanupClient()
        {
            Logger.Information("Cleaning clients...");

            CurrentGuestClient?.Dispose();
            CurrentHostClient?.Dispose();
            CurrentHostClient = null;
            CurrentGuestClient = null;

            GC.Collect(2, GCCollectionMode.Forced);

            Task.Delay(200).ContinueWith(_ => GC.Collect(2, GCCollectionMode.Forced));
        }

        /// <summary>
        /// 启动 Host 客户端，并加入房间
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public async Task StartHost(string ip, int port)
        {
            CleanupClient();

            var listener = new QuicNetworkListener(ip, port);

            await listener.Init();

            CurrentHostClient = new HostClient(listener, default);
            await CurrentHostClient.Init();

            await StartGuest(ip, port);
        }

        /// <summary>
        /// 启动 Guest 客户端
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task StartGuest(string ip, int port, string? playerName = null)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = Environment.UserName;
            }

            if (CurrentGuestClient != null && !CurrentGuestClient.IsDisposed)
            {
                throw new InvalidOperationException();
            }

            var connect = await QuicNetworkConnect.Connect(ip, port, default);

            CurrentGuestClient = new GuestClient(connect);
            await CurrentGuestClient.Init(playerName);
        }

        #region Menu

        private void Hook__TitleScreen__constructor__(Hook__TitleScreen.orig___constructor__ orig, TitleScreen arg1, bool? playMusic)
        {
            lobby = new(this, arg1);
            orig(arg1, playMusic);

            lobby.createRootInLayers(Main.Class.ME.root, dc.Const.Class.ROOT_DP_MENU);
            lobby.controllerHelper = new(ModConfig.Config, lobby.config.ControlKeys, arg1.controller.parent);
            lobby.fControlLabel = new dc.h2d.Flow(null);
            lobby.setControlLabelKeys();
            lobby.onResize();
            lobby.RegisterMode(new DefaultMode(lobby));
            lobby.RegisterMode(new SteamMode(lobby));

            if (lobby.curTopbts > lobby.modes.Count - 1 || lobby.curTopbts < 0)
                lobby.curTopbts = 0;
        }

        private void Hook_TitleScreen_mainMenu(
            Hook_TitleScreen.orig_mainMenu orig, TitleScreen self)
        {
            orig(self);
            lobby!.BuildMenuChild("Test_Host", () => Test.Start());

            const int Index = 1;

            int color = (255 << 16) | (215 << 8) | 0;
            lobby!.BuildMenuChild("Online", () => lobby.Show(), color: color);

            var wrapper = self.menuItemsWrapper;
            var menu = wrapper.children.getDyn(wrapper.children.length - 1);
            wrapper.removeChild(menu);
            wrapper.addChildAt(menu, Index);

            var item = self.menuItems.pop();
            self.menuItems.insert(Index, item);

            self.fControlLabel.reflow();
            self.select(0, default);
        }
        private static string T(string key) => GetText.Instance.GetString(key);

        #endregion
    }
}
