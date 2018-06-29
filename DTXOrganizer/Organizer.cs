using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace DTXOrganizer {
    
    public class Organizer {

        private string PathToSongs { get; }
        private string PathToNewSongs { get; }
        
        public Organizer() {
            JObject jObject = JObject.Parse(File.ReadAllText("./config.json"));
            PathToSongs = jObject["path"].ToString();
            PathToNewSongs = jObject["newSongsPath"].ToString();
        }

        public void Start() {
            OptionsMenu menu = new OptionsMenu();
            menu.Add("Check files for errors", () => IterateOverSongs(PathToSongs, CheckFiles));
            menu.Add("Check files for errors and try to fix them", () => IterateOverSongs(PathToSongs, CheckFiles, true));
            menu.Add("Move new anime songs into correct folder", () => IterateOverSongs(PathToNewSongs, MoveToFolder));
            menu.Add("Exit", () => { });
            
            Console.Clear();
            menu.DisplayMenu("Please choose action to perform...");
        }

        private void IterateOverSongs(string pathToSongs, Action<DefFile, bool> songAction, bool tryToFix = false) {
            string[] baseDirs = Directory.GetDirectories(pathToSongs);
            List<string> songDirectories = new List<string>();

            foreach (string dir in baseDirs) {
                if (File.Exists(Path.Combine(dir, "box.def"))) {
                    songDirectories.AddRange(Directory.GetDirectories(dir));
                } else {
                    songDirectories.Add(dir);
                }
            }

            Console.Write("Going over songs... ");
            using (ProgressBar progressBar = new ProgressBar()) {
                Console.WriteLine();

                for (int i = 0; i < songDirectories.Count; i++) {
                    bool foundFile = false;
    
                    DefFile defFile = new DefFile();
                    foreach (string file in Directory.GetFiles(songDirectories[i])) {
                        if (Path.GetFileName(file).ToLower() == "set.def") {
                            foundFile = true;
                            defFile = new DefFile(file);
                            break;
                        }
                    }
    
                    if (!foundFile) {
                        Logger.Instance.LogError(
                            $"Couldn't find file SET.DEF in folder '{Path.GetFileName(songDirectories[i])}'");
                        
                        if (tryToFix) {
                            defFile = new DefFile(Path.GetFileName(songDirectories[i]),
                                Path.Combine(new[] {songDirectories[i], "SET.def"}));
                        }
                    }
                    
                    if (defFile.ProperlyInitialized) {
                        songAction(defFile, tryToFix);
                    }
                    
                    progressBar.Report((double) i / songDirectories.Count);
                }
            }
        }
        
        private void CheckFiles(DefFile defFile, bool tryToFix) {
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

        private void MoveToFolder(DefFile defFileToMove, bool tryToFix) {
            char firstChar = defFileToMove.Title[0];
            bool isKatakana = firstChar >= 0x30A0 && firstChar <= 0x30FF;
            if (firstChar > 122) {    // ASCII table values
                firstChar = TranslationTool.GetPhoneticReading(defFileToMove.Title)[0];

                if (firstChar > 122) {
                    firstChar = TranslationTool.GetTranslation(defFileToMove.Title)[0];
                }
                
                if (isKatakana && (firstChar == 'r' || firstChar == 'R')) {    // Check if letter was actually an L
                    OptionsMenu menu = new OptionsMenu();
                    menu.Add("R", () => firstChar = 'R');
                    menu.Add("L", () => firstChar = 'L');
                    menu.DisplayMenu($"Original song name: '{defFileToMove.Title}'. Please choose folder letter to move the file to...");
                }
            }
            
            string folderName = string.Format(Constants.FOLDER_NAME_FORMAT, Constants.NameDirMap[firstChar]);
            string folderPath = Path.Combine(PathToSongs, folderName);

            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
                string boxDefPath = Path.Combine(folderPath, Path.GetFileName(Constants.PATH_BOX_DEF_BASE));
                File.Copy(Constants.PATH_BOX_DEF_BASE, boxDefPath);
                File.Copy(Constants.PATH_FOLDER_IMG, Path.Combine(folderPath, Path.GetFileName(Constants.PATH_FOLDER_IMG)));
                
                BoxFile boxFile = new BoxFile(boxDefPath);
                boxFile.SetTitle(folderName);
            }

            string newFolderPath =
                Path.Combine(folderPath, Path.GetFileName(Path.GetDirectoryName(defFileToMove.FilePath)));

            // If dest folder already exist, we will start enumerating the folders with exact same song name
            if (Directory.Exists(newFolderPath)) {
                Regex regex = new Regex(@".*_(?<fileNum>\d{2})$");
                Match match = regex.Match(newFolderPath);

                // If there's already more than one, then we need to keep the numbering order
                if (match.Success) {
                    int currentNum = int.Parse(match.Groups["fileNum"].Value) + 1;
                    newFolderPath = newFolderPath.Replace($"_{match.Groups["fileNum"].Name}", $"_{currentNum:D2}");
                } else {
                    newFolderPath += "_02";
                }
            }
            
            Directory.Move(Path.GetDirectoryName(defFileToMove.FilePath), newFolderPath);
            
            Logger.Instance.LogInfo($"Moved song '{defFileToMove.Title}' to folder '{folderName}'");
        }
    }
}