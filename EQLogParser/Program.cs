using System;
using System.Collections.Generic;
using EQLogParser.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace EQLogParser
{
    public class LogFile
    {
        public string Path { get; set; }
    }

    public class SpellFile
    {
        public string Path { get; set; }
    }

    class Program
    {
        private const string DefaultLegendsSpellFilePath = @"C:\Users\Public\Daybreak Game Company\Installed Games\EverQuest Legends\spells_us.txt";


        static void Main(string[] args)
        {

            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Usage: EQLogParser <log file path> [spell file path]");
                return;
            }

            string logFilePath = args[0];
            string spellFilePath = args.Length == 2 ? args[1] : DefaultLegendsSpellFilePath;
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger, ConsoleLogger>();
            serviceCollection.AddSingleton<EverquestLogReader>();
            serviceCollection.AddSingleton(new LogFile() { Path = logFilePath });
            serviceCollection.AddSingleton(new SpellFile() { Path = spellFilePath });


            serviceCollection.AddSingleton<ILogProcessor, NpcMissedYouLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, SpellCastBeginLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, PlayerTakesDamageLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, SpellCastLandedLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, YourSpellCastFizzledLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, YourSpellCastWasInterruptedLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, YouLoseBuffLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, OtherPlayerCastsBuffOnYouLogProcessor>();
            serviceCollection.AddSingleton<ILogProcessor, SpellCastDidNotTakeHoldLogProcessor>();
            serviceCollection.AddSingleton<IBuffManager, BuffManager>();
            serviceCollection.AddSingleton<CurrentSpellCast>();
            serviceCollection.AddSingleton(provider =>
            {
                SpellFile spellFile = provider.GetRequiredService<SpellFile>();
                SpellParser spellParser = new SpellParser(spellFile.Path);
                return spellParser.GetSpells();

            });
            serviceCollection.AddSingleton<SpellCache>();

            Run(serviceCollection.BuildServiceProvider());
        }

        private static void Run(IServiceProvider serviceProvider)
        {
            using (IServiceScope scopedServiceProvider = serviceProvider.CreateScope())
            {
                EverquestLogReader logReader = scopedServiceProvider.ServiceProvider.GetRequiredService<EverquestLogReader>();
                logReader.Begin();
            }
        }

    }


}
