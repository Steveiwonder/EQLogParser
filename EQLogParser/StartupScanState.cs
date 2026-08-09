using System;
using EQLogParser.Contracts;

namespace EQLogParser
{
    public class StartupScanState
    {
        private readonly object _lock = new object();
        private StartupScanStatus _status = new StartupScanStatus();

        public StartupScanStatus Current
        {
            get
            {
                lock (_lock)
                {
                    return Copy(_status);
                }
            }
        }

        public void Begin(DateTime windowStart, DateTime windowEnd)
        {
            lock (_lock)
            {
                _status = new StartupScanStatus()
                {
                    IsScanning = true,
                    Message = "Scanning recent log entries",
                    Percent = 0,
                    LinesScanned = 0,
                    WindowStart = windowStart,
                    WindowEnd = windowEnd
                };
            }
        }

        public void Update(int percent, long linesScanned)
        {
            lock (_lock)
            {
                _status.IsScanning = true;
                _status.Message = "Scanning recent log entries";
                _status.Percent = Math.Clamp(percent, 0, 100);
                _status.LinesScanned = linesScanned;
            }
        }

        public void Complete(long linesScanned)
        {
            lock (_lock)
            {
                _status.IsScanning = false;
                _status.Message = "Startup scan complete";
                _status.Percent = 100;
                _status.LinesScanned = linesScanned;
            }
        }

        public void Skipped(string message)
        {
            lock (_lock)
            {
                _status = new StartupScanStatus()
                {
                    IsScanning = false,
                    Message = message,
                    Percent = 100
                };
            }
        }

        private static StartupScanStatus Copy(StartupScanStatus status)
        {
            return new StartupScanStatus()
            {
                IsScanning = status.IsScanning,
                Message = status.Message,
                Percent = status.Percent,
                LinesScanned = status.LinesScanned,
                WindowStart = status.WindowStart,
                WindowEnd = status.WindowEnd
            };
        }
    }
}
