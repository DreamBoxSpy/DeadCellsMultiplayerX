using dc;
using dc.libs;
using dc.libs.heaps.slib;
using dc.libs.misc;
using dc.pr;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using DeadCellsMultiplayerX.Common.Data;
using dc.libs.heaps.slib._AnimManager;
using dc.h3d.shader;
using dc.hxd;
using dc.hxd.res;
using Serilog.Core;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    public abstract class Ghost : Entity
    {
        public string GUID { get; }

        public EntityInfo? PrevState { get; private set; } //上一个状态
        public EntityInfo? CurrentState { get; private set; } //当前状态

        public bool IsFirstUpdate => PrevState == null; //首次更新

        private string? lastColorMapModel;  //当前皮肤色带图
        private string? lastColorMapSkin;   //当前皮肤模型
        private string lastGroup = "";  //当前播放的hero动画

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
                initSprite(client.GetSpriteLib(info.MainSprite.AtlasName), info.MainSprite.GroupName.AsHaxeString(), null, null, null, null, null, null);
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
                        var gd = new virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_();
                        gdd.Deserialize(gd, null);
                        setGlowData(idx, gd, spr);
                        Log.Information($"{gd.ToString()}");
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

            CurrentState.EntityData.Deserialize(this, typeof(Entity));

            setPosCase(cx, cy, xr, yr);
            DisableGameplay();
            UpdateAnim(CurrentState);

            OnApplyUpdate(incoming, firstTime);
            SyncFacing(incoming);
        }


        /// <summary>
        /// 方向
        /// </summary>
        /// <param name="info"></param>
        private void SyncFacing(EntityInfo info)
        {
            if (info.EntityData.IntValues.TryGetValue("facingRight", out var fr) && fr != 0)
                dir = fr;
        }


        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            if (spr == null || info == null || info.MainSprite == null || animinfo == null) return;
            var anim = spr.get_anim();
            if (lastGroup != info.MainSprite.GroupName)
            {
                lastGroup = info.MainSprite.GroupName;
                var currloop = anim.play(info.MainSprite.GroupName.AsHaxeString(), animinfo.plays, null);
                var stack = currloop.stack.getDyn(0) as AnimInstance;
                if (stack != null)
                {
                    stack.speed = animinfo.speed;
                    stack.plays = animinfo.plays;
                }
            }
        }


        protected void DisableGameplay()
        {
            set_targetable(false);
            circularRepel = 0;
            hasRepelling = false;
            detectsWater = false;
        }
    }
}
