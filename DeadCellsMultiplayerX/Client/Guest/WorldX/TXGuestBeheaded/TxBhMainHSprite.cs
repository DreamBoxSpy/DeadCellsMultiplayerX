using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Server;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.TXGuestBeheaded
{
    internal class TxBhMainHSprite(GuestClientSession Session, TXGuestHeroManager manager) : TransmitBeheaded(Session, manager)
    {
        public override void Fill(HeroInfo info)
        {
            var spr = Hero.spr;

            var getSkinInfo = Hero.getSkinInfo();

            info.ColorMapModel = getSkinInfo.model.ToString();
            info.ColorMapSkin = getSkinInfo.colorMap.ToString();

            var atlaspath = "atlas/" + info.ColorMapModel + ".atlas";

            info.MainSprite = new SpriteInfo();
            info.MainSprite.AtlasName = atlaspath;
            info.MainSprite.GroupName = spr.groupName.ToString();
            info.MainSprite.PivotData = DCMXSerializers.MessagePack.Serialize(spr?.pivot);
            info.MainSprite.Parent = info.GUID;


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

        public override void Dispose()
        {

        }
    }
}