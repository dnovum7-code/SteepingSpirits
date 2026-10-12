using SteepingSpirits.World;

namespace SteepingSpirits.Plantation
{
    /// <summary>
    /// IDayClock on top of the game's GameClock: "today" is GameClock.TotalDays, ending the day
    /// jumps the clock to the next morning (like the bed on the meadow).
    /// </summary>
    public sealed class GameClockDays : IDayClock
    {
        private readonly GameClock clock;

        public GameClockDays(GameClock clock)
        {
            this.clock = clock;
        }

        public int Today => clock != null ? clock.TotalDays : 1;

        public void EndDay()
        {
            if (clock != null) clock.AdvanceToNextDay();
        }
    }
}
