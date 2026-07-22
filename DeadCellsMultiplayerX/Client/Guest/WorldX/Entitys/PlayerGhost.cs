using dc;
using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entities
{
    public class PlayerGhost : Ghost
    {

        public PlayerGhost(Level lvl, string guid) : base(lvl, guid) { }

        protected override void OnApplyUpdate(EntityInfo info)
        {
            if (info.TypeName != typeof(Hero).GetType().FullName) return;
        }
    }
}
