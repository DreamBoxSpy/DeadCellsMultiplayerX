using dc.hxd.res;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib.assets;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest.World;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    /// <summary>
    /// 控制客户端的表现
    /// </summary>
    /// <param name="session"></param>
    internal class WorldDirector(GuestClientSession session) : DisposableEventReceiver,
        IOnHeroUpdate
    {


        // 客户端 Viewport 的 Tile 大小
        public static readonly int VIEWPORT_WIDTH = 64; 
        public static readonly int VIEWPORT_HEIGHT = 64;

        public GuestClientSession Session => session;

        private readonly Dictionary<string, IWorldGhost> ghosts = [];

        private readonly Dictionary<string, SpriteLib> spriteLibLookup = [];

        protected override void MyDispose()
        {
            base.MyDispose();

            foreach(var v in ghosts.Values.ToArray())
            {
                v.Dispose();
            }
            ghosts.Clear();
        }

        public async Task Init()
        {
            Loop().Forget();
        }

        private async Task Loop()
        {
            SynchronizationContext.SetSynchronizationContext(ModCore.Modules.Game.SynchronizationContext);

            while(true)
            {
                await Task.Delay(1000 / 30);
                DisposeToken.ThrowIfCancellationRequested();

                await UpdateWorld();
            }
        }

        public SpriteLib GetSpriteLib(string atlasPath)
        {
            if(!spriteLibLookup.TryGetValue(atlasPath, out var spriteLib))
            {
                spriteLib = dc.libs.heaps.slib.assets.Atlas.Class.load(atlasPath.AsHaxeString(), null, null, null);
                spriteLibLookup.Add(atlasPath, spriteLib);
            }
            return spriteLib;
        }

        public void UpdateEntity(EntityInfo inf, Level? lvl)
        {
            lvl ??= (Level) Game.Class.ME.subLevels.getDyn(inf.SubLevelId);

            var ghost = GetGhost<EntityGhost>(inf.GUID);

            if (ghost == null)
            {
                ghost = new(this, lvl, inf.GUID);

                AddGhost(ghost);
            }

            ghost.SetVisible(true);
            ghost.UpdateInfo(inf);

            if(inf.MainSprite != null)
            {
                UpdateSprite(inf.MainSprite);
            }
        }

        public void UpdateSprite(SpriteInfo inf)
        {
            var ghost = GetGhost<SpriteGhost>(inf.GUID);
            if(ghost == null)
            {
                ghost = new(this, inf);
                AddGhost(ghost);
            }
            ghost.SetVisible(true);
            ghost.UpdateInfo(inf);

            foreach(var v in inf.Children)
            {
                UpdateSprite(v);
            }
        }

        public void AddGhost(IWorldGhost ghost)
        {
            ghosts.Add(ghost.GUID, ghost);
        }

        public T? GetGhost<T>(string guid) where T : class, IWorldGhost
        {
            Debug.Assert(Guid.TryParse(guid, out _));

            if(ghosts.TryGetValue(guid, out var ghost))
            {
                return (T?) ghost;
            }
            return null;
        }

        private async Task UpdateWorld()
        {
            var hero = session.Game?.hero;

            if(hero == null)
            {
                return;
            }

            var lvl = hero._level;

            if(lvl == null)
            {
                return;
            }

            var gm = Game.Class.ME;
            var map = lvl.map;
            var vp = hero._level.viewport;
            var tileX = vp.realX / 24;
            var tileY = vp.realY / 24;

            

            var rect = new RectInt()
            {
                X = (int)tileX - VIEWPORT_WIDTH / 2,
                Y = (int)tileY - VIEWPORT_HEIGHT / 2,
                Width = VIEWPORT_HEIGHT,
                Height = VIEWPORT_WIDTH,
            };
           

            if(rect.X < 0)
            {
                rect.X = 0;
            }
            if(rect.Y < 0)
            {
                rect.Y = 0;
            }
            if(rect.X + rect.Width >= map.wid)
            {
                rect.Width = map.wid - rect.X;
            }
            if(rect.Y + rect.Height >= map.hei)
            {
                rect.Height = map.hei - rect.Y;
            }

            var request = new IServerRPC.AreaInfoRequest()
            {
                SubLevelId = LevelUtils.GetSubLevelIndex(lvl, gm),
                Rect = rect
            };
            var result = await session.Server.RequestAreaInfo(request);

            //同步碰撞箱

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
            
            // 同步 Entity

            foreach(var v in ghosts.Values)
            {
               v.SetVisible(false); //隐藏不可见的 Entity (状态未知)
            }
            
            // 更新 Entity Ghost 状态
            foreach(var v in result.Entities)
            {
                UpdateEntity(v, lvl);
            }

        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            
        }
    }
}
