using System.Linq;
using System.Net.Http.Headers;
using System.Windows.Controls;
using CoreLibrary.Utilities;
using dc;
using dc.h2d;
using dc.h2d.col;
using dc.hl.types;
using dc.hxd;
using dc.hxd.res;
using dc.libs.heaps.slib;
using dc.libs.misc;
using dc.pr;
using dc.ui;
using DeadCellsMultiplayerX.Client.UI.Modes;
using Hashlink.Virtuals;
using HashlinkNET.Native.Impl;
using HaxeProxy.Runtime;
using ModCore.Modules;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal class LobbyMenu : dc.ui.Process
    {
        public readonly ClientMain client;
        private readonly ILogger logger;
        private ModeConfig? currentMode;
        public PlayerSlotPanel? playerPanel; //玩家显示面板
        public ModConfig config = null!;
        public TitleScreen titleScreen { get; private set; } = null!;
        public ControllerHelperSuper<ModConfig> controllerHelper = null!; //控制器

        public readonly List<ModeConfig> modes = [];
        private readonly List<BtnData> btns = [];   //左侧按钮
        private readonly List<BtnData> topbtns = [];    //顶部按钮
        private readonly List<ReleaseNotes> notes = [];   //更新记录


        private readonly Dictionary<ControllerKey, int> keys = [];    // 按键act
        public readonly Dictionary<Flow, (Loading, Mask)> loadingFlow = [];


        public Action? onResizeAllloadingFlow;

        // 布局
        public FlowBox? mainFlow;
        internal FlowBox? rightFlow;
        private Flow? tabFlow;
        public Flow? leftFlow;
        public Flow? rightFlowMain;
        public Flow? bodyFlow;
        private Flow? infoFlow;



        private HSprite? selection;
        private HSprite? tapselection;
        private ArrayObj? defaultbtns;
        private Tile? Imagetile;
        private Graphics? spacer;

        internal int ScreenW { get; private set; }
        internal int ScreenH { get; private set; }
        internal int PanelW { get; private set; }
        public bool lockInter { get; set; }
        private int frameCounter { get; set; }
        public bool isHost { get; private set; }
        public int curTopbts
        {
            get => config.currentMode;
            set
            {
                config.currentMode = value;
                ModConfig.Config.Save();
            }
        }
        public int curleftbtn { get; set; } = 0;
        private long latencyMs;


        public long GetLatency() => latencyMs;

        /// <summary>
        /// 获取屏幕大小
        /// </summary>
        private void CalcLayout()
        {
            ScreenW = dc.libs.Process.Class.CUSTOM_STAGE_WIDTH > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_WIDTH
                : dc.hxd.Window.Class.getInstance().get_width();
            ScreenH = dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT
                : dc.hxd.Window.Class.getInstance().get_height();
            PanelW = (int)(ScreenW * 0.60);
        }

        public LobbyMenu(ClientMain client, TitleScreen titleScreen) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
            this.config = ModConfig.Config.Value;
            this.titleScreen = titleScreen;

            //调整loadding大小
            onResizeAllloadingFlow = () =>
            {
                foreach (var item in loadingFlow)
                {
                    var load = item.Value.Item1;
                    var mask = item.Value.Item2;
                    var flow = item.Key;

                    flow.addChild(mask);

                    mask.width = flow.minWidth ?? flow.borderWidth;
                    mask.height = flow.minHeight ?? flow.borderHeight;

                    load.onResize(mask.width, mask.height);
                }
            };
        }
        /// <summary>
        /// 注册游戏模式
        /// </summary>
        /// <param name="mode">ModeConfig 子类实例</param>
        public void RegisterMode(ModeConfig mode) => modes.Add(mode);


        public GuestInfo? GetMe()
        {
            var lobby = isHost
                ? client.CurrentHostClient?.LobbyInfo
                : client.CurrentGuestClient?.LobbyInfo;
            var gc = client.CurrentGuestClient;
            if (lobby == null || gc == null) return null;
            return lobby.Guests.Values.FirstOrDefault(g => g.Guid == gc.Guid);
        }

        /// <summary>
        /// 获取房间人数
        /// </summary>
        /// <returns></returns>
        public int GetPlayerCount()
        {
            var lobby = isHost
                ? client.CurrentHostClient?.LobbyInfo
                : client.CurrentGuestClient?.LobbyInfo;
            return lobby?.Guests.Count ?? 0;
        }

        /// <summary>
        /// 获取延迟
        /// </summary>
        /// <returns></returns>
        public async Task RefreshLatency()
        {
            var gc = client.CurrentGuestClient;
            if (client == null || gc == null) return;
            latencyMs = await gc.Ping();
        }


        public void Show()
        {
            setControlLabel();
            Hide();
            BuildUI();
            BuildInformation();

            titleScreen.controller.manualLock = true;
            titleScreen.blur(default, default);
        }

        public void Hide()
        {
            mainFlow?.removeChildren();
            mainFlow?.remove(); mainFlow = null;
            infoFlow?.remove(); infoFlow = null;
            tabFlow = null; leftFlow = null; rightFlow = null;
            currentMode = null;
            loadingFlow.Clear();
            btns.Clear();
            topbtns.Clear();
            titleScreen.controller.manualLock = false;
            titleScreen.unblur();
        }

        /// <summary>
        /// 重新构建
        /// </summary>
        public void RefreshUI() { Hide(); Show(); }

        public void SelectMode(ModeConfig mode)
        {
            if (rightFlow == null) return;
            currentMode = mode;
            BuildRightMode(mode);
            mainFlow!.reflow();


            void BuildRightMode(ModeConfig mode)
            {
                if (rightFlow == null) return;
                ClearContent(rightFlow);
                WriteTestLog();
                mode.BuildContent(rightFlow, PanelW);
                rightFlow.reflow();
            }
        }

        /// <summary>
        /// 打开面板
        /// </summary>
        /// <param name="ishost"></param>
        public void OpenPanel(bool ishost)
        {
            if (rightFlow == null || leftFlow == null) return;
            this.isHost = ishost;


            ClearContent(rightFlow);
            ClearContent(leftFlow!);

            playerPanel = new PlayerSlotPanel(rightFlow, loadingFlow[rightFlow].Item1);
            RefreshLobbySlots();

            loadingFlow[rightFlow].Item2.set_visible(true);

            rightFlow.reflow();
            leftFlow.reflow();
            btns.Clear();
        }

        #region LeftButton
        /// <summary>
        /// 左侧按钮玩家面板
        /// </summary>
        private void BuildDefaultButtons()
        {
            btns.Clear();
            ClearContent(leftFlow!);

            BuildLeftBtn(T("CreateRoom"), () => { if (currentMode != null) ShowHost(currentMode); onResize(); });
            BuildLeftBtn(T("JoinRoom"), () => { if (currentMode != null) ShowClient(currentMode); onResize(); });
            currentMode?.BuildMenu();
            BuildLeftBtn(T("return"), () => delayer.addF(null, Hide, 1));

            void ShowHost(ModeConfig mode)
            {
                mode.OnHost(() =>
                {
                    logger.Information($"modetype:{mode.GetType().Name}");
                    OpenPanel(true);
                    AddHostButtons();
                });
            }

            void ShowClient(ModeConfig mode)
            {
                mode.OnClient(() =>
                {
                    OpenPanel(false);
                    AddClientButtons();
                });
            }
        }

        /// <summary>
        /// 房主按钮
        /// </summary>
        public void AddHostButtons()
        {
            BuildLeftBtn("开始游戏", async () =>
            {
                var players = client?.CurrentGuestClient?.LobbyInfo?.Guests;
                if (players == null)
                {
                    ShowError(() => { }, "连接异常无法开始游戏");
                    return;
                }

                bool canstart = true;
                List<string> name = [];
                foreach (var item in players)
                {
                    if (!item.Value.IsReady)
                    {
                        canstart = false;
                        name.Add(item.Value.Name);
                    }

                }
                if (!canstart)
                {
                    ShowError(() =>
                    {
                        RefreshLobbySlots();
                    }, $"玩家: {string.Join(",", name)} 未准备，无法开始游戏");
                    return;
                }
                currentMode?.OnHostStartGame();
                onResize();
            });
            BuildLeftBtn(T("return") + T("并销毁房间"), () =>
            {
                currentMode?.OnHostLeave();
                Return();
            });

            void ShowError(HlAction retry, string text = "")
            {
                logger.Error(text);
                var pop = new ModalPopUp(Ref<bool>.In(false), null);
                pop.text(text.AsHaxeString(), null, default);
                pop.onClose = retry;
            }
        }

        /// <summary>
        /// 客户按钮
        /// </summary>
        public void AddClientButtons()
        {
            BuildLeftBtn("准备 / 取消", () =>
            {
                var me = GetMe();
                if (me == null) return;
                client.CurrentGuestClient!.SetReady(!me.IsReady);
                me.IsReady = !me.IsReady;
                RefreshLobbySlots();
            });
            BuildLeftBtn(T("return") + T("并离开房间"), () =>
            {
                currentMode?.OnClientLeave();
                Return();
            });
        }


        /// <summary>
        /// 返回lobby主页
        /// </summary>
        public void Return()
        {
            RefreshUI();
            loadingFlow[rightFlow!].Item2.set_visible(false);
            playerPanel?.ClearAll();
            playerPanel = null;
        }



        /// <summary>
        /// 刷新玩家面板
        /// </summary>
        public void RefreshLobbySlots()
        {
            if (playerPanel == null) return;

            var lobby = isHost
                ? client.CurrentHostClient?.LobbyInfo
                : client.CurrentGuestClient?.LobbyInfo;

            if (lobby != null)
                playerPanel.Refresh(lobby.Guests.Values);
            else
                playerPanel.ClearAll();
        }
        #endregion



        #region updates
        public override void update()
        {
            base.update();

            if (currentMode != null)
            {
                currentMode.Update();
            }

            // 每15帧刷新一次玩家卡槽
            if (playerPanel != null && frameCounter++ % 15 == 0)
            {
                RefreshLobbySlots();
            }

            //更新延迟
            if (currentMode is DefaultMode)
            {
                if (frameCounter % 180 == 0)
                    _ = RefreshLatency();
            }

            if (selection != null)
                updateHSprite(selection);
            if (tapselection != null)
                updateHSprite(tapselection);

            //联机模式切换
            if (controllerHelper != null && defaultbtns != null && !lockInter)
            {
                if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_E]))
                    SwitchMode(1);

                if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_Q]))
                    SwitchMode(-1);

                if (controllerHelper.IsPressed(keys[ControllerKey.Default_UP]))
                    MoveLeftBtn(-1);

                if (controllerHelper.IsPressed(keys[ControllerKey.Default_DOWN]))
                    MoveLeftBtn(1);

                if (controllerHelper.IsPressed(keys[ControllerKey.Confirm]))
                {
                    if (btns.Count == 0) return;
                    var btn = btns[curleftbtn];
                    btn?.interactive?.onClick?.Invoke(null!);
                }
            }




            void SwitchMode(int step)
            {
                if (topbtns.Count == 0) return;

                curTopbts += step;

                if (curTopbts < 0)
                    curTopbts = modes.Count - 1;
                else if (curTopbts >= modes.Count)
                    curTopbts = 0;

                SelectMode(modes[curTopbts]);

                var btn = topbtns[curTopbts];
                btn?.interactive?.onClick?.Invoke(null!);

                logger.Information($"modes: {currentMode?.GetType().Name}");
            }


            void updateHSprite(HSprite spr)
            {
                double? v = ((HaxeProxyBase)Data.Class.gui.byId.get("co_blinkCursorSpeed".AsHaxeString())).ToVirtual<virtual_biome_color_comment_id_v0_>().v0;
                if (v == null) return;
                double cos = Lib_std.math_cos.Invoke((double)(ftime * 0.1 * v!));
                spr.alpha = 0.8 + 0.2 * cos;
            }

        }


        /// <summary>
        /// 调整ui大小
        /// </summary>
        public override void onResize()
        {
            base.onResize();
            CalcLayout();

            if (bodyFlow != null)
            {
                bodyFlow.set_minHeight((int?)(ScreenH * 0.95));
                bodyFlow.reflow();
            }

            if (mainFlow == null) return;

            int leftW = ScreenW / 4;
            mainFlow.set_minWidth(ScreenW);
            mainFlow.set_minHeight(ScreenH);
            mainFlow.set_maxWidth(ScreenW);
            mainFlow.set_maxHeight(ScreenH);
            mainFlow.reflow();

            if (leftFlow != null)
            {
                leftFlow.set_minWidth(leftW);
                leftFlow.reflow();
            }

            if (rightFlowMain != null)
            {
                rightFlowMain.set_minWidth(PanelW);
                rightFlowMain.set_maxWidth(PanelW);
                rightFlowMain.set_minHeight(ScreenH / 2);
                rightFlowMain.reflow();
            }

            if (rightFlow != null)
            {
                rightFlow.set_minWidth(PanelW);
                rightFlow.set_maxWidth(PanelW);
                rightFlow.set_minHeight((int?)(ScreenH / 1.8));
                rightFlow.reflow();

                Imagetile?.width = (int)rightFlow.minWidth!;
                Imagetile?.height = (int)rightFlow.minHeight!;
            }

            if (tabFlow != null)
            {
                tabFlow.set_horizontalSpacing(pixel(6));
                foreach (var item in tabFlow.children)
                {
                    if (item is not ControlIcon e) continue;
                    e.scaleX = e.scaleY = get_pixelScale.Invoke() * 0.5;

                    tabFlow.getProperties(e).paddingTop = pixel(5);
                    tabFlow.getProperties(e).paddingTop = pixel(5);
                }
            }

            if (tabFlow != null)
                tabFlow.reflow();

            onResizeAllloadingFlow?.Invoke();


            foreach (var item in btns)
            {
                item?.OnReszie?.Invoke();
            }

            foreach (var item in topbtns)
            {
                item?.OnReszie?.Invoke();
            }

            delayer.addF(null, () => topbtns[curTopbts]?.OnReszie?.Invoke(), 1);


            if (selection != null)
            {
                selection.scaleX = selection.scaleY = get_pixelScale.Invoke();
                selection.posChanged = true;
            }
            if (tapselection != null)
            {
                tapselection.scaleX = tapselection.scaleY = get_pixelScale.Invoke() / 2;
                tapselection.posChanged = true;
            }

            if (spacer != null)
            {
                spacer.clear();
                spacer.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.1));
                spacer.drawRect(0, 0, ScreenW / 2, pixel(1));
                spacer.endFill();
            }

            currentMode?.onResize();

            if (playerPanel != null)
            {
                playerPanel.Container.set_minWidth(rightFlow?.minWidth ?? PanelW);
                playerPanel.title.set_minWidth(rightFlow?.minWidth ?? PanelW);
                playerPanel.title.set_minHeight(rightFlow?.minHeight / 10);
                playerPanel.title.set_maxHeight(rightFlow?.minHeight / 10);
                ApplyHTMLFont(playerPanel.titletext, get_pixelScale.Invoke() * 0.25);
                playerPanel.titletext.posChanged = true;
                playerPanel.Container.reflow();
                playerPanel.title.reflow();
                playerPanel.RefreshStatuses();
                foreach (var slot in playerPanel.Slots)
                {
                    slot.OnReszie();
                }
            }


            delayer.addF(null, () =>
            {
                BuildInformation();
                onResizeAllloadingFlow?.Invoke();

                if (btns.Count != 0)
                {
                    while (btns.Count < curleftbtn)
                    {
                        curleftbtn--;
                    }
                    btns[curleftbtn]?.interactive?.onMove?.Invoke(null!);
                }

            }, 1);

        }
        #endregion

        #region Control
        //注册按键
        public void setControlLabelKeys()
        {
            var info = ModEntry.Instance.Info;
            const string Lobby_Tab_Exchange_Q = "Lobby_Tab_Exchange_Q";
            const string Lobby_Tab_Exchange_E = "Lobby_Tab_Exchange_E";

            keys.Add(ControllerKey.Tab_Exchange_Q, controllerHelper.GetAction(Lobby_Tab_Exchange_Q)
            ?? controllerHelper.AddKey(info, Lobby_Tab_Exchange_Q, KeyHelper.Q));

            keys.Add(ControllerKey.Tab_Exchange_E, controllerHelper.GetAction(Lobby_Tab_Exchange_E)
            ?? controllerHelper.AddKey(info, Lobby_Tab_Exchange_E, KeyHelper.E));

            keys.Add(ControllerKey.Default_UP, 10);
            keys.Add(ControllerKey.Default_DOWN, 12);
            keys.Add(ControllerKey.Default_Left, 11);
            keys.Add(ControllerKey.Default_Rigth, 13);
            keys.Add(ControllerKey.Confirm, 14);
            keys.Add(ControllerKey.Cancel, 16);


            defaultbtns = (ArrayObj)ArrayUtils.CreateDyn().array;
            defaultbtns.push(creataBCreateButton(14, "Valider"));
            defaultbtns.push(creataBCreateButton(16, "Retour"));

            virtual_acts_cond_label_onAdd_ creataBCreateButton(int actionId, string labelText)
            {
                ArrayBytes_Int acts = ArrayUtils.CreateInt();
                acts.push(actionId);
                var buttonConfig = new virtual_acts_cond_label_
                {
                    acts = acts,
                    label = Lang.Class.t.get(labelText.AsHaxeString(), null),
                    cond = null
                };
                return buttonConfig.ToVirtual<virtual_acts_cond_label_onAdd_>();
            }
        }

        //设置按键提示
        public void setControlLabel()
        {
            Flow flow = fControlLabel;
            flow.reflow();
            createControlLabel(defaultbtns);
            const double SIZE = 0.7;
            for (int i = 0; i < flow.children.length; i++)
            {
                var contor = flow.children.getDyn(i) as ControlLabel;
                if (contor == null)
                    continue;
                contor.scaleX = contor.scaleY = SIZE;
            }
            flow.reflow();

        }
        #endregion

        #region UI Building
        private void BuildUI()
        {
            CalcLayout();
            int tabH = pixel(36);
            int gap = pixel(8);
            int leftW = ScreenW / 4;

            // 背景容器
            mainFlow = CreateBoxLegendaryOutline(null, true);
            mainFlow.set_isVertical(true);
            mainFlow.set_minWidth(ScreenW);
            mainFlow.set_minHeight(ScreenH);
            mainFlow.set_maxWidth(ScreenW);
            mainFlow.set_maxHeight(ScreenH);
            mainFlow.set_horizontalAlign(new FlowAlign.Middle());
            root.addChildAt(mainFlow, 1);

            // 顶部标签
            tabFlow = new Flow(mainFlow);
            tabFlow.set_isVertical(false);
            tabFlow.set_horizontalAlign(new FlowAlign.Middle());
            tabFlow.set_verticalAlign(new FlowAlign.Top());
            tabFlow.set_horizontalSpacing(pixel(6));

            tapselection = new HSprite(Assets.Class.ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            tapselection.scaleX = tapselection.scaleY = get_pixelScale.Invoke() / 2;
            var pivot = tapselection.pivot;
            pivot.centerFactorX = 0;
            pivot.centerFactorY = 0;
            pivot.usingFactor = true;
            pivot.isUndefined = false;
            tabFlow.addChild(tapselection);
            tabFlow.getProperties(tapselection).set_isAbsolute(true);

            //切换按钮
            var q = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_Q], 0, 0, tabFlow);
            var e = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_E], 0, 0, tabFlow);
            q.scaleX = q.scaleY = e.scaleX = e.scaleY = get_pixelScale.Invoke() * 0.5;

            tabFlow.getProperties(e).paddingTop = pixel(5);
            tabFlow.getProperties(q).paddingTop = pixel(5);

            topbtns.Clear();
            foreach (var m in modes)
            {
                var mode = m;
                BuildTab(tabFlow, mode.Name, () => { });
            }
            tabFlow.reflow();

            Point point = tapselection!.parent.globalToLocal(topbtns[curTopbts].text!.localToGlobal(null));
            tapselection.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
            tapselection.y = point.y;
            tapselection.posChanged = true;

            // 上下分隔
            spacer = new Graphics(mainFlow);
            spacer.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.1));
            spacer.drawRect(0, 0, ScreenW / 2, pixel(1));
            spacer.endFill();

            // 主体
            bodyFlow = new Flow(mainFlow);
            bodyFlow.set_isVertical(false);
            bodyFlow.set_paddingTop(gap);
            bodyFlow.set_minHeight((int?)(ScreenH * 0.95));
            bodyFlow.set_horizontalAlign(new FlowAlign.Middle());
            bodyFlow.set_verticalAlign(new FlowAlign.Middle());

            // 左侧
            leftFlow = new Flow(bodyFlow);
            leftFlow.set_isVertical(true);
            leftFlow.set_minWidth(leftW);
            leftFlow.set_verticalSpacing(pixel(6));
            leftFlow.set_horizontalAlign(new FlowAlign.Middle());
            leftFlow.set_verticalAlign(new FlowAlign.Middle());

            selection = new HSprite(Assets.Class.ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            selection.visible = false;
            selection.scaleX = selection.scaleY = get_pixelScale.Invoke();
            pivot = selection.pivot;
            pivot.centerFactorX = 0;
            pivot.centerFactorY = 0;
            pivot.usingFactor = true;
            pivot.isUndefined = false;
            leftFlow.addChild(selection);
            leftFlow.getProperties(selection).set_isAbsolute(true);

            //右侧
            rightFlowMain = new Flow(null);
            rightFlowMain.set_isVertical(true);
            rightFlowMain.set_minWidth(PanelW);
            rightFlowMain.set_maxWidth(PanelW);
            rightFlowMain.set_minHeight(ScreenH / 2);
            rightFlowMain.set_verticalSpacing(pixel(4));
            rightFlowMain.set_horizontalAlign(new FlowAlign.Middle());
            rightFlowMain.set_verticalAlign(new FlowAlign.Middle());
            bodyFlow.addChildAt(rightFlowMain, 1);

            // 右侧内容面板
            rightFlow = CreateBoxLegendaryOutline(rightFlowMain);
            rightFlow.set_isVertical(true);
            rightFlow.set_minWidth(PanelW);
            rightFlow.set_maxWidth(PanelW);
            rightFlow.set_minHeight((int?)(ScreenH / 1.8));
            rightFlow.set_verticalSpacing(pixel(4));
            rightFlow.set_horizontalAlign(new FlowAlign.Middle());
            rightFlow.set_verticalAlign(new FlowAlign.Middle());
            mainFlow.reflow();
            mainFlow.x = 0;


            if (curTopbts >= modes.Count) curTopbts = 0;
            if (modes.Count > 0) SelectMode(modes[curTopbts]);


            BuildDefaultButtons();


            if (fControlLabel is not null)
            {
                fControlLabel.set_verticalSpacing(pixel(10));
                fControlLabel.reflow();
                rightFlowMain!.addChild(fControlLabel);
            }

        }

        /// <summary>
        /// 右侧 下ui
        /// </summary>
        private void BuildInformation()
        {
            infoFlow?.remove();
            infoFlow = new Flow(rightFlowMain);

            int w = PanelW / 2;
            int h = ScreenH / 4;


            var container = CreateBoxLegendaryOutline(infoFlow);
            container.set_isVertical(true);
            container.set_minWidth(w);
            container.set_maxWidth(w);
            container.set_minHeight(h);
            container.set_horizontalAlign(new FlowAlign.Middle());
            container.reflow();

            var box = container.box;
            double ps = get_pixelScale.Invoke();
            int borderPadW = (int)(box.borderW * ps);
            int borderPadH = (int)(box.borderH * ps);
            int maskW = box.wid - borderPadW * 2;
            int maskH = box.hei - borderPadH * 2;

            double invScale = 1.0 / ps;
            var mask = new Mask(maskW, maskH, null)
            {
                x = box.borderW,
                y = box.borderH,
                scaleX = invScale,
                scaleY = invScale,
                width = maskW,
                height = maskH,
            };
            box.addChildAt(mask, 1);


            int scrollPad = pixel(10);
            var contentContainer = new dc.h2d.Object(null);
            mask.addChild(contentContainer);
            contentContainer.x = pixel(10);
            contentContainer.y = scrollPad;

            var contentFlow = new Flow(contentContainer);
            contentFlow.set_isVertical(true);
            contentFlow.set_minWidth(maskW - pixel(20));
            contentFlow.set_maxWidth(maskW - pixel(20));
            AddText("Release Notes", null, contentFlow, new FlowAlign.Middle(), 2);

            foreach (var note in notes)
            {
                var header = AddText(
                    $"Version:{note.Version}\nLast updated:{note.ReleaseTime}",
                    null, contentFlow, new FlowAlign.Left(), 1.5, 10);

                foreach (var change in note.Changes)
                    AddText($"{change.Type}:{change.Description}",
                        GetColor(change.Type), contentFlow, new FlowAlign.Left(), 1.4, 30);

                contentFlow.getProperties(header).paddingTop = pixel(10);
            }

            contentFlow.reflow();
            var bounds = contentContainer.getBounds(null, null);
            double maxScrollY = System.Math.Max(0, bounds.yMax - bounds.yMin - maskH);

            var inter = new Interactive(maskW, maskH, mask, null) { propagateEvents = true };
            inter.onWheel = e =>
            {
                if (e.wheelDelta == 0) return;
                double newY = contentContainer.y - e.wheelDelta * pixel(30);
                newY = System.Math.Max(-maxScrollY - scrollPad, System.Math.Min(scrollPad, newY));
                contentContainer.posChanged = true;
                contentContainer.y = newY;
            };

            // Mod info box
            var modBox = CreateBoxLegendaryOutline(infoFlow);
            modBox.set_isVertical(true);
            modBox.set_minWidth(w);
            modBox.set_maxWidth(w);
            modBox.set_minHeight(h);
            modBox.set_horizontalAlign(new FlowAlign.Middle());
            modBox.reflow();

            var mod = ModEntry.Instance.Info;
            AddText("Mod Information:", null, modBox, new FlowAlign.Middle(), 2, paddingTop: 8);
            AddText($"Mod name: {mod.Name}", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: 8);
            AddText("Lead author: HKLab", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: 8);
            AddText("Contribution: ChiuYi", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: 8);
            AddText($"Current version: {mod.Version}", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: 8);

            static int GetColor(ChangeType type) => type switch
            {
                ChangeType.Feature => 0x00FF00,
                ChangeType.Improvement => 0x0000FF,
                ChangeType.BugFix => 0xFF0000,
                ChangeType.Breaking => 0xFF00FF,
                _ => 0xFFFFFF,
            };
        }

        #endregion

        #region Dispose
        /// <summary>
        /// 清除flow子项
        /// </summary>
        /// <param name="flow"></param>
        public void ClearContent(Flow flow)
        {
            var len = flow.children.length;
            for (int i = len - 1; i >= 0; i--)
            {
                var child = (dc.h2d.Object)flow.children.getDyn(i);
                var props = flow.getProperties(child);
                if (props == null || !props.isAbsolute)
                    flow.removeChild(child);
            }
        }


        public override void onDispose()
        {
            mainFlow?.removeChildren();
            mainFlow?.remove();

            mainFlow = null;
            tabFlow = null;
            leftFlow = null;
            rightFlow = null;
            currentMode = null;
            rightFlowMain = null;
            base.onDispose();
        }
        #endregion

        #region Helper
        /// <summary>
        /// 按钮构建
        /// </summary>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        public void BuildLeftBtn(string label, Action cb)
        {
            var pixe = get_pixelScale.Invoke();
            var btn = new Flow(leftFlow);
            btn.set_padding(pixel(6));
            btn.set_minWidth(PanelW / 3);
            var text = Assets.Class.makeMedievalText(label.AsHaxeString(), null, btn, null);
            text.scaleX = text.scaleY = get_pixelScale.Invoke() / 2;
            btn.set_enableInteractive(true);
            btn.reflow();



            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.cursor = new Cursor.Button();
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(e =>
                {
                    if (lockInter) return;
                    cb(); AudioHelper.LoadAudioFormString("sfx/ui/menu_select.wav");
                });
                btn.interactive.onOver = new(e =>
                {
                    if (lockInter) return;
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");

                });
                btn.interactive.onMove = new(e =>
                {
                    if (selection == null || lockInter) return;

                    selection.visible = true;
                    Point point = selection.parent.globalToLocal(text.localToGlobal(null));
                    HSprite sel = selection;
                    sel.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
                    sel.y = point.y;
                    sel.posChanged = true;
                });
            }

            var resize = new Action(() =>
            {
                btn.interactive?.onMove.Invoke(null);
                btn.set_padding(pixel(6));
                btn.set_minWidth(PanelW / 3);
                text.scaleX = text.scaleY = get_pixelScale.Invoke() / 2;
                text.posChanged = true;
                text.onResize();
                btn.reflow();
            });

            var data = new BtnData
            {
                Flow = btn,
                text = text,
                interactive = btn.interactive,
                OnReszie = resize
            };

            btns.Add(data);
        }

        private void MoveLeftBtn(int step)
        {
            if (btns.Count == 0) return;
            curleftbtn += step;

            if (curleftbtn < 0)
                curleftbtn = btns.Count - 1;
            else if (curleftbtn >= btns.Count)
                curleftbtn = 0;

            var btn = btns[curleftbtn];
            btn?.interactive?.onMove?.Invoke(null!);
            AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
        }




        /// <summary>
        /// 联机模式
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        private void BuildTab(Flow parent, string label, Action cb)
        {
            var btn = new Flow(null);
            btn.set_padding(pixel(4));
            btn.set_minWidth(pixel(70));
            btn.set_horizontalAlign(new FlowAlign.Middle());

            var text = Assets.Class.makeMedievalText(label.AsHaxeString(), null, btn, null);
            text.scaleX = text.scaleY = get_pixelScale.Invoke() * 0.25;
            text.posChanged = true;

            btn.set_enableInteractive(true);
            btn.reflow();

            parent.addChildAt(btn, 2);


            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.cursor = new Cursor.Button();
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(e =>
                {
                    if (lockInter) return;
                    cb();
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
                    Point point = tapselection!.parent.globalToLocal(topbtns[curTopbts].text!.localToGlobal(null));
                    HSprite sel = tapselection;
                    sel.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
                    sel.y = point.y;
                    sel.posChanged = true;
                });
                btn.interactive.onOver = new(_ =>
                {
                    if (lockInter) return;
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
                });
            }

            var resize = new Action(() =>
            {
                btn.set_padding(pixel(4));
                btn.set_minWidth(pixel(70));
                text.scaleX = text.scaleY = get_pixelScale.Invoke() * 0.25;
                text.posChanged = true;
                btn.reflow();

                Point point = tapselection!.parent.globalToLocal(topbtns[curTopbts].text!.localToGlobal(null));
                HSprite sel = tapselection;
                sel.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
                sel.y = point.y;
                sel.posChanged = true;
            });

            var data = new BtnData
            {
                Flow = btn,
                text = text,
                interactive = btn.interactive,
                OnReszie = resize
            };

            topbtns.Add(data);
        }



        private void WriteTestLog()
        {
            for (int i = 0; i < 15; i++)
            {
                var note = new ReleaseNotes("2.1.0",
                    new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)), "Summer Update");
                note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了深色模式"));
                note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
                notes.Add(note);
            }
        }

        /// <summary>
        /// logo显示
        /// </summary>
        /// <param name="Path"></param>
        public void LoadImageTorightFlow(string Path)
        {
            var Image = Res.Class.load(Path.AsHaxeString()).toTexture();
            Imagetile = Tile.Class.fromTexture(Image);
            Imagetile.width = (int)rightFlow!.minWidth!;
            Imagetile.height = (int)rightFlow.minHeight!;
            var Imagemap = new Bitmap(Imagetile, null);
            rightFlow.addChildAt(Imagemap, 1);
        }


        public dc.ui.Text AddText(string text, int? color, Flow parent, FlowAlign hAlign,
            double scale = 1.0, int padLeft = 0, int paddingTop = 0)
        {
            var t = Assets.Class.makeText(text.AsHaxeString(), color ?? 0xFFFFFF, null, parent);
            t.scaleX = t.scaleY = get_pixelScale.Invoke() * 0.25 * scale;
            parent.getProperties(t).horizontalAlign = hAlign;
            if (padLeft > 0) parent.getProperties(t).paddingLeft = pixel(padLeft);
            if (paddingTop > 0) parent.getProperties(t).paddingTop = pixel(paddingTop);
            return t;
        }



        public string T(string str) => GetText.Instance.GetString(str);
        public dc.String HT(string str) => GetText.Instance.GetString(str).AsHaxeString();

        public FlowBox CreateBoxLegendaryOutline(dc.h2d.Object? p, bool boxspr = true)
        {
            FlowBox flowBox = FlowBox.Class.createBoxValidation(p, default, default, Ref<bool>.In(true), null);

            var gui = Data.Class.gui.byId;
            var blue = gui.get("co_defaultOpacityBlue".AsHaxeString());
            var black = gui.get("co_defaultOpacityBlack".AsHaxeString());
            flowBox.box.alpha = blue != null ? ((HaxeProxyBase)blue).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.8 : 0.8;
            flowBox.blackBG.alpha = black != null ? ((HaxeProxyBase)black).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.5 : 0.5;
            flowBox.box.bgDuo.alpha = 0.5;

            flowBox.box.sg.onParentChanged();

            var mask = new Mask((int)flowBox.realMaxWidth, (int)flowBox.realMaxHeight, flowBox);
            flowBox.getProperties(mask).set_isAbsolute(true);
            var loadingobj = new Loading(mask);
            loadingobj.bgMask.set_visible(false);
            mask.set_visible(false);
            loadingobj.onResize(mask.width, mask.height);

            loadingFlow.Add(flowBox, (loadingobj, mask));


            return flowBox;
        }


        /// <summary>
        /// 黑屏显示正在加载
        /// </summary>
        /// <param name="text"></param>
        /// <param name="onend"></param>
        /// <param name="s"></param>
        public void LoaddingIn(string text, HlAction onend, double s = 0.5)
        {
            onResizeAllloadingFlow?.Invoke();

            var item = loadingFlow[mainFlow!];
            var loading = item.Item1;
            var mask = item.Item2;

            if (loading == null) return;

            loading.text?.remove();
            loading.text = Assets.Class.makeMedievalText.Invoke(text.AsHaxeString(), null, loading.loadingFlow, null);
            loading.onResize(ScreenW, ScreenH);

            mask.set_visible(true);
            loading.bgMask.set_visible(true);

            mask.alpha = 0;
            lockInter = true;
            mask.posChanged = true;
            loading.posChanged = true;
            var animin = CreateTween(() => mask.alpha, (v) => { mask.alpha = v; mask.posChanged = true; }, 1.0, s);
            animin.end(onend);
        }

        /// <summary>
        /// 完成加载
        /// </summary>
        /// <param name="s"></param>
        public void LoaddingOut(double s = 1.0)
        {

            var item = loadingFlow[mainFlow!];
            var loading = item.Item1;
            var mask = item.Item2;

            var animout = CreateTween(() => mask.alpha, (v) => { mask.alpha = v; mask.posChanged = true; }, 0, s);
            animout.end(() =>
            {
                if (loading != null)
                {
                    loading.text?.set_text("".AsHaxeString());
                    loading.bgMask.set_visible(false);
                    loading.posChanged = true;
                }
                if (mask != null)
                {
                    mask.set_visible(false);
                    mask.posChanged = true;
                }
                lockInter = false;
            });
        }

        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
        string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }

        public Tween CreateTween(HlFunc<double> getter, HlAction<double> setterAction, double targetValue, double duration)
        {
            var tweenType = new TType.TEaseOut();
            return tw.create_(getter, setterAction, null, targetValue, tweenType, (int)(duration * 1000), Ref<bool>.Null);
        }

        /// <summary>
        /// 调整html文本大小
        /// </summary>
        /// <param name="htmlText"></param>
        /// <param name="scale"></param>
        public void ApplyHTMLFont(HtmlText htmlText, double scale = 1)
        {
            double pixelScale = get_pixelScale.Invoke();
            var fontConf = Assets.Class.fontConf;


            BitmapFont bitmapFont;

            if (pixelScale <= 3.0)
            {
                bitmapFont = fontConf.font12;
            }
            else if (pixelScale <= 4.0)
            {
                bitmapFont = fontConf.font18;
            }
            else if (pixelScale <= 6.0)
            {
                bitmapFont = fontConf.font12;
            }
            else
            {
                bitmapFont = fontConf.font18;
            }

            Font font = bitmapFont.toFont();
            htmlText.set_font(font);
            htmlText.scaleX = htmlText.scaleY = scale;
            htmlText.posChanged = true;
        }
    }
    #endregion
}
