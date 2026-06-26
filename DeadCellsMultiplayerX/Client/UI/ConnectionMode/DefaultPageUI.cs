using System.Net.Sockets;
using System.Text.RegularExpressions;
using dc;
using dc.ui;
using DeadCellsMultiplayerX.Server.Events;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Utilities;
using Newtonsoft.Json;
using SDL2;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class DefaultMode : ModeConfig,
    IOnGameExit,
    IOnLobbyMenuDisposed,
    IOnServerEnterNewLevel
    {
        private string lastClipboard = "";
        private bool isJoining = false; //是否受邀请进入房间
        private bool loadingTriggered = false; //房主是否开始游戏

        private string ip = "";
        private int port;


        public DefaultMode(LobbyMenu manager) : base(manager, "默认联机") { }

        public override void BuildContent(FlowBox right, int panelW)
            => Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile.png");

        public override void OnHost(Action onend) => StartConnect(true, onend);
        public override void OnClient(Action canEnter) => StartConnect(false, null, canEnter);

        public override void OnHostLeave() { Manager.client.CurrentHostClient?.Dispose(); Manager.client.CurrentGuestClient?.Quit(); }
        public override void OnClientLeave() => Manager.client.CurrentGuestClient?.Quit();

        /// <summary>
        /// 房主开始游戏
        /// </summary>
        public async override void OnHostStartGame()
        {
            var hc = Manager.client.CurrentHostClient;
            if (hc == null || !hc.CanStartGame) return;
            await hc.StartGame();
        }

        public override void Update()
        {
            if (Manager != null && Manager.playerPanel != null && Manager.playerPanel.titletext != null)
            {
                long latency = Manager.GetLatency();

                string hex = latency <= 50 ? "00FF00" :
                        latency <= 100 ? "FFFF00" : "FF0000";
                string text = $"ip:{this.ip}:{this.port} 当前人数:{Manager.GetPlayerCount()} 服务端延迟:<font color=\"#{hex}\">{latency}ms</font>";
                Manager.playerPanel.titletext.set_text(text.AsHaxeString());
            }


            if (Manager?.playerPanel != null && !Manager.isHost)
            {
                bool disconnected = false;
                string reason = "";

                if (Manager.client == null)
                {
                    reason = "客户端未连接";
                    disconnected = true;
                }
                else if (Manager.client.CurrentGuestClient == null)
                {
                    reason = "尚未加入房间";
                    disconnected = true;
                }
                else if (Manager.client.CurrentGuestClient.IsDisposed)
                {
                    reason = "房主已断开连接";
                    disconnected = true;
                }
                else if (Manager.client.CurrentGuestClient.LobbyInfo == null)
                {
                    reason = "房间信息丢失";
                    disconnected = true;
                }

                if (disconnected)
                {
                    Manager.Return();
                    ShowError(() => { }, reason);
                }
            }

            //房主开始游戏时,进入黑屏
            if (!loadingTriggered && Manager?.client != null)
            {
                bool isStarted = Manager.client.CurrentHostClient?.LobbyInfo?.IsStarted == true
                              || Manager.client.CurrentGuestClient?.LobbyInfo?.IsStarted == true;

                if (isStarted)
                {
                    loadingTriggered = true;
                    string loadtext = Manager.isHost
                        ? "正在加载游戏..."
                        : "房主已开始游戏 正在加载...";

                    Manager.LoaddingIn(loadtext, () => { });
                }
            }




            if (Manager?.GetMe() != null) return;
            // 检测粘贴,进入对应房间
            if (!isJoining && TryParseInvite(out var ip, out var port))
            {
                var newip = GenerateInvite(ip, port);
                if (lastClipboard != newip)
                {
                    lastClipboard = newip;
                    ShowJoinConfirm(ip, port);
                }
            }
        }


        void IOnGameExit.OnGameExit()
        {
            var me = Manager.GetMe();
            if (me == null) return;
            if (me.IsHost) OnHostLeave(); else OnClientLeave();
        }

        private void StartConnect(bool asHost, Action? onEnd = null, Action? canEnter = null)
        {
            Manager.lockInter = true;
            string defaultIp = "";
#if DEBUG
            defaultIp = "127.0.0.1:12345";
#endif
            var input = new TextInput(
                Manager,
                "输入ip及端口".AsHaxeString(),
                "test".AsHaxeString(),
                defaultIp.AsHaxeString(),
                (str) =>
                {
                    
                    if (!TryParseIpPort(str.ToString(), out var ip, out var port))
                    {
                        ShowError(asHost ? () => OnHost(onEnd!) : () => OnClient(canEnter!));
                        return;
                    }

                    Manager.LoaddingIn(asHost ? "正在创建房间..." : "正在加入房间...", async () =>
                    {
                        try
                        {
                            if (asHost)
                            {
                                await ClientMain.Instance.StartHost(ip, port);
                                ClientMain.Instance.CurrentGuestClient!.SetReady(true);

                                CopyInvite(ip, port);
                            }
                            else
                            {
                                await ClientMain.Instance.StartGuest(ip, port);
                                // ClientMain.Instance.CurrentGuestClient?.SetReady(true);
                            }
                        }
                        catch (StreamJsonRpc.ConnectionLostException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"连接已断开{ex.Message}");
                            return;
                        }
                        catch (SocketException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"网络错误: {ex.Message}");
                            return;
                        }
                        catch (TimeoutException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"连接超时，请检查对方地址或网络{ex.Message}");
                            return;
                        }
                        catch (OperationCanceledException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"连接超时或已取消{ex.Message}");
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"发生未知错误{ex.Message}");
                            return;
                        }

                        onEnd?.Invoke();
                        canEnter?.Invoke();

                        this.ip = ip;
                        this.port = port;
                    });
                    Manager.delayer.addMs(null, () =>
                    {
                        Manager.LoaddingOut();
                    }, 3000);
                }, "回车确认".AsHaxeString(), null, null);

            input.onDisposeCb = () =>
            {
                Manager.lockInter = false;
            };
        }


        private bool TryParseInvite(out string ip, out int port)
        {
            ip = null!; port = 0;
            if (SDL.SDL_HasClipboardText() != SDL.SDL_bool.SDL_TRUE) return false;
            var match = Regex.Match(SDL.SDL_GetClipboardText(), @"IP:\s*([\d.]+)\s*端口:\s*(\d+)");
            if (!match.Success) return false;
            ip = match.Groups[1].Value;
            return int.TryParse(match.Groups[2].Value, out port);
        }

        private bool TryParseIpPort(string input, out string ip, out int port)
        {
            ip = null!; port = 0;
            var parts = input.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;
            if (!int.TryParse(parts[^1], out port) || port < 0 || port > 65535) return false;
            ip = parts[0];
            return true;
        }

        /// <summary>
        /// 邀请文字
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private string GenerateInvite(string ip, int port)
            => $"复制此文字打开死亡细胞联机模组即可加入我的房间 IP: {ip} 端口: {port}";

        /// <summary>
        /// 提示以复制邀请
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        private void CopyInvite(string ip, int port)
        {
            lastClipboard = GenerateInvite(ip, port);
            SDL.SDL_SetClipboardText(lastClipboard);
            new Confirmation(Manager, "邀请已复制".AsHaxeString(), () => { }, null, "好的".AsHaxeString(), null, null);
        }

        /// <summary>
        /// 进入粘贴面板中的房间
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        private void ShowJoinConfirm(string ip, int port)
        {
            isJoining = true;
            Manager.lockInter = true;
            new Confirmation(
                Manager,
                $"检测到邀请：{ip}:{port}\n是否加入？".AsHaxeString(),
                () =>
                {
                    isJoining = false;
                    Manager.LoaddingIn("正在加入房间...", async () =>
                    {
                        this.ip = ip;
                        this.port = port;

                        try
                        {
                            await ClientMain.Instance.StartGuest(ip, port);
                            // ClientMain.Instance.CurrentGuestClient?.SetReady(true);
                        }
                        catch (StreamJsonRpc.ConnectionLostException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"连接已断开:{ex.Message}");
                            return;
                        }
                        catch (SocketException ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"网络错误: {ex.Message}");
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            ShowError(() => Manager.RefreshUI(), $"发生错误:{ex.Message}");
                            return;
                        }

                        Manager.OpenPanel(false);
                        Manager.AddClientButtons();
                        Manager.onResize();

                    });
                    Manager.delayer.addMs(null, () => Manager.LoaddingOut(), 3000);
                },
                () => { isJoining = false; Manager.lockInter = false; },
                "加入".AsHaxeString(),
                "取消".AsHaxeString(),
                null
            );
        }

        /// <summary>
        /// 显示报错
        /// </summary>
        /// <param name="retry"></param>
        /// <param name="text"></param>
        private void ShowError(HlAction retry, string text = "请输入正确IP及端口")
        {
            logger.Error(text);
            Manager.LoaddingOut();
            var pop = new ModalPopUp(Ref<bool>.In(false), null);
            pop.text(text.AsHaxeString(), null, default);
            pop.onClose = retry;
        }

        void IOnLobbyMenuDisposed.OnLobbyMenuDisposed()
        {
            if (loadingTriggered)
            {
                Manager.LoaddingOut();
                loadingTriggered = false;
            }

        }

        void IOnServerEnterNewLevel.OnServerEnterNewLevel()
        {

        }
    }
}