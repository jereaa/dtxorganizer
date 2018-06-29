using System;
using System.Globalization;
using System.Threading;

namespace DTXOrganizer {
    
    internal class Program {
        
        public static void Main(string[] args) {
            
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-GB");

            Organizer organizer = new Organizer();
            organizer.Start();

            Logger logger = Logger.Instance;
            Console.WriteLine("Program finished with: \n" +
                              logger.ErrorLogs + " errors\n" +
                              logger.WarnLogs + " warnings\n" +
                              logger.InfoLogs + " information logs");
        }
    }
}