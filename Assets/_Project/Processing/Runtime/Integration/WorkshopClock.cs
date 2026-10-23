using SteepingSpirits.Plantation;
using SteepingSpirits.World;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// The workshop's game time: whole days from the garden's IDayClock (GameClockDays) plus the
    /// minute of the day from the GameClock, with a debug fast-forward offset on top.
    /// A later tea-house day system only needs to replace <see cref="Create"/>.
    /// </summary>
    public static class WorkshopClock
    {
        public static SkippableTime Create(GameClock clock)
        {
            IDayClock days = new GameClockDays(clock);
            var time = new DayAndMinuteTime(() => days.Today, () => clock != null ? clock.TimeOfDayHours * 60.0 : 0.0);
            return new SkippableTime(time);
        }
    }
}
