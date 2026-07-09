using dc;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib._AnimManager;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using MessagePack.Formatters;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DeadCellsMultiplayerX.Server.Connection
{
    internal partial class SGuestConnection
    {

        private readonly Dictionary<nint, EntityInfo> entitiesInfo = [];
        private readonly Dictionary<string, EntityInfo> guid2entityInfoLookup = [];
        private readonly Dictionary<nint, SpriteInfo> spritesInfo = [];

        private SpriteInfo GetSpriteInfo(HSprite spr)
        {
            if (!spritesInfo.TryGetValue(spr.HashlinkPointer, out var result))
            {
                result = new();
                spritesInfo.Add(spr.HashlinkPointer, result);
            }
            return result;
        }
        private EntityInfo GetEntityInfo(Entity e)
        {
            if (!entitiesInfo.TryGetValue(e.HashlinkPointer, out var result))
            {
                result = new();
                entitiesInfo.Add(e.HashlinkPointer, result);
                guid2entityInfoLookup[result.GUID] = result;
            }
            return result;
        }

        private EntityInfo? GetEntityInfo(string guid)
        {
            if (guid2entityInfoLookup.TryGetValue(guid, out var result))
            {
                return result;
            }
            return null;
        }

        private void FillSpriteInfo(HSprite spr, string? parent, SpriteInfo inf)
        {
            if (ServerMain.Instance.spriteLib2altas.TryGetValue(spr.lib, out var atlasPath))
            {
                inf.AtlasName = atlasPath;
                inf.GroupName = spr.groupName.ToString();
            }

            inf.PivotData.Serialize(spr?.pivot, typeof(SpritePivot));
            inf.Parent = parent;

            var children = spr?.children;

            inf.Children.Clear();
            for (int i = 0; i < children?.length; i++)
            {
                var child = children.getDyn(i) as HSprite;
                if (child == null)
                {
                    continue;
                }

                var sinfo = GetSpriteInfo(child);
                inf.Children.Add(sinfo);
                FillSpriteInfo(child, inf.GUID, sinfo);
            }
        }

        public void FillEntityAnimInfo(EntityInfo inf, HSprite spr)
        {
            var anim = spr.get_anim();
            if (spr != null && anim != null && !anim.destroyed && anim.stack.length > 0)
            {
                var current = anim.stack.getDyn(0) as AnimInstance;
                var transitions = anim.transitions;
                if (current != null)
                {
                    AnimInfo info = new AnimInfo
                    {
                        Speed = current.speed,
                        Paused = current.paused,
                        Frame = spr.frame,
                        Plays = current.plays
                    };

                    if (inf.animInfo.AnimTransitions.Count == 0 && transitions.length > 0)
                    {
                        List<AnimTransitions> anims = [];
                        foreach (Transition data in transitions)
                        {
                            var tr = new AnimTransitions();
                            tr.Anim = data.anim.ToString();
                            tr.From = data.from.ToString();
                            tr.To = data.to.ToString();
                            tr.reverse = data.reverse;
                            tr.speed = data.spd;

                            anims.Add(tr);
                        }
                        inf.animInfo.AnimTransitions = anims;
                    }
                    inf.animInfo = info;
                }
            }
        }

        public void FillEntityGlowkeyData(Entity e, EntityInfo info)
        {
            var glow = (dc.shader.GlowKey)e.spr.getShader(dc.shader.GlowKey.Class);
            if (info.GlowData.Count == 0 && glow != null)
            {
                var array = glow.getGlowDatas();
                for (int i = 0; i < array.length; i++)
                {
                    var data = array.getDyn(i);
                    var virtuals = ((HaxeProxyBase)data).ToVirtual<virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_>();
                    info.GlowData.Add(i, DCMXSerializers.MessagePack.Serialize(virtuals));
                }
            }
        }

        private void FillEntityInfo(Entity e, EntityInfo inf)
        {
            inf.TypeName = e.GetType().FullName;

            inf.SubLevelId = e._level.GetSubLevelIndex();
            inf.EntityData.Serialize(e, typeof(Entity));

            var pos = new PosVector(e.cx, e.cy, e.xr, e.yr, e.dir);
            inf.PosVector = pos;

            inf.TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (e.spr != null)
            {
                var sinfo = GetSpriteInfo(e.spr);
                inf.MainSprite = sinfo;
                FillSpriteInfo(e.spr, inf.GUID, sinfo);
                FillEntityAnimInfo(inf, e.spr);
                FillEntityGlowkeyData(e, inf);
            }
        }

        private bool TryGetInfoIfVisable(Entity e, [NotNullWhen(true)] out EntityInfo? info)
        {
            if (lastRequest == null)
            {
                info = null;
                return false;
            }
            var rect = lastRequest.Rect;
            var rx = rect.X;
            var ry = rect.Y;
            var rxt = rect.X + rect.Width;
            var ryt = rect.Y + rect.Height;
            if (e.cx >= rx && e.cx <= rxt && e.cy >= ry && e.cy <= ryt && e.visible)
            {
                EntityInfo inf = GetEntityInfo(e);

                e.isOnScreen = true;

                inf.TypeName = e.GetType().FullName;

                info = inf;
                return true;
            }
            info = null;
            return false;
        }

        private bool TryUpdateEntity(Entity e)
        {
            if (TryGetInfoIfVisable(e, out var inf))
            {
                FillEntityInfo(e, inf);
                guest.UpdateEntity(inf);
                return true;
            }
            return false;
        }
    }
}
