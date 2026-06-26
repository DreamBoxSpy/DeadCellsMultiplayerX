using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class SteamMode : ModeConfig
    {
        public SteamMode(LobbyMenu manager) : base(manager, "SteamP2P") { }

        public override void BuildContent(FlowBox right, int panelW)
        {
            Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile_2.png");
        }

        public override void OnHost(Action onend) { ShowError(() => { }, "Steam连接暂不可用"); }
        public override void OnClient(Action canEnter) { ShowError(() => { }, "Steam连接暂不可用"); }
        public override void Update() { }
        public override void OnHostLeave() { }
        public override void OnClientLeave() { }
        public override void OnHostStartGame() { }

        private void ShowError(HlAction retry, string text = "请输入正确IP及端口")
        {
            logger.Error(text);
            var pop = new ModalPopUp(Ref<bool>.In(false), null);
            pop.text(text.AsHaxeString(), null, default);
            pop.onClose = retry;
        }

    }
}
