using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DTXOrganizer {
    
    public class Organizer {

//        private static readonly string PATH_TO_FILES = "D:\\DTXMania\\testfiles";

        public void ListTitles() {
            JObject jObject = JObject.Parse(File.ReadAllText("./config.json"));
            string pathToFiles = jObject["path"].ToString();

            string[] directories = Directory.GetDirectories(pathToFiles);

            Console.Write("Going over directories... ");
            using (ProgressBar progressBar = new ProgressBar()) {
                
                Console.Write("\n");
                for (int i = 0; i < directories.Length; i++) {
                    bool foundFile = false;
    
                    DefFile defFile = new DefFile();
                    foreach (string file in Directory.GetFiles(directories[i])) {
                        if (Path.GetFileName(file).ToLower() == "set.def") {
                            foundFile = true;
                            defFile = new DefFile(file);
                            break;
                        }
                    }
    
                    if (!foundFile) {
                        Logger.Instance.LogWarning("Couldn't find file SET.DEF in folder '" +
                                                 Path.GetFileName(directories[i]) + "'");
                        defFile = new DefFile(Path.GetFileName(directories[i]),
                            Path.Combine(new[] {directories[i], "SET.def"}));
                    }
                    
                    if (defFile.ProperlyInitialized) {
    //                    Logger.Instance.LogInfo(defFile.Title);
    //                    defFile.LogDTXLevels();
                        defFile.FindProblems(true);
                                
                        if (!Path.GetFileName(Path.GetFileName(defFile.FilePath)).Equals(defFile.Title)) {
                            defFile.RenameSongFolderToTitle();
                        }
                    }
                    
                    progressBar.Report((double) i / directories.Length);
                }
            }
            Console.WriteLine("");
        }

    }
}