using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public class PlayerLevelDetector
    {
        private static readonly Regex LevelRegex = new Regex(@"Welcome to level (?<level>\d+)!", RegexOptions.IgnoreCase);

        public int? DetectLatestLevel(string logFilePath)
        {
            int? level = null;

            using (FileStream fs = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        Match match = LevelRegex.Match(line);
                        if (match.Success && int.TryParse(match.Groups["level"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedLevel))
                        {
                            level = parsedLevel;
                        }
                    }
                }
            }

            return level;
        }
    }
}
