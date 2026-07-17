using dc;
using dc.en;
using dc.en.inter;
using dc.pr;
using dc.tool;
using DeadCellsMultiplayerX.Client.Guest.WorldX;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Proxy.Clousre;
using Microsoft.VisualStudio.Threading;
using ModCore;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Modules;
using ModCore.Utilities;
using PolyType.Abstractions;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    /// <summary>
    /// 访客的客户端 session
    /// </summary>
    internal class GuestClientSession(GuestClient client, Stream serverStream) : ClientSession,
        IGuestRPC,
        IOnFrameUpdate
    {
        private JsonRpc? rpc;
        private IServerRPC server = null!;
        private bool isOwner = false;
        private byte[]? saveData;
        private ClientReplicator? replicator;
        private Task? syncTimeStampTask;
        private readonly List<HashlinkHooks.HookHandle> hooks = [];

        private long lastSyncStopwatchTime = 0;
        private long prevStopwatchTime = 0;
        private readonly Stopwatch stopwatch = new();

        /// <summary>
        /// 当前服务器时间 (ms)
        /// </summary>
        public long CurrentTimeStamp { get; private set; }

        /// <summary>
        /// 供 Ghost.postUpdate() 等渲染代码读取的同步服务器时间。
        /// 由 <see cref="IOnFrameUpdate.OnFrameUpdate"/> 在每帧开始时写入。
        /// 在主线程上运行，无需同步。
        /// </summary>
        public static long SyncedTimeMs { get; private set; }

        public IServerRPC Server => server ?? throw new InvalidOperationException();

        public dc.pr.Game Game => dc.pr.Game.Class.ME;
        public GuestClient Client { get; private set; } = null!;

        public override async Task Init()
        {
            InitHooks();

            stopwatch.Start();

            rpc = serverStream.CreateJsonRpc();
            rpc.AddLocalRpcTarget(this);

            server = rpc.Attach<IServerRPC>();

            rpc.Disconnected += Rpc_Disconnected;
            rpc.StartListening();

            if (!await server.CheckVersion(
               VersionUtils.ModVersion.ToString()
               ))
            {
                Logger.Information("Failed to connect lobby. Dismatch version.");
                Dispose();
                return;
            }

            Debug.Assert(client.LobbyInfo != null);

            Client = client;

            if (client.LobbyInfo.Owner == client.Guid)
            {
                isOwner = true;
            }

            Logger.Information("Updating guest info...");

            await server.SetGuestInfo(client.LobbyInfo.Guests[client.Guid]);

            if (isOwner)
            {
                //上传存档到服务器
                Logger.Information("Updating savedata...");
                var fp = System.IO.Path.GetFullPath("save/" + Save.Class.fileName(null).ToString());

                //必须使用现有存档进行联机
                Debug.Assert(System.IO.File.Exists(fp));

                await server.UploadSavedata(await System.IO.File.ReadAllBytesAsync(fp));
            }

        }

        private void InitHooks()
        {
            Hook__Save.save += Hook__Save_save;
            Hook__File.getBytes += Hook__File_getBytes;

            if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
            {
                HashlinkHooks.Instance.CreateHook("tool.$File", "getSteamCloudStatus", Hook__File_getSteamCloudStatus);
                HashlinkHooks.Instance.CreateHook("tool.$File", "saveSteamCloudStatus", Hook__File_saveSteamCloudStatus);
            }

            Hook_Game.onDispose += Hook_Game_onDispose;
        }

        private void Hook_Game_onDispose(Hook_Game.orig_onDispose orig, dc.pr.Game self)
        {
            orig(self);

            Logger.Information("Quiting...");

            client.Quit();
            Dispose();
        }

        private void Hook__File_saveSteamCloudStatus(HashlinkClosure orig)
        {

        }

        private bool? Hook__File_getSteamCloudStatus(HashlinkClosure orig)
        {
            return null;
        }

        private dc.haxe.io.Bytes Hook__File_getBytes(Hook__File.orig_getBytes orig, dc.String file)
        {
            var fn = file.ToString();
            if (fn.StartsWith("user"))
            {
                Debug.Assert(saveData != null);

                var bytes = dc.haxe.io.Bytes.Class.alloc(saveData.Length);
                saveData.CopyTo(bytes.AsSpan());
                return bytes;
            }
            return orig(file);
        }

        private void Hook__Save_save(Hook__Save.orig_save orig, dc.User u, bool onlyGameData)
        {

        }

        private void Rpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            if (e.Reason == DisconnectedReason.LocallyDisposed)
            {
                return;
            }
            Logger.Error(e.Exception, "Abort connection: {reason}: {desc}", e.Reason, e.Description);

            ModCore.Modules.Game.SynchronizationContext.Post(static _ =>
            {
                ClientMain.Instance.CleanupClient();

                Boot.Class.ME.returnToMainMenu();
            }, null);

            Dispose();
        }

        protected override void MyDispose()
        {
            base.MyDispose();

            serverStream?.Dispose();
            replicator?.Dispose();
            rpc?.Dispose();

            Hook__Save.save -= Hook__Save_save;
            Hook__File.getBytes -= Hook__File_getBytes;

            foreach (var v in hooks)
            {
                v.Disable();
            }
            hooks.Clear();
        }

        /// <summary>
        /// 载入新 level 并初始化
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public async Task EnterNewLevel(byte[] saveData)
        {
            this.saveData = saveData;

            Logger.Information("Entering new level...");

            Main.Class.ME.options.disableLoreRooms = true;

            Main.Class.ME.cleanUser();
            Main.Class.ME.launchGame(new LaunchMode.LoadSave(), null, null);

            while (true)
            {
                await Task.Delay(1);
                DisposeToken.ThrowIfCancellationRequested();

                var g = dc.pr.Game.Class.ME;
                if (g?.curLevel == null || g.subLevels == null)
                {
                    continue;
                }
                if (!Main.Class.ME.isLoading)
                {
                    break;
                }
            }

            Logger.Information("Clearing entities...");

            var gm = dc.pr.Game.Class.ME;

            foreach (Level level in gm.subLevels)
            {
                List<Entity> entities = [];
                foreach (Entity v in level.entities)
                {
                    if (v is Hero || v is Interactive)
                    {
                        continue;
                    }
                    entities.Add(v);
                }
                foreach (var v in entities)
                {
                    v.destroy();
                }
            }

            replicator?.Dispose();
            replicator = new(this);
            replicator.Start();
        }

        private void UpdateTimeStamp()
        {
            if (prevStopwatchTime == 0)
            {
                prevStopwatchTime = stopwatch.ElapsedMilliseconds;
                return;
            }

            if (rpc?.IsDisposed ?? true)
            {
                return;
            }

            CurrentTimeStamp += stopwatch.ElapsedMilliseconds - prevStopwatchTime;
            prevStopwatchTime = stopwatch.ElapsedMilliseconds;

            if (stopwatch.ElapsedMilliseconds - lastSyncStopwatchTime > 5 * 1000 ||
                lastSyncStopwatchTime == 0)
            {
                if (syncTimeStampTask?.IsCompleted ?? false)
                {
                    return;
                }
                long startTime;
                //与服务器同步
                async Task SyncWithServer()
                {
                    try
                    {
                        startTime = stopwatch.ElapsedMilliseconds;
                        var time = await Server.GetTimeStamp();
                        CurrentTimeStamp = time + (stopwatch.ElapsedMilliseconds - startTime) / 2;
                    }
                    catch (Exception) when (IsDisposed || (rpc?.IsDisposed ?? true))
                    { }
                }
                syncTimeStampTask = SyncWithServer();
            }
        }

        void IOnFrameUpdate.OnFrameUpdate(double dt)
        {

            // 同步 TimeStamp
            UpdateTimeStamp();
            SyncedTimeMs = CurrentTimeStamp;
        }

        public void UpdateEntity(EntityInfo info)
        {
            replicator?.ApplyEntityInfo(info, null);
        }
    }
}
