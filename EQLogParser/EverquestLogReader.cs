using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private readonly IEnumerable<ILogProcessor> _logProcessors;
        private readonly ParserStatusFactory _parserStatusFactory;
        private readonly IStatusPublisher _statusPublisher;

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
            Camping
        }
        private readonly FileStream _stream;
        private readonly StreamReader _reader;
        private bool _firstRead = true;
        public EverquestLogReader(
            LogFile logFile,
            IEnumerable<ILogProcessor> logProcessors,
            ParserStatusFactory parserStatusFactory,
            IStatusPublisher statusPublisher)
        {
            _logProcessors = logProcessors;
            _parserStatusFactory = parserStatusFactory;
            _statusPublisher = statusPublisher;
            _stream = OpenLogFile(logFile.Path);
            _reader = new StreamReader(_stream);
            _firstRead = false;
        }

        private FileStream OpenLogFile(string path)
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public async Task BeginAsync(CancellationToken cancellationToken)
        {
            int tickCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                string line = _reader.ReadLine();
                if (_firstRead)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        _firstRead = false;
                    }
                    continue;
                }

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

        private static readonly Regex LogLineRegEx = new Regex(@"\[(?<date>(.*?))\]\s(?<message>.*)");

        private Task PublishStatusAsync(CancellationToken cancellationToken)
        {
            return _statusPublisher.PublishAsync(_parserStatusFactory.Create(), cancellationToken);
        }

        private void ProcessLine(string line)
        {
            Match match = LogLineRegEx.Match(line);
            if (!match.Success)
            {
                return;
            }

            LogLine logLine = new LogLine()
            {
                When = DateTime.ParseExact(match.Groups["date"].Value, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture),
                Message = match.Groups["message"].Value
            };
            foreach (var logProcessor in _logProcessors)
            {
                if (logProcessor.IsMatch(logLine))
                {
                    logProcessor.Process(logLine);
                    break;
                }
            }
        }

    }

}
