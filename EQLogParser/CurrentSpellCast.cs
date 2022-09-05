namespace EQLogParser
{
    public class CurrentSpellCast
    {
        public string Name { get; private set; }
        
        public bool IsCasting { get; private set; }
        public bool LastCastFizzled { get; private set; }
        public bool LastCastInterrupted { get; set; }
        public bool LastCastDidNotTakeHold { get; set; }
        public string[] CastLandedMessages { get; private set; } = new string[0];
        public void BeginCast(string name, string[] castLandedMessages)
        {
            IsCasting = true;
            Name = name;
            CastLandedMessages = castLandedMessages;
        }

        public void CastFizzled()
        {
            IsCasting = false;
            LastCastFizzled = true;
            LastCastInterrupted = false;
            Name = null;
            CastLandedMessages = new string[0]; ;
        }

        public void CastLanded()
        {

            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            Name = null;
            CastLandedMessages = new string[0]; ;
        }

        public void CastInterrupted()
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = true;
            Name = null;
            CastLandedMessages = new string[0]; 
        }


        public void CastDidNotTakeHold()
        {
            IsCasting = false;
            LastCastFizzled = false;
            LastCastInterrupted = false;
            LastCastDidNotTakeHold = false;
            Name = null;
            CastLandedMessages = new string[0];
        }

        
    }
}