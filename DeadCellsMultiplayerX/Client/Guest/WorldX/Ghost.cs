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

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    public abstract class Ghost : Entity
    {
        public string GUID { get; }

        public EntityInfo? PrevState { get; private set; }
        public EntityInfo? CurrentState { get; private set; }
        public bool IsFirstUpdate => PrevState == null;

        private string? lastColorMapModel;
        private string? lastColorMapSkin;
        private string lastGroup = "";

        private Tween? posTween;
        private double posFromX, posFromY;
        private double posToX, posToY;
        private double posCurX, posCurY;
        private double posProgress;
        private double smoothDurationMs = DefaultDurationMs;
        private const double SmoothingFactor = 0.3;   // 新区间的EMA权重
        private const int MinDurationMs = 20;
        private const int MaxDurationMs = 120;
        private const int DefaultDurationMs = 50;
        private const double TilePx = 24.0;

        /// <summary>
        /// 在跳过插值前的最大像素距离
        /// </summary>
        protected virtual double TeleportThresholdPx => 300.0;

        /// <summary>
        /// 通过 setPosCase 将插值后的像素位置写入实体的
        /// </summary>
        private void CommitPosition(double pixelX, double pixelY)
        {
            int tcx = (int)(pixelX / TilePx);
            int tcy = (int)(pixelY / TilePx);
            double txr = (pixelX - tcx * TilePx) / TilePx;
            double tyr = (pixelY - tcy * TilePx) / TilePx;
            setPosCase(tcx, tcy, txr, tyr);
        }

        /// <summary>
        /// 创建或重新定位单点补间。
        /// 该补间对归一化进度 0→1 进行插值；
        /// 该设置器根据 <see cref="posFromX/Y"/> 和 <see cref="posToX/Y"/>.
        /// </summary>
        private void EnsurePositionTween()
        {
            double speed = 1.0 / (smoothDurationMs * tw.baseFps / 1000.0);

            if (posTween != null && !posTween.done)
            {
                posTween.from = 0.0;
                posTween.to = 1.0;
                posTween.ln = 0.0;
                posTween.speed = speed;
            }
            else
            {
                posTween = tw.create_(
                    getter: () => posProgress,
                    setter: (val) =>
                    {
                        posProgress = val;
                        posCurX = posFromX + val * (posToX - posFromX);
                        posCurY = posFromY + val * (posToY - posFromY);
                        CommitPosition(posCurX, posCurY);
                    },
                    from: 0.0, to: 1.0,
                    tp: new TType.TLinear(),
                    duration_ms: smoothDurationMs,
                    allowDuplicates: Ref<bool>.In(true)
                );
            }
        }

        /// <summary>
        /// 根据最近两次接收到的快照之间的原始包间隔，更新平滑插值的持续时间。
        /// 使用指数移动平均法来抑制抖动。
        /// </summary>
        private void UpdateSmoothDuration()
        {
            long rawInterval = CurrentState!.TimeStamp - PrevState!.TimeStamp;
            smoothDurationMs = smoothDurationMs * (1.0 - SmoothingFactor)
                              + rawInterval * SmoothingFactor;
            smoothDurationMs = System.Math.Clamp(smoothDurationMs, MinDurationMs, MaxDurationMs);
        }

        /// <summary>
        /// 应用一个新的、由服务器确定的目标位置。
        /// 保持视觉连续性。
        /// </summary>
        private void ApplyNetworkTarget(PosVector pos, bool firstTime)
        {
            double targetX = pos.CX * TilePx + pos.XR * TilePx;
            double targetY = pos.CY * TilePx + pos.XY * TilePx;

            if (firstTime)
            {
                posFromX = posToX = posCurX = targetX;
                posFromY = posToY = posCurY = targetY;
                smoothDurationMs = DefaultDurationMs;
                CommitPosition(targetX, targetY);
                EnsurePositionTween();
                return;
            }

            // 识别是否是传送
            double threshold = TeleportThresholdPx;
            double dx = targetX - posCurX;
            double dy = targetY - posCurY;
            if (dx * dx + dy * dy > threshold * threshold)
            {
                posFromX = posToX = posCurX = targetX;
                posFromY = posToY = posCurY = targetY;
                smoothDurationMs = DefaultDurationMs;
                CommitPosition(targetX, targetY);
                EnsurePositionTween();
                return;
            }

            //根据数据包间隔计算动态持续时间
            UpdateSmoothDuration();

            //从当前渲染位置继续
            posFromX = posCurX;
            posFromY = posCurY;
            posToX = targetX;
            posToY = targetY;

            EnsurePositionTween();
        }


        protected abstract void OnApplyUpdate(EntityInfo incoming, bool firstTime);

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
            easeSpritePos = false;
            initClonesGfx();
            if (_level != null && _level.minimap != null && !_level.minimap.destroyed)
                minimapTracking();

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

                // foreach (AnimTransitions data in info.animInfo.AnimTransitions)
                // {
                //     dc.String? anim = null;
                //     dc.String? to = null;
                //     dc.String? from = null;

                //     if (data.From != string.Empty)
                //         from = data.From!.AsHaxeString();
                //     if (data.Anim != string.Empty)
                //         anim = data.Anim!.AsHaxeString();
                //     if (data.To != string.Empty)
                //         to = data.To!.AsHaxeString();

                //     spr.get_anim().registerTransition(from, to, anim, data.speed, data.reverse, null);
                // }

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
            }
        }

        public void ApplyUpdate(EntityInfo incoming)
        {
            bool firstTime = IsFirstUpdate;
            PrevState = CurrentState;
            CurrentState = incoming;

            if (spr == null) return;

            ApplyNetworkTarget(CurrentState.PosVector, firstTime);

            DisableGameplay();
            UpdateAnim(CurrentState);

            OnApplyUpdate(incoming, firstTime);
            SyncFacing(incoming);
        }

        private void SyncFacing(EntityInfo info)
        {
            if (info.EntityData.IntValues.TryGetValue("dir", out var fr))
                dir = fr;
        }

        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            if (spr == null || info == null || info.MainSprite == null || animinfo == null) return;
            var anim = spr.get_anim();
            var stack = anim.stack.getDyn(0) as AnimInstance;
            if (lastGroup != info.MainSprite.GroupName)
            {
                lastGroup = info.MainSprite.GroupName;
                anim.play(info.MainSprite.GroupName.AsHaxeString(), info.animInfo.Plays, null).loop(null);
            }

            if (stack != null)
            {
                stack.speed = info.animInfo.Speed;
                stack.paused = info.animInfo.Paused;
            }
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
