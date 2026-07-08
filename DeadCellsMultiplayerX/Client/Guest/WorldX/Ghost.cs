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
using dc.shader;
using System.Windows.Documents;
using System.Numerics;

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
        private const int TweenDurationMs = 50;

        private Tween? tweenX;
        private Tween? tweenY;
        private double tweenCurX, tweenCurY;
        private double targetX, targetY;

        private void ApplyTweenedPos()
        {
            const double TILE = 24.0;
            int tcx = (int)(tweenCurX / TILE);
            int tcy = (int)(tweenCurY / TILE);
            double txr = (tweenCurX - tcx * TILE) / TILE;
            double tyr = (tweenCurY - tcy * TILE) / TILE;
            setPosCase(tcx, tcy, txr, tyr);
        }

        private void EnsurePositionTweenX(double target)
        {
            double speed = 1.0 / (TweenDurationMs * tw.baseFps / 1000.0);

            if (tweenX != null && !tweenX.done)
            {
                tweenX.from = tweenCurX;
                tweenX.to = target;
                tweenX.ln = 0.0;
                tweenX.speed = speed;
            }
            else
            {
                tweenX = tw.create_(
                    getter: () => tweenCurX,
                    setter: (val) => { tweenCurX = val; ApplyTweenedPos(); },
                    from: tweenCurX, to: target,
                    tp: new TType.TLinear(), duration_ms: TweenDurationMs,
                    allowDuplicates: Ref<bool>.In(true)
                );
            }
        }

        private void EnsurePositionTweenY(double target)
        {
            double speed = 1.0 / (TweenDurationMs * tw.baseFps / 1000.0);

            if (tweenY != null && !tweenY.done)
            {
                tweenY.from = tweenCurY;
                tweenY.to = target;
                tweenY.ln = 0.0;
                tweenY.speed = speed;
            }
            else
            {
                tweenY = tw.create_(
                    getter: () => tweenCurY,
                    setter: (val) => { tweenCurY = val; ApplyTweenedPos(); },
                    from: tweenCurY, to: target,
                    tp: new TType.TLinear(), duration_ms: TweenDurationMs,
                    allowDuplicates: Ref<bool>.In(true)
                );
            }
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

                foreach (AnimTransitions data in info.animInfo.AnimTransitions)
                {
                    dc.String? anim = null;
                    dc.String? to = null;
                    dc.String? from = null;

                    if (data.From != string.Empty)
                        from = data.From!.AsHaxeString();
                    if (data.Anim != string.Empty)
                        anim = data.Anim!.AsHaxeString();
                    if (data.To != string.Empty)
                        to = data.To!.AsHaxeString();

                    spr.get_anim().registerTransition(from, to, anim, data.speed, data.reverse, null);
                }

                info.MainSprite.PivotData.Deserialize(spr.pivot, typeof(SpritePivot));

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

            var pos = CurrentState.PosVector;
            targetX = pos.X * 24.0 + pos.Z * 24.0;
            targetY = pos.Y * 24.0 + pos.W * 24.0;

            if (firstTime)
            {
                setPosCase(pos.X, pos.Y, pos.Z, pos.W);
                tweenCurX = targetX;
                tweenCurY = targetY;
            }

            EnsurePositionTweenX(targetX);
            EnsurePositionTweenY(targetY);

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
                anim.play(info.MainSprite.GroupName.AsHaxeString(), animinfo.plays, null).loop(null);
            }

            if (stack != null)
            {
                stack.speed = animinfo.speed;
                stack.paused = animinfo.paused;
            }
        }

        protected void DisableGameplay()
        {
            set_targetable(false);
            circularRepel = 0;
            hasRepelling = false;
            detectsWater = false;
            hasGravity = false;
            gravity = 0;
        }
    }
}
