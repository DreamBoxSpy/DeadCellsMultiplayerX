using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Client.Event
{

    [Event]
    public interface IOnGuestHeroInitDone
    {
        void OnHeroInitDone(Hero hero);
    }
}