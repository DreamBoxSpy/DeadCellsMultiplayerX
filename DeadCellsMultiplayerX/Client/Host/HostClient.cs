using dc.sys.net;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using ModCore.Modules;
using ModCore.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Host
{
    internal class HostClient(BaseNetworkListener listener, CancellationToken cancellationToken) : ClientBase,
        IOnGuestQuit
    {
        private readonly List<GuestConnection> guests = [];

        private readonly CancellationTokenSource acceptGuestsCancelSource = new();

        public HostSession? session;

        /// <summary>
        /// 房间信息
        /// </summary>
        public LobbyInfo LobbyInfo { get; set; } = new();

        /// <summary>
        /// 游戏时基本信息
        /// </summary>
        public GameSessionInfo GameSessionInfo { get; set; } = new();

        /// <summary>
        /// 是否可以开始游戏
        /// </summary>
        public bool CanStartGame => !LobbyInfo.IsStarted && !LobbyInfo.CanConnectServer && LobbyInfo.Guests.Values.All(x => x.IsReady);

        /// <summary>
        /// 开始游戏
        /// 
        /// 在指定时间(StartTimeout)内没有做出回应的Guest Client将被踢出房间
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartGame()
        {
            if(!CanStartGame)
            {
                throw new InvalidOperationException();
            }

            await Task.Delay(1).ConfigureAwait(false);

            acceptGuestsCancelSource.Cancel();

            LobbyInfo.IsStarted = true;

            // 初始化服务器
            session = new HostSession();
            await session.Init();

            // 告诉 Guest 可以连接服务器

            Logger.Information("Waiting clients connect server...");
            foreach(var v in guests)
            {
                v.StartConnectServer();
            }
            LobbyInfo.CanConnectServer = true;

            while(true)
            {
                DisposeToken.ThrowIfCancellationRequested();
                await Task.Yield();
                bool finish = true;
                foreach(var v in guests)
                {
                    if(v.IsDisposed)
                    {
                        continue;
                    }
                    if(!v.IsConnectedServer)
                    {
                        finish = false;
                    }
                }
                if(finish)
                {
                    break;
                }
            }

            Logger.Information("Starting game...");
            session.StartGame();

            _ = Task.Delay(-1, session.DisposeToken)
                .ContinueWith(_ =>
                {
                    Dispose();
                }, TaskContinuationOptions.OnlyOnCanceled);
        }

        public async Task Init()
        {
            Logger.Information("Initializing host client...");

            _ = AcceptNewConnectTask();
        }

        private async Task AcceptNewConnectTask()
        {
            await Task.Yield().ConfigureAwait(false);
            while(!LobbyInfo.IsStarted)
            {
                await Task.Yield();
                DisposeToken.ThrowIfCancellationRequested();
                cancellationToken.ThrowIfCancellationRequested();

                Logger.Information("Waiting connection...");
                var connect = await listener.WaitConnect(cancellationToken.Combine(DisposeToken, acceptGuestsCancelSource.Token));

                Logger.Information("Connecting...");
                var gConnect = new GuestConnection(this, connect);
                guests.Add(gConnect);
                gConnect.Init();

                //第一个玩家为 Owner (通常为 房主)
                if(string.IsNullOrEmpty(LobbyInfo.Owner) ||
                    !LobbyInfo.Guests.ContainsKey(LobbyInfo.Owner))
                {
                    LobbyInfo.Owner = gConnect.guestInfo.Guid;
                    gConnect.guestInfo.IsHost = true;
                }
            }
        }

        protected override void MyDispose()
        {
            base.MyDispose();
            acceptGuestsCancelSource.Cancel();
            listener.Dispose();
            session?.Dispose();
            session = null;

            foreach (var v in guests.ToArray())
            {
                v.Dispose();
            }

            guests.Clear();
        }

        void IOnGuestQuit.OnGuestQuit(GuestInfo guest)
        {
            if(guest.IsHost)
            {
                // 房主退出
                Dispose();
            }
        }
    }
}
