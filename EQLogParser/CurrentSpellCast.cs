using System;

namespace EQLogParser
{
    public class CurrentSpellCast
    {
        private static readonly TimeSpan LandedMessageWindow = TimeSpan.FromSeconds(3);
        private DateTime? _lastLandedAt;

        public string Name { get; private set; }
        public bool IsCasting { get; private set; }
        public bool LastCastFizzled { get; private set; }
        public bool LastCastInterrupted { get; set; }
        public bool LastCastDidNotTakeHold { get; set; }
        public string[] CastLandedMessages { get; private set; } = new string[0];

        public void BeginCast(string name, string[] castLandedMessages)
        {
            IsCasting = true;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = false;
            Name = name;
            CastLandedMessages = castLandedMessages;
            _lastLandedAt = null;
        }

        public bool CanMatchLandedMessage(DateTime when)
        {
            if (IsCasting)
            {
                return true;
            }

            return _lastLandedAt != null
                && when - _lastLandedAt.Value <= LandedMessageWindow;
        }

        public void CastFizzled()
        {
            IsCasting = false;
            LastCastFizzled = true;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = false;
            ClearCast();
        }

        public void CastLanded(DateTime when)
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = false;
            _lastLandedAt = when;
        }

        public void CastInterrupted()
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = true;
            LastCastDidNotTakeHold = false;
            ClearCast();
        }

        public void CastDidNotTakeHold()
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = true;
            ClearCast();
        }

        public void Reset()
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = false;
            ClearCast();
        }

        private void ClearCast()
        {
            Name = null;
            CastLandedMessages = new string[0];
            _lastLandedAt = null;
        }
    }
}
