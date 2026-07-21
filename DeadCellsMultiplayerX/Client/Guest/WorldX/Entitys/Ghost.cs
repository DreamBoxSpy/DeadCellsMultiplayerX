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

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entities
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
            bool firstTime = IsFirstUpdate;
            PrevState = CurrentState;
            CurrentState = incoming;

            if (spr == null) return;

            

            ApplyNetworkTarget(CurrentState.PosVector, firstTime);
            DisableGameplay();
            UpdateAnim(CurrentState);
            OnApplyUpdate(incoming, firstTime);
        }

        private void ApplyNetworkTarget(PosVector pos, bool firstTime)
        {
            setPosCase(pos.CX, pos.CY, pos.XR, pos.XY);
            dir = pos.DIR;
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
