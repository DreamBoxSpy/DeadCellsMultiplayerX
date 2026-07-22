using dc;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entities
{
    public class MobGhost : Ghost
    {
        public MobGhost(Level lvl, string guid) : base(lvl, guid) { }

        protected override void OnApplyUpdate(EntityInfo info)
        {

        }
    }
}

