using EQLogParser.Contracts;

namespace EQLogParser.Api
{
    public class StatusStore
    {
        private readonly object _lock = new object();
        private ParserStatusUpdate? _current;

        public ParserStatusUpdate? Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public void Set(ParserStatusUpdate status)
        {
            lock (_lock)
            {
                _current = status;
            }
        }
    }
}
