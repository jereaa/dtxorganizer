using System;
using System.IO;

namespace DTXOrganizer {
    
    public class Logger : Singleton<Logger> {

        private static readonly string LOG_PATH = "./DTXO.log";
        
        private static readonly string PREFIX_INFO = "[INFO] ";
        private static readonly string PREFIX_WARNING = "[WARNING] ";
        private static readonly string PREFIX_ERROR = "[ERROR] ";

        public int InfoLogs { get; private set; }
        public int WarnLogs { get; private set; }
        public int ErrorLogs { get; private set; }
        
        public Logger() {
            if (File.Exists(LOG_PATH)) {
                File.Delete(LOG_PATH);
            }
        }
        
        public void LogInfo(string log) {
            Log(PREFIX_INFO + log);
            InfoLogs++;
        }

        public void LogWarning(string log) {
            Log(PREFIX_WARNING + log);
            WarnLogs++;
        }

        public void LogError(string log) {
            Log(PREFIX_ERROR + log);
            ErrorLogs++;
        }

        private void Log(string log) {
            DateTime now = DateTime.Now;
            string logLine = now.ToString("yyyy/MM/dd - HH:mm:ss.fff zzz") + ": " + log + "\n";
            
            File.AppendAllText(LOG_PATH, logLine);
        }
        
    }
}