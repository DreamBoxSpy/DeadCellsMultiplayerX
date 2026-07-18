using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib._AnimManager;
using DeadCellsMultiplayerX.Client.Guest.WorldX.TXGuestBeheaded;
using DeadCellsMultiplayerX.Common.Data;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.GuestHero
{
    internal class TxBhAnimation(GuestClientSession session, TXGuestHeroManager manager) : TransmitBeheaded(session, manager)
    {
        private AnimManager anim { get; set; } = null!;
        private AnimInfo current = new();
        private AnimInfo previous = new();
        private AnimInfo temp = null!;

        public override void Initialize()
        {
            anim = Hero.spr.get_anim();
        }

        public override void Tick()
        {
           
        }

        public override bool ShouldSync()
        {
            return true;
        }

        public override HeroInfo Fill(HeroInfo info)
        {
            temp = previous;
            previous = current;
            current = temp;

            CaptureCurrentState();


            info.animInfo = current.Clone();
            return info;
        }

        public override void Reset()
        {
            anim = Hero.spr.get_anim();

            current.GroupName = "idle";
            current.Speed = 1.0;
            current.Paused = false;
            current.Frame = 0;
            current.Plays = 99999;
            current.playDuration = 0;
            current.AnimTransitions?.Clear();

            previous.CopyFrom(current);
        }

        public override void Dispose()
        {
            anim = null!;
            current = null!;
            previous = null!;
        }

        private void CaptureCurrentState()
        {
            var spr = Hero.spr;
            if (anim.stack.length > 0)
            {
                var inst = anim.stack.getDyn(0) as AnimInstance;
                if (inst != null)
                {
                    current.GroupName = spr.groupName.ToString();
                    current.Speed = inst.speed;
                    current.Paused = inst.paused;
                    current.Frame = spr.frame;
                    current.Plays = inst.plays;
                    current.playDuration = inst.playDuration;
                    return;
                }
            }


            Reset();
        }
    }
}