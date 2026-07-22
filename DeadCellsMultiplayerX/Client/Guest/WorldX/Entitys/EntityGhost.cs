using dc;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using Hashlink.Virtuals;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entities
{
    public class EntityGhost : Ghost
    {
        public EntityGhost(Level lvl, string guid) : base(lvl, guid) { }

        protected override void OnApplyUpdate(EntityInfo info)
        {

        }
    }
}
