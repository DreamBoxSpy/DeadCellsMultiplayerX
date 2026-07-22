using dc;
using dc.libs;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib._AnimManager;
using dc.libs.misc;
using dc.pr;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using Serilog.Core;
using Serilog;
using Mirror;
using System.Timers;
using dc.hxd;
using DeadCellsMultiplayerX.Common;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entities
{
    public abstract class Ghost : Entity
    {
        public string GUID { get; }

        private string? lastColorMapModel;
        private string? lastColorMapSkin;
        private string lastGroup = "";

        private dc.libs.Process? interpolationProcess; //挂载位置渲染程序


        private double visualX, visualY;   // 实际渲染位置
        private bool visualInit;//是否首次渲染


        /// <summary>
        /// Mirror参数
        /// </summary>
        private SortedList<double, GhostSnapshot> snapshotBuffer = new();// 快照缓冲区，按远程时间排序，用于插值
        private const float sendInterval = 1f / 60f; // 服务器快照发送间隔（60Hz）
        private double bufferTime = 0.1; // 本地时间线落后服务器的目标缓冲时间（秒）
        private int bufferLimit = 64; // 缓冲区最大快照数量，防止内存无限增长
        private const double catchupSpeed = 0; // 追赶加速比例
        private const double slowdownSpeed = 0; // 减速比例
        private const float catchupNegativeThreshold = -0.5f;  // 触发减速的漂移负阈值（sendInterval的倍数，当前无效）
        private const float catchupPositiveThreshold = 2f;  // 触发加速的漂移正阈值（sendInterval的倍数，当前无效）
        private double localTimeline; // 本地插值时间线，始终落后服务器 latestRemoteTime - bufferTime
        private double localTimescale; // 时间缩放因子，用于追赶/减速，当前恒为1.0
        private ExponentialMovingAverage driftEma = new(10); // 漂移量（latestRemoteTime - localTimeline）的指数移动平均
        private ExponentialMovingAverage deliveryTimeEma = new(10); // 快照交付间隔的指数移动平均，备用动态缓冲调整


        private const double TilePx = 24.0;

        protected abstract void OnApplyUpdate(EntityInfo incoming);

        protected Ghost(Level lvl, string guid) : base(lvl, 0, 0)
        {
            GUID = guid;
        }

        internal void init(EntityInfo info, ClientReplicator client)
        {
            const double fps = 60.0;
            delayer = new Delayer(fps);
            tw = new Tweenie(fps);
            createAttackSource();
            createAttackTarget();
            initGfx(info, client);
            DisableGameplay();
            //easeSpritePos = false;
            initClonesGfx();
            if (_level != null && _level.minimap != null && !_level.minimap.destroyed)
                minimapTracking();

            interpolationProcess = new dc.libs.Process(_level);
            interpolationProcess.onUpdateCb = new HlAction(OnInterpolationUpdate);

            DisableGameplay();

            initDone = true;
            isOnScreen = false;
            isOutOfGame = true;
            if (!isInQuadTree()) return;
            _level?.qTree.tryInsert(cx, cy, this);
        }

        internal void initGfx(EntityInfo info, ClientReplicator client)
        {
            base.initGfx();

            if (info != null && info.MainSprite != null)
            {
                var sprlib = client.GetSpriteLib(info.MainSprite.AtlasName);
                var group = info.MainSprite.GroupName.AsHaxeString();
                dc.h3d.mat.Texture normalMapFromGroup = sprlib.getNormalMapFromGroup(group);
                initSprite(sprlib, group, null, null, null, true, null, normalMapFromGroup);


                spr.pivot.copyFrom(DCMXSerializers.MessagePack.Deserialize<SpritePivot>(info.MainSprite.PivotData));

                lastColorMapModel = info.ColorMapModel;
                lastColorMapSkin = info.ColorMapSkin;
                setColorMap(lastColorMapModel?.AsHaxeString(),
                 lastColorMapSkin?.AsHaxeString(), null);

                if (info.GlowData != null)
                {
                    foreach ((var idx, var gdd) in info.GlowData)
                    {
                        if (gdd == null) continue;
                        setGlowData(idx, DCMXSerializers.MessagePack.Deserialize<virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_>(gdd), spr);
                    }
                }


                if (info.animInfo.AnimTransitions != null)
                {
                    foreach (AnimTransitions transition in info.animInfo.AnimTransitions)
                    {
                        dc.String? from = transition.From?.AsHaxeString();
                        dc.String? to = transition.To?.AsHaxeString();
                        dc.String? a = transition.Anim?.AsHaxeString();

                        spr.get_anim().registerTransition(
                            from,
                            to,
                            a,
                            transition.speed,
                            transition.reverse,
                            null
                        );
                    }
                }
            }
        }

        public void ApplyUpdate(EntityInfo incoming)
        {
            if (spr == null) return;

            double remoteSeconds = incoming.remoteTime / 1000.0;
            double localSeconds = GuestClientSession.SyncedTimeMs / 1000.0;

            var snap = new GhostSnapshot
            {
                State = incoming,
                remoteTime = remoteSeconds,
                localTime = localSeconds
            };

            SnapshotInterpolation.InsertAndAdjust(
                snapshotBuffer,
                bufferLimit,
                snap,
                ref localTimeline,
                ref localTimescale,
                sendInterval,
                bufferTime,
                catchupSpeed,
                slowdownSpeed,
                ref driftEma,
                catchupNegativeThreshold,
                catchupPositiveThreshold,
                ref deliveryTimeEma
            );
        }

        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            var anim = spr.get_anim();

            if (spr == null || info == null || info.MainSprite == null || animinfo == null || anim == null) return;

            var stack = anim.stack.getDyn(0) as AnimInstance;
            if (lastGroup != info.MainSprite.GroupName)
            {
                lastGroup = info.MainSprite.GroupName;
                var cur = anim.stack?.getDyn(0) as AnimInstance;
                if (cur != null) cur.plays = 0;
                anim.play(info.MainSprite.GroupName.AsHaxeString(), info.animInfo.Plays, null).loop(null);
            }

            if (stack != null)
            {
                stack.speed = info.animInfo.Speed;
                stack.paused = info.animInfo.Paused;
                stack.playDuration = info.animInfo.playDuration;
            }
        }

        void OnInterpolationUpdate()
        {
            if (snapshotBuffer.Count == 0) return;
            float deltaTime = (float)dc.hxd.Timer.Class.dt;

            // 使用 Mirror 快照插值系统，获取当前时间线对应的 from/to 快照和插值因子 t
            SnapshotInterpolation.Step(
                snapshotBuffer, deltaTime,
                ref localTimeline, localTimescale,
                out GhostSnapshot from, out GhostSnapshot to, out double t);

            // 将格子坐标和归一化偏移转换为全局像素坐标，避免跨格子插值失真
            double fromPx = from.State.PosVector.CX * TilePx + from.State.PosVector.XR * TilePx;
            double fromPy = from.State.PosVector.CY * TilePx + from.State.PosVector.XY * TilePx;
            double toPx = to.State.PosVector.CX * TilePx + to.State.PosVector.XR * TilePx;
            double toPy = to.State.PosVector.CY * TilePx + to.State.PosVector.XY * TilePx;

            // 在像素空间线性插值，得到当前帧的目标位置
            double targetX = fromPx + (toPx - fromPx) * t;
            double targetY = fromPy + (toPy - fromPy) * t;

            dir = to.State.PosVector.DIR;

            //传送检测
            if (!visualInit ||
                System.Math.Sqrt((targetX - visualX) * (targetX - visualX) + (targetY - visualY) * (targetY - visualY)) > 600)
            {
                visualX = targetX;
                visualY = targetY;
                visualInit = true;
            }
            else
            {
                double tdx = targetX - visualX;
                double tdy = targetY - visualY;
                double distance = System.Math.Sqrt(tdx * tdx + tdy * tdy);

                // 微小抖动直接吸附，消除静止状态下的浮点/网络波动
                if (distance < 0.3)
                {
                    visualX = targetX;
                    visualY = targetY;
                }
                else
                {
                    // 指数移动平均（EMA）平滑，无追赶，保证视觉匀速且无过冲
                    const double smoothFactor = 55.0;
                    double talpha = 1.0 - System.Math.Exp(-smoothFactor * deltaTime);
                    visualX += tdx * talpha;
                    visualY += tdy * talpha;
                }
            }

            
            setPosPixel(visualX, visualY);
            
            //同时更新位置避免位置与动画不同步
            UpdateAnim(from.State);
        }


        protected void DisableGameplay()
        {
            //set_targetable(false);
            circularRepel = 0;
            hasRepelling = false;
            detectsWater = false;
            hasGravity = false;
            gravity = 0;
        }
    }
}
