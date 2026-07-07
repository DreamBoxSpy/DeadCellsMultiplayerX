using dc;
using dc.libs.heaps.slib;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest.Ghost;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest.World
{
    internal class EntityGhost : IDisposable, IWorldGhost
    {
        public Entity ghost;

        public string GUID => guid;

        /// <summary>
        /// 上一个状态
        /// </summary>
        private EntityInfo? prevInfo;

        /// <summary>
        /// 当前状态
        /// </summary>
        private EntityInfo? currentInfo;

        private readonly WorldDirector director;
        private readonly string guid;

        private bool setColorMapSuccess = false;

        public EntityGhost(WorldDirector director, Level lvl, string guid)
        {
            this.director = director;
            this.guid = guid;

            ghost = new(lvl, 0, 0);
            ghost.init();
        }

        public void Dispose()
        {
            ghost.dispose();
        }

        public void SetVisible(bool visible)
        {
            ghost.visible = visible;
        }

        private void UpdateAnim()
        {

            if(currentInfo == null ||
                ghost.spr == null)
            {
                return;
            }
            if(currentInfo.GlowData != null)
            {
                foreach((var idx, var gdd) in currentInfo.GlowData)
                {
                    var gd = new virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_();
                    //gdd.Deserialize(gd, null);
                    ghost.setGlowData(idx, gd, null);
                }
            }

            ghost.setDepth(ghost.curLayer);
        }

        private void UpdateColorMap()
        {
            if(currentInfo == null ||
                ghost.spr == null)
            {
                return;
            }

            if(prevInfo != null && 
                prevInfo.ColorMapModel == currentInfo.ColorMapModel &&
                prevInfo.ColorMapSkin == currentInfo.ColorMapSkin &&
                setColorMapSuccess)
            {
                return;
            }

            if(string.IsNullOrEmpty(currentInfo.ColorMapModel) ||
                string.IsNullOrEmpty(currentInfo.ColorMapSkin))
            {
                return;
            }

            ghost.setColorMap(currentInfo.ColorMapModel.AsHaxeString(), currentInfo.ColorMapSkin.AsHaxeString(), null);

            setColorMapSuccess = true;
        }

        public void UpdateInfo(EntityInfo info)
        {
            prevInfo = currentInfo;
            currentInfo = info;

            info.EntityData.Deserialize(ghost, typeof(Entity));

            ghost.setPosCase(ghost.cx, ghost.cy, ghost.xr, ghost.yr);
            ghost.set_targetable(false);
            ghost.circularRepel = 0;
            ghost.hasRepelling = false;
            ghost.detectsWater = false;

            UpdateAnim();
            UpdateColorMap();
        }
    }
}
