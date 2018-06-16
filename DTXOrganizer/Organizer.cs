using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DTXOrganizer {
    
    public class Organizer {

        private string PathToFiles { get; }
        
        public Organizer() {
            JObject jObject = JObject.Parse(File.ReadAllText("./config.json"));
            PathToFiles = jObject["path"].ToString();
        }

        public void Start() {
            OptionsMenu menu = new OptionsMenu();
            menu.Add("Check files for errors", () => CheckFiles());
            menu.Add("Check files for errors and try to fix them", () => CheckFiles(true));
            menu.Add("Exit", () => { });
            
            menu.DisplayMenu();
        }

        private void CheckFiles(bool tryToFix = false) {
            string[] directories = Directory.GetDirectories(PathToFiles);

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
                        if (tryToFix) {
                            defFile = new DefFile(Path.GetFileName(directories[i]),
                                Path.Combine(new[] {directories[i], "SET.def"}));
                        }
                    }
                    
                    if (defFile.ProperlyInitialized) {
                        defFile.FindProblems(tryToFix);

                        string dirName = Path.GetFileName(Path.GetDirectoryName(defFile.FilePath));
                        if (dirName != null && !dirName.Equals(defFile.Title)) {
                            if (tryToFix) {
                                defFile.RenameSongFolderToTitle();
                            } else {
                                Logger.Instance.LogWarning(
                                    $"Song directory should be renamed to match song title '{defFile.Title}'. Current dir name: '{dirName}'");
                            }
                        }
                    }
                    
                    progressBar.Report((double) i / directories.Length);
                }
            }
        }
        
        //TODO reduce to rubble
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