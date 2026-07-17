using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Server.Events;
using ModCore.Events.Interfaces.Game.Hero;

namespace DeadCellsMultiplayerX.Server.Connection
{
    internal partial class SGuestConnection :
    IOnHeroInitDone,
    IOnHeroUpdate
    {
        void IOnHeroInitDone.OnHeroInitDone(Hero hero)
        {

        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {

        }
    }
}