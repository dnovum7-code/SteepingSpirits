using System;

namespace SteepingSpirits.Processing
{
    /// <summary>
    /// Game time in minutes for the passive steps. In the game it docks onto the garden's day clock
    /// (IDayClock days) plus the time of day; a later tea-house day system can replace it.
    /// Must never run backwards on purpose – if it does (clock reset), batches simply wait.
    /// </summary>
    public interface IGameTime
    {
        double Minutes { get; }
    }

    /// <summary>A clock moved by hand (tests, simulator, scene without game clock).</summary>
    public sealed class ManualGameTime : IGameTime
    {
        public ManualGameTime(double minutes = 0)
        {
            Minutes = minutes;
        }

        public double Minutes { get; private set; }

        public void Advance(double minutes)
        {
            if (minutes > 0) Minutes += minutes;
        }
    }

    /// <summary>Day number plus minute of day as one continuous time (1 day = 1440 minutes).</summary>
    public sealed class DayAndMinuteTime : IGameTime
    {
        private readonly Func<int> day;
        private readonly Func<double> minuteOfDay;

        public DayAndMinuteTime(Func<int> day, Func<double> minuteOfDay)
        {
            this.day = day;
            this.minuteOfDay = minuteOfDay;
        }

        public double Minutes => day() * 1440.0 + minuteOfDay();
    }

    /// <summary>Wraps a clock and adds a debug offset ("fast forward").</summary>
    public sealed class SkippableTime : IGameTime
    {
        private readonly IGameTime inner;

        public SkippableTime(IGameTime inner)
        {
            this.inner = inner;
        }

        /// <summary>Minutes skipped with the debug function.</summary>
        public double Offset { get; private set; }

        public double Minutes => inner.Minutes + Offset;

        public void FastForward(double minutes)
        {
            if (minutes > 0) Offset += minutes;
        }
    }
}
