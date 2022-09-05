using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public static class LineHelper
    {
        private static Regex DateTimeRegex = new Regex(@"\[(.*)\]");
        public static DateTime GetDateTime(string line)
        {

            string dateTime = DateTimeRegex.Match(line).Groups[1].Value;

            return  DateTime.ParseExact(dateTime, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);

        }

        public static string StripDate(string line)
        {
            return line.Substring(line.IndexOf(']') + 1).Trim();
        }
    }
}