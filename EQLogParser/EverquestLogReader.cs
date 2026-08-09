using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EQLogParser.Processors;

namespace EQLogParser
{
    public class LogLine
    {
        public DateTime When { get; set; }
        public string Message { get; set; }
    }

    public class EverquestLogReader
    {
        private const int PublishIntervalTicks = 100;
        private const long ScanSearchGranularityBytes = 1024 * 1024;
        private static readonly TimeSpan StartupProgressPublishInterval = TimeSpan.FromMilliseconds(250);
        private static readonly Regex LogLineRegEx = new Regex(@"\[(?<date>(.*?))\]\s(?<message>.*)");
        private static readonly HashSet<LogType> StartupLogTypes = new HashSet<LogType>()
        {
            LogType.PlayerLevel,
            LogType.SpellCastBegin,
            LogType.SpellCastLanded,
            LogType.YourSpellCastFizzled,
            LogType.YourSpellCastWasInterrupted,
            LogType.YouLoseBuff,
            LogType.OtherPlayerCastsBuffOnYou,
            LogType.SpellCastDidNotTakeHold,
            LogType.SpellWornOff
        };

        private readonly string _logFilePath;
        private readonly IReadOnlyList<ILogProcessor> _logProcessors;
        private readonly IReadOnlyList<ILogProcessor> _startupLogProcessors;
        private readonly ParserStatusFactory _parserStatusFactory;
        private readonly IStatusPublisher _statusPublisher;
        private readonly ParserOptions _parserOptions;
        private readonly StartupScanState _startupScanState;
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly FileStream _stream;
        private readonly StreamReader _reader;

        public enum LogType
        {
            PlayerTakesDamage,
            NpcMissedYou,
            SpellCastBegin,
            SpellCastLanded,
            YourSpellCastFizzled,
            YourSpellCastWasInterrupted,
            YouLoseBuff,
            OtherPlayerCastsBuffOnYou,
            SpellCastDidNotTakeHold,
            Camping,
            PlayerLevel,
            SpellWornOff,
            DamageDone
        }

        public EverquestLogReader(
            LogFile logFile,
            IEnumerable<ILogProcessor> logProcessors,
            ParserStatusFactory parserStatusFactory,
            IStatusPublisher statusPublisher,
            ParserOptions parserOptions,
            StartupScanState startupScanState,
            CurrentSpellCast currentSpellCast)
        {
            _logFilePath = logFile.Path;
            _logProcessors = logProcessors.ToArray();
            _startupLogProcessors = _logProcessors
                .Where(x => StartupLogTypes.Contains(x.LogType))
                .ToArray();
            _parserStatusFactory = parserStatusFactory;
            _statusPublisher = statusPublisher;
            _parserOptions = parserOptions;
            _startupScanState = startupScanState;
            _currentSpellCast = currentSpellCast;
            _stream = OpenLogFile(_logFilePath);
            _reader = new StreamReader(_stream);
        }

        public async Task BeginAsync(CancellationToken cancellationToken)
        {
            await ScanStartupWindowAsync(cancellationToken);

            int tickCount = PublishIntervalTicks;
            while (!cancellationToken.IsCancellationRequested)
            {
                string line = _reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    ProcessLine(line);
                }

                if (tickCount >= PublishIntervalTicks)
                {
                    await PublishStatusAsync(cancellationToken);
                    tickCount = 0;
                }

                await Task.Delay(1, cancellationToken);
                tickCount++;
            }
        }

        private static FileStream OpenLogFile(string path)
        {
            FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.End);
            return stream;
        }

        private async Task ScanStartupWindowAsync(CancellationToken cancellationToken)
        {
            int scanHours = Math.Max(0, _parserOptions.StartupScanHours);
            if (scanHours == 0)
            {
                _startupScanState.Skipped("Startup scan disabled");
                await PublishStatusAsync(cancellationToken);
                return;
            }

            DateTime windowEnd = DateTime.Now;
            DateTime windowStart = windowEnd.AddHours(-scanHours);
            long scanEndPosition = new FileInfo(_logFilePath).Length;
            long scanStartPosition = FindScanStartPosition(windowStart, scanEndPosition);
            long scanLength = Math.Max(1, scanEndPosition - scanStartPosition);
            long linesScanned = 0;

            _startupScanState.Begin(windowStart, windowEnd);
            await PublishStatusAsync(cancellationToken);

            using FileStream scanStream = File.Open(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            scanStream.Seek(scanStartPosition, SeekOrigin.Begin);
            using StreamReader scanReader = new StreamReader(scanStream);
            Stopwatch publishTimer = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                string line = scanReader.ReadLine();
                if (line == null)
                {
                    break;
                }

                ProcessLine(line, windowStart, isStartupScan: true);
                linesScanned++;

                if (publishTimer.Elapsed >= StartupProgressPublishInterval)
                {
                    int percent = GetPercent(scanStream.Position - scanStartPosition, scanLength);
                    _startupScanState.Update(percent, linesScanned);
                    await PublishStatusAsync(cancellationToken);
                    publishTimer.Restart();
                }
            }

            _currentSpellCast.Reset();
            _startupScanState.Complete(linesScanned);
            await PublishStatusAsync(cancellationToken);

            _reader.DiscardBufferedData();
            _stream.Seek(scanStream.Position, SeekOrigin.Begin);
        }

        private long FindScanStartPosition(DateTime cutoff, long endPosition)
        {
            if (endPosition == 0)
            {
                return 0;
            }

            using FileStream stream = File.Open(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long low = 0;
            long high = endPosition;

            while (high - low > ScanSearchGranularityBytes)
            {
                long middle = low + ((high - low) / 2);
                stream.Seek(middle, SeekOrigin.Begin);
                MoveToNextLineBoundary(stream, endPosition);

                long lineStart = stream.Position;
                string line = ReadLine(stream, endPosition);
                if (line == null)
                {
                    high = middle;
                    continue;
                }

                if (TryParseLogLine(line, out LogLine logLine) && logLine.When >= cutoff)
                {
                    high = lineStart;
                }
                else
                {
                    low = stream.Position;
                }
            }

            stream.Seek(low, SeekOrigin.Begin);
            while (stream.Position < endPosition)
            {
                long lineStart = stream.Position;
                string line = ReadLine(stream, endPosition);
                if (line == null)
                {
                    break;
                }

                if (TryParseLogLine(line, out LogLine logLine) && logLine.When >= cutoff)
                {
                    return lineStart;
                }
            }

            return Math.Min(high, endPosition);
        }

        private static void MoveToNextLineBoundary(FileStream stream, long endPosition)
        {
            if (stream.Position == 0)
            {
                return;
            }

            while (stream.Position < endPosition)
            {
                if (stream.ReadByte() == '\n')
                {
                    break;
                }
            }
        }

        private static string ReadLine(FileStream stream, long endPosition)
        {
            List<byte> bytes = new List<byte>(256);
            while (stream.Position < endPosition)
            {
                int value = stream.ReadByte();
                if (value == -1)
                {
                    break;
                }

                if (value == '\n')
                {
                    break;
                }

                if (value != '\r')
                {
                    bytes.Add((byte)value);
                }
            }

            return bytes.Count == 0 && stream.Position >= endPosition
                ? null
                : Encoding.UTF8.GetString(bytes.ToArray());
        }

        private static int GetPercent(long bytesRead, long totalBytes)
        {
            if (totalBytes <= 0)
            {
                return 100;
            }

            return Math.Clamp((int)Math.Round((bytesRead / (double)totalBytes) * 100), 0, 100);
        }

        private Task PublishStatusAsync(CancellationToken cancellationToken)
        {
            return _statusPublisher.PublishAsync(_parserStatusFactory.Create(), cancellationToken);
        }

        private void ProcessLine(string line, DateTime? minimumWhen = null, bool isStartupScan = false)
        {
            if (!TryParseLogLine(line, out LogLine logLine))
            {
                return;
            }

            if (minimumWhen != null && logLine.When < minimumWhen.Value)
            {
                return;
            }

            IReadOnlyList<ILogProcessor> processors = isStartupScan ? _startupLogProcessors : _logProcessors;
            foreach (ILogProcessor logProcessor in processors)
            {
                bool isMatch = isStartupScan && logProcessor is IStartupLogProcessor startupLogProcessor
                    ? startupLogProcessor.IsStartupMatch(logLine)
                    : logProcessor.IsMatch(logLine);

                if (isMatch)
                {
                    logProcessor.Process(logLine);
                    break;
                }
            }
        }

        private static bool TryParseLogLine(string line, out LogLine logLine)
        {
            logLine = null;
            Match match = LogLineRegEx.Match(line);
            if (!match.Success)
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                match.Groups["date"].Value,
                "ddd MMM dd HH:mm:ss yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime when))
            {
                return false;
            }

            logLine = new LogLine()
            {
                When = when,
                Message = match.Groups["message"].Value
            };
            return true;
        }
    }
}
