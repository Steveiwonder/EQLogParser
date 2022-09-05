using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
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
        private readonly ILogger _logger;
        private readonly IBuffManager _buffManager;
        private readonly CurrentSpellCast _currentSpellCast;
        private readonly IEnumerable<ILogProcessor> _logProcessors;

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
        public EverquestLogReader(LogFile logFile, IEnumerable<ILogProcessor> logProcessors, ILogger logger, IBuffManager buffManager, CurrentSpellCast currentSpellCast)
        {
            _logger = logger;
            _buffManager = buffManager;
            _currentSpellCast = currentSpellCast;
            _logProcessors = logProcessors;
            _stream = OpenLogFile(logFile.Path);
            _reader = new StreamReader(_stream);
            _firstRead = false;
        }

        private FileStream OpenLogFile(string path)
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public void Begin()
        {
            int tickCount = 0;
            while (true)
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

                if (tickCount >= 500)//draw ever 500ms~
                {
                    DrawConsole();
                    tickCount = 0;
                }
                Thread.Sleep(1);
                tickCount++;

            }
        }

        private ConsoleColor GetBarColor(int percent)
        {
            if (percent > 45)
            {
                return ConsoleColor.Green;
            }
            else if (percent > 25)
            {
                return ConsoleColor.Yellow;
            }
            else if (percent > 10)
            {
                return ConsoleColor.DarkYellow;
            }

            return ConsoleColor.Red;
        }

        private void DrawConsole()
        {
            Console.Clear();

            if (_currentSpellCast.IsCasting)
            {
                Console.WriteLine($"Casting [{_currentSpellCast.Name}]");
            }
            else
            {
                Console.WriteLine();
            }
            foreach (Player player in _buffManager.GetPlayers().OrderBy(x => x.Name))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{player.Name}");
                Console.ForegroundColor = ConsoleColor.White;
                //Console.WriteLine("========================");
                foreach (Buff playerBuff in player.GetBuffs())
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string line = $"[{playerBuff.Name}] ";
                    string pctBar = "[";
                    int percent = playerBuff.Percent;
                    for (int i = 0; i < 50; i++)
                    {
                        if (percent / 2 > i)
                        {
                            pctBar += ".";
                        }
                        else
                        {
                            pctBar += " ";
                        }
                    }

                    pctBar += $"] {percent}%";
                    Console.Write(line);
                    Console.SetCursorPosition(20, Console.CursorTop);
                    Console.ForegroundColor = GetBarColor(percent);
                    Console.WriteLine(pctBar);
                }
                Console.WriteLine("");
            }


        }

        private static readonly Regex LogLineRegEx = new Regex(@"\[(?<date>(.*?))\]\s(?<message>.*)");

        private void ProcessLine(string line)
        {
            Match match = LogLineRegEx.Match(line);
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