using dc.libs.heaps.slib;
using DeadCellsMultiplayerX.Client.Guest.Ghost;
using DeadCellsMultiplayerX.Common.Data;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest.World
{
    internal class SpriteGhost : IDisposable, IWorldGhost
    {
        public HSprite ghost;

        public WorldDirector director;

        public SpriteInfo? prevInfo;
        public SpriteInfo currentInfo;

        public string GUID { get; }

        public SpriteGhost(WorldDirector director, SpriteInfo info)
        {
            this.director = director;
            ghost = new(director.GetSpriteLib(info.AtlasName), info.GroupName?.AsHaxeString(), default, null);
            currentInfo = info;
            GUID = info.GUID;
        }

        private void UpdateParent()
        {
            if(prevInfo != null &&
                prevInfo.Parent == currentInfo.Parent &&
                ghost.parent != null)
            {
                return;
            }

            ghost.parent?.removeChild(ghost);

            if(currentInfo.Parent == null)
            {
                return;
            }

            var newParent = director.GetGhost<IWorldGhost>(currentInfo.Parent);

            if(newParent is SpriteGhost sg)
            {
                sg.ghost.addChild(ghost);
            }
            else if(newParent is EntityGhost eg)
            {
                if(eg.ghost.spr != ghost)
                {
                    Debug.Assert(eg.ghost.spr == null);

                    eg.ghost.initSprite(ghost.lib, ghost.groupName, null, null, null, null, null, null);

                    Debug.Assert(eg.ghost.spr != null);

                    var dst = eg.ghost.spr.children;
                    var src = ghost.children;
                    for (int i = 0; i < src.length; i++)
                    {
                        dst.push(src.getDyn(i));
                    }

                    ghost.remove();
                    ghost = eg.ghost.spr;
                }
            }
        }

        private void UpdateAnim()
        {
            var atlas = currentInfo.AtlasName;
            if (atlas == null)
            {
                return;
            }
            var lib = director.GetSpriteLib(atlas);
            var groupName = currentInfo.GroupName?.AsHaxeString();

            //currentInfo.PivotData.Deserialize(ghost.pivot, typeof(SpritePivot));

            if (lib != ghost.lib || prevInfo == null || prevInfo.GroupName != currentInfo.GroupName)
            {
                //ghost.set(lib, groupName, Ref<int>.In(currentInfo.Frame), default);
                ghost.get_anim().play(groupName, int.MaxValue, null);
            }

            var delt = (director.Session.CurrentTimeStamp - currentInfo.TimeStamp) / 1000f;

            var anim = ghost.get_anim();
            //anim.setFrame(currentInfo.Frame);

            if (delt < 0)
            {
                delt = 0;
                return;
            }

        }

        public void UpdateInfo(SpriteInfo info)
        {
            prevInfo = currentInfo;
            currentInfo = info;

            UpdateParent();
            UpdateAnim();
        }

        public void Dispose()
        {
                
        }

        public void SetVisible(bool visible)
        {
            ghost.visible = visible;
        }
    }
}
