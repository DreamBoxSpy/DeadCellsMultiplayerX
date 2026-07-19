using dc.en;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server.Connection;
using Microsoft.VisualStudio.Threading;
using ModCore;
using ModCore.Events.Interfaces.Game;
using Nerdbank.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal class ServerSession : DisposableEventReceiver,
        IOnFrameUpdate
    {
        public AnonymousPipeClientStream outPipe = new(PipeDirection.Out, Environment.GetEnvironmentVariable("DCMP_HOST_IN_PIPE")!);
        public AnonymousPipeClientStream inPipe = new(PipeDirection.In, Environment.GetEnvironmentVariable("DCMP_HOST_OUT_PIPE")!);

        private MultiplexingStream? multiplexingStream;
        private MultiplexingStream.Channel? mainChannel;
        private Stream? mainChannelReader;
        private ServerMainThread? mainThread;

        private readonly List<SGuestConnection> guests = [];

        private readonly Stopwatch stopwatch = new();
        private long prevStopwatchTimeStamp = 0;
        
        public long CurrentTimeStamp { get; private set; }

        public ServerMainThread Main => mainThread ?? throw new InvalidOperationException();

        public async Task Init()
        {
            await Task.Yield().ConfigureAwait(false);

            mainThread = new(this);
            stopwatch.Start();
            outPipe.WriteByte(0x32);

            Logger.Information("Waiting host...");
            multiplexingStream = await MultiplexingStream.CreateAsync(
                FullDuplexStream.Splice(inPipe, outPipe),
                new MultiplexingStream.Options()
                {
                    ProtocolMajorVersion = 3
                }
                );

            await Task.Delay(10);

            Logger.Information("Binding main channel...");

            mainChannel = await multiplexingStream.AcceptChannelAsync("main");
            mainChannelReader = mainChannel.AsStream();

            Logger.Information("Waiting guests...");
            await WaitGuestConnect();
        }

        /// <summary>
        /// 等待 Guest 连接
        /// </summary>
        /// <returns></returns>
        private async Task WaitGuestConnect()
        {
            await Task.Yield().ConfigureAwait(false);
            Debug.Assert(mainChannelReader != null);
            Debug.Assert(multiplexingStream != null);

            byte[] numBuffer = new byte[4];
            while (true)
            {
                await Task.Delay(1);

                await mainChannelReader.ReadAtLeastAsync(numBuffer, 4, false);

                var channelId = BitConverter.ToInt32(numBuffer);

                if (channelId == -1)
                {
                    return; //加载完成
                }

                Logger.Information("Connecting from channel {id}", channelId);

                var channel = multiplexingStream.AcceptChannel(channelId);
                guests.Add(new SGuestConnection(this, channel.AsStream()));
            }
        }


        public async Task<List<HeroInfo>> GetGuestsHeroInfos()
        {
            List<HeroInfo> infos = [];
            foreach (var item in guests)
            {
                infos.Add(await item.guest.RequestHeroInfo());
            }

            return infos;
        }

        private void UpdateTimeStamp()
        {
            CurrentTimeStamp += stopwatch.ElapsedMilliseconds - prevStopwatchTimeStamp;
            prevStopwatchTimeStamp = stopwatch.ElapsedMilliseconds;
        }
        void IOnFrameUpdate.OnFrameUpdate(double dt)
        {
            UpdateTimeStamp();
        }
    }
}
