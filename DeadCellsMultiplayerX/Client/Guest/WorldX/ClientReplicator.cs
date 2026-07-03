using dc;
using dc.h2d;
using dc.libs.heaps.slib;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using Microsoft.VisualStudio.Threading;
using ModCore.Utilities;
using System;
using System.Collections.Generic;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal class ClientReplicator : DisposableEventReceiver, IDisposable
    {
        private readonly GuestClientSession session;
        private readonly Dictionary<string, Ghost> ghosts = [];
        private readonly Dictionary<string, SpriteLib> spriteLibs = [];
        private readonly Dictionary<string, HSprite> spriteCache = [];

        public ClientReplicator(GuestClientSession session)
        {
            this.session = session;
        }

        public void Start()
        {
            PollLoop().Forget();
        }

        protected override void MyDispose()
        {
            foreach (var g in ghosts.Values)
                g.destroy();
            ghosts.Clear();
            spriteLibs.Clear();
            spriteCache.Clear();
        }


        public SpriteLib GetSpriteLib(string atlasPath)
        {
            if (!spriteLibs.TryGetValue(atlasPath, out var lib))
            {
                lib = Assets.Class.lib.get(atlasPath.AsHaxeString());
                spriteLibs.Add(atlasPath, lib);
            }
            return lib;
        }


        private async Task PollLoop()
        {
            SynchronizationContext.SetSynchronizationContext(
                ModCore.Modules.Game.SynchronizationContext);

            while (true)
            {
                await Task.Delay(1000 / 30);
                // TODO: 接入 DisposeToken
                session.DisposeToken.ThrowIfCancellationRequested();

                await PollOnce();
            }
        }

        private async Task PollOnce()
        {
            var hero = session.Game?.hero;
            if (hero?.spr == null) return;
            var lvl = hero._level;
            if (lvl == null) return;

            //计算视口矩形
            var vp = lvl.viewport;
            var tileX = (int)(vp.realX / 24);
            var tileY = (int)(vp.realY / 24);

            var rect = new RectInt
            {
                X = tileX - 32,   // 64/2
                Y = tileY - 32,
                Width = 64,
                Height = 64,
            };

            var map = lvl.map;
            if (rect.X < 0) rect.X = 0;
            if (rect.Y < 0) rect.Y = 0;
            if (rect.X + rect.Width >= map.wid) rect.Width = map.wid - rect.X;
            if (rect.Y + rect.Height >= map.hei) rect.Height = map.hei - rect.Y;

            //请求服务端
            var gm = dc.pr.Game.Class.ME;
            var request = new IServerRPC.AreaInfoRequest
            {
                SubLevelId = LevelUtils.GetSubLevelIndex(lvl, gm),
                Rect = rect
            };
            var result = await session.Server.RequestAreaInfo(request);

            //应用碰撞
            var rrect = result.Rect;
            if (result.Collision != null)
            {
                unsafe
                {
                    var dst = new Span<int>((void*)map.collisions.bytes, map.collisions.length);
                    for (int y = 0; y < rrect.Height; y++)
                    {
                        var src = new Span<int>(result.Collision, y * rrect.Width, rrect.Width);
                        src.CopyTo(dst.Slice((rrect.Y + y) * map.wid + rrect.X, map.wid));
                    }
                }
            }

            //应用实体
            ApplyAreaInfo(result.Entities, lvl);
        }


        public void ApplyAreaInfo(List<EntityInfo> entities, Level lvl)
        {
            foreach (var g in ghosts.Values)
                g.visible = false;

            foreach (var info in entities)
                ApplyEntityInfo(info, lvl);
        }

        public void ApplyEntityInfo(EntityInfo info, Level? lvl = null)
        {
            lvl ??= (Level)dc.pr.Game.Class.ME.subLevels.getDyn(info.SubLevelId);

            if (!ghosts.TryGetValue(info.GUID, out var ghost))
            {
                ghost = new EntityGhost(lvl, info.GUID);
                ghost.init(info, this);
                ghosts.Add(info.GUID, ghost);
            }

            ghost.visible = true;
            ghost.ApplyUpdate(info);
        }

        public T? GetGhost<T>(string guid) where T : Ghost
        {
            if (ghosts.TryGetValue(guid, out var g))
                return g as T;
            return null;
        }
    }
}
