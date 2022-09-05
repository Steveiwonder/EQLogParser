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
    class Program
    {


        static void Main(string[] args)
        {

            if (args.Length != 1)
            {
                Console.WriteLine("Supply log file path");
                return;
            }

            string logFilePath = args[0];
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger, ConsoleLogger>();
            serviceCollection.AddSingleton<EverquestLogReader>();
            serviceCollection.AddSingleton(new LogFile() { Path = logFilePath });


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
                SpellParser spellParser = new SpellParser(@"C:\Everquest\p99\spells_en.txt");
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
