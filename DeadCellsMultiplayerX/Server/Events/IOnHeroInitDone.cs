using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Server.Events
{
    [Event]
    public interface IOnHeroInitDone
    {
        void OnHeroInitDone(Hero hero);
    }
}