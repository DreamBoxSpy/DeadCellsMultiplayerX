using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCellsMultiplayerX.Client.Guest.WorldX.TXGuestBeheaded;
using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.GuestHero
{
    internal class TxBhMovement(GuestClientSession Session, TXGuestHeroManager manager) : TransmitBeheaded(Session, manager)
    {
        public override void Dispose()
        {
            
        }

        public override void Fill(HeroInfo info)
        {
            var e = Hero;
            info.PosVector = new PosVector(e.cx, e.cy, e.xr, e.yr, e.dir);
        }

        public override void Initialize()
        {
            
        }

        public override void Reset()
        {
            
        }

        public override bool ShouldSync()
        {
            return true;
        }

        public override void Tick()
        {
            
        }
    }
}