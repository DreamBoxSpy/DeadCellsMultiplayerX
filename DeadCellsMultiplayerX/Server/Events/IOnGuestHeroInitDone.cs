using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Server.Events
{

    [Event]
    public interface IOnGuestHeroInitDone
    {
        void OnHeroInitDone(Hero hero);
    }
}