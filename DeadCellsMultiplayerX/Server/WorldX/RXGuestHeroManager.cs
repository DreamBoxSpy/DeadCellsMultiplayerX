using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCellsMultiplayerX.Server.Connection;
using DeadCellsMultiplayerX.Server.WorldX.RXGuestBeheaded;

namespace DeadCellsMultiplayerX.Server.WorldX
{
    internal class RXGuestHeroManager : IDisposable
    {
        private readonly SGuestConnection connection;

        private readonly List<ReceiveBeheaded> modules = new();

        public RXGuestHeroManager(SGuestConnection connection)
        {
            this.connection = connection;
        }

        public T Register<T>(T module)where T : ReceiveBeheaded
        {
            module.Initialize();
            modules.Add(module);
            return module;
        }

        public void Receive()
        {
            foreach (var module in modules)
                module.Receive();
        }

        public void Tick()
        {
            foreach (var module in modules)
                module.Tick();
        }

        public void Reset()
        {
            foreach (var module in modules)
                module.Receive();
        }

        public void Dispose()
        {
            foreach (var module in modules)
                module.Dispose();

            modules.Clear();
        }
    }
}