using dc;
using dc.en;
using dc.libs.heaps.slib;
using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DeadCellsMultiplayerX.Server.Connection
{
    internal partial class SGuestConnection : IServerRPC
    {


        public Task<bool> CheckVersion(string version)
        {
            if (Version.Parse(version) != VersionUtils.ModVersion)
            {
                Logger.Error("Dismatch version: {ver}", guestInfo.Guid, version);
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                {
                    if (rpc == null)
                    {
                        return;
                    }
                    if (!rpc.IsDisposed)
                    {
                        Dispose();
                    }
                });
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public Task SetGuestInfo(GuestInfo info)
        {
            GuestInfo = info;
            return Task.CompletedTask;
        }

        public async Task UploadSavedata(byte[] data)
        {
            await Task.Delay(1).ConfigureAwait(false);

            var savePath = ServerMain.Instance.savePath;

            await File.WriteAllBytesAsync(savePath, data);

            Logger.Information("Saving savedata to {path}", savePath);

            Logger.Information("Building game...");

            await Main.LaunchGame();

            await Task.Delay(1).ConfigureAwait(false);
        }

        public async Task<byte[]> DownloadSavedata()
        {
            while (string.IsNullOrEmpty(Main.savePath))
            {
                await Task.Yield();
            }
            return File.ReadAllBytes(Main.savePath);
        }

        public async Task<AreaInfo> RequestAreaInfo(IServerRPC.AreaInfoRequest request)
        {
            var rect = request.Rect;
            if (rect.X < 0)
            {
                rect.X = 0;
            }
            if (rect.Y < 0)
            {
                rect.Y = 0;
            }
            var areaInfo = new AreaInfo()
            {
                Rect = rect
            };

            lastRequest = request;

            var lvl = (dc.pr.Level)dc.pr.Game.Class.ME.subLevels.getDyn(request.SubLevelId);
            var map = lvl.map;

            areaInfo.Collision = new int[rect.Width * rect.Height];

            // 碰撞箱
            unsafe
            {
                var src = new Span<int>((void*)map.collisions.bytes, map.collisions.length);
                for (int y = 0; y < rect.Height; y++)
                {
                    var dst = new Span<int>(areaInfo.Collision, y * rect.Width, rect.Width);
                    src.Slice((rect.Y + y) * map.wid + rect.X, rect.Width).CopyTo(dst);
                }
            }

            // Entity
            {
                var rx = rect.X;
                var ry = rect.Y;
                var rxt = rect.X + rect.Width;
                var ryt = rect.Y + rect.Height;

                foreach (Entity v in lvl.entities)
                {
                    if (v is Interactive)
                    {
                        continue;
                    }

                    if (v.cx >= rx && v.cx <= rxt && v.cy >= ry && v.cy <= ryt && v.visible)
                    {
                        EntityInfo inf = GetEntityInfo(v);

                        v.isOnScreen = true;

                        FillEntityInfo(v, inf);

                        areaInfo.Entities.Add(inf);
                    }
                }
            }

            foreach (var item in await Session.GetGuestsHeroInfos())
            {
                areaInfo.Entities.Add(item);
            }

            return areaInfo;
        }



        public Task<EntityInfo?> RequestEntityInfo(string guid)
        {
            return Task.FromResult(GetEntityInfo(guid));
        }

        public Task<long> GetTimeStamp()
        {
            return Task.FromResult(Session.CurrentTimeStamp);
        }


    }
}
