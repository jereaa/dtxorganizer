using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DTXOrganizer {
    
    public class Organizer {

//        private static readonly string PATH_TO_FILES = "D:\\DTXMania\\testfiles";

        public void ListTitles() {
            JObject jObject = JObject.Parse(File.ReadAllText("./config.json"));
            string pathToFiles = jObject["path"].ToString();

            IEnumerable<string> directories = Directory.EnumerateDirectories(pathToFiles);
            
            foreach (string dir in directories) {
                bool foundFile = false;
                
                foreach (string file in Directory.EnumerateFiles(dir)) {
                    if (Path.GetFileName(file)?.ToLower() == "set.def") {
                        foundFile = true;
                        DefFile defFile = new DefFile(file);
                        if (defFile.ProperlyInitialized) {
//                            Logger.Instance.LogInfo(defFile.Title);
//                            defFile.LogDTXLevels();
                            if (!Path.GetFileName(Path.GetFileName(defFile.FilePath)).Equals(defFile.Title)) {
                                defFile.RenameSongFolderToTitle();
                            }
                        }

                        break;
                    }
                }

                if (!foundFile) {
                    Logger.Instance.LogError("Couldn't find file SET.DEF in folder '" + Path.GetFileName(dir) + "'");
                    DefFile newDefFile = new DefFile(Path.GetFileName(dir), Path.Combine(new []{dir, "SET.def"}));
                }
            }
        }

    }
}