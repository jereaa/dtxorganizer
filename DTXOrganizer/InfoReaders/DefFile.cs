using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DTXOrganizer {
    
    public class DefFile : BaseFile {

        private static readonly string[] DTX_LABELS = {"BASIC", "ADVANCED", "EXTREME", "MASTER"};
        private static readonly float[] DTX_LEVELS = {4.0f, 6.0f, 7.5f};
        private const string PROPERTY_LABEL_PRE = "#L{0}LABEL: ";
        private const string PROPERTY_FILE_PRE = "#L{0}FILE: ";

        private readonly List<DTXFile> _dtxFiles = new List<DTXFile>();

        public DefFile() {
            
        }
        
        public DefFile(string path) : base(path) {
            if (!ProperlyInitialized) {
                return;
            }
            
            InitializeDtxFiles();
        }

        public DefFile(string title, string path, bool getTitleFromDtx = true) : base(title, path) {
            if (!ProperlyInitialized) {
                return;
            }

            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path), "*.dtx", SearchOption.AllDirectories)) {
                DTXFile dtxFile = new DTXFile(file, Path.GetDirectoryName(FilePath));
                if (dtxFile.ProperlyInitialized) {
                    _dtxFiles.Add(dtxFile);
                }
            }

            if (_dtxFiles.Count == 0) {
                Logger.Instance.LogError("No DTX files were found in '" + Path.GetDirectoryName(FilePath) + "'.");
                ProperlyInitialized = false;
                return;
            }
            
            _dtxFiles.Sort();

            if (getTitleFromDtx) {
                if (TryChangeValueForProperty(PROPERTY_TITLE, _dtxFiles[0].Title)) {
                    Title = _dtxFiles[0].Title;
                    if (!SaveFile()) {
                        Logger.Instance.LogError($"Couldn't save file '{FilePath}' after setting title to '{Title}'");
                    }
                }
            }

            SortDTXFileListByLevel();
            string newInfo = "\r\n";
            Uri defFileUri = new Uri(FilePath);

            for (int i = 0; i < DTX_LABELS.Length; i++) {
                if (_dtxFiles[i] != null) {
                    Uri dtxFileUri = new Uri(_dtxFiles[i].FilePath);
                    newInfo += string.Format(PROPERTY_LABEL_PRE + DTX_LABELS[i] + "\r\n", i + 1);
                    newInfo += string.Format(
                        PROPERTY_FILE_PRE + defFileUri.MakeRelativeUri(dtxFileUri).ToString().Replace('/', Path.DirectorySeparatorChar) +
                        "\r\n\r\n", i + 1);
                }
            }

            rawValue += newInfo;
            File.AppendAllText(FilePath, newInfo, Encoding.GetEncoding("shift_jis"));
            _dtxFiles.RemoveAll(file => file == null);
            
            Logger.Instance.LogInfo("Created new SET.DEF file for '" + Title + "' in '" + Path.GetDirectoryName(path) + "'.");
        }

        private void InitializeDtxFiles() {
            Regex regex = new Regex(@"#L\dFILE\s*:?\s*(?<file>[^.]*\.dtx)\s*\n?");
            MatchCollection matches = regex.Matches(rawValue);

            foreach (Match match in matches) {
                string file = match.Groups["file"].Value;

                DTXFile dtxFile = new DTXFile(Path.Combine(new [] {Path.GetDirectoryName(FilePath), file}), Path.GetDirectoryName(FilePath));
                if (dtxFile.ProperlyInitialized) {
                    _dtxFiles.Add(dtxFile);
                }
            }
            
            _dtxFiles.Sort();
        }

        /// <summary>
        /// Sets the DTXFiles list capacity to 4 and moves elements in the list for them
        /// to be aligned with their difficulty level.
        /// </summary>
        private void SortDTXFileListByLevel() {
            if (_dtxFiles.Count == DTX_LABELS.Length) {
                return;
            }

            while (_dtxFiles.Count < DTX_LABELS.Length) {
                _dtxFiles.Add(null);
            }

            // Files are sorted and are right-aligned in the list.
            // We move to the right the files that correspond according to their level.
            for (int i = DTX_LABELS.Length - 2; i >= 0; i--) {
                if (i >= 3 || _dtxFiles[i] == null) {
                    continue;
                }

                if (_dtxFiles[i].Level > DTX_LEVELS[i] && _dtxFiles[i + 1] == null) {
                    _dtxFiles[i + 1] = _dtxFiles[i];
                    _dtxFiles[i] = null;
                    i += 2;
                }
            }
        }

        public override bool RenameSongFolderToTitle() {
            if (!base.RenameSongFolderToTitle()) {
                return false;
            }
            
            // Since path has changed, DTX files' path has also changed,
            // so it's easier to re-initialize them.
            _dtxFiles.Clear();
            InitializeDtxFiles();
            return true;
        }

        public override void FindProblems(bool autoFix) {
            Regex regex = new Regex(@"(?<prop>#L(?<num>\d)FILE\s*:?)\s*(?<file>[^.]*\.dtx)");
            MatchCollection matches = regex.Matches(rawValue);

            using (UserPrompt userPrompt = new UserPrompt()) {
                
                foreach (Match match in matches) {
                    string file = match.Groups["file"].Value;
                    string filePath = Path.Combine(new[] {Path.GetDirectoryName(FilePath), file});
    
                    if (!File.Exists(filePath)) {

                        if (!autoFix) {
                            Logger.Instance.LogWarning($"Couldn't find file '{Path.GetFileName(filePath)}' in '{filePath}'.");
                            
                        } else {
                            
                            // Get all DTX Files in song directory (including subdirectories)
                            List<string> dtxFiles = Directory.GetFiles(Path.GetDirectoryName(FilePath), "*.dtx",
                                SearchOption.AllDirectories).ToList();
                            // Remove all DTX Files which are already initialized (since they were found)
                            dtxFiles.RemoveAll(dtxFilePath => _dtxFiles.Exists(file1 => file1.FilePath == dtxFilePath));

                            if (dtxFiles.Count != 0) {
                                // Make paths relative to Def file.
                                string[] unbindedDtxFiles = dtxFiles.Select(fullPath =>
                                    fullPath.Replace(Path.GetDirectoryName(FilePath) + "\\", "")).ToArray();
                                
                                string propertyName = match.Groups["prop"].Value;
                                string prompt = $"\n\nPlease select appropiate file for '{propertyName}' property in song '{Title}':";
                                
                                int fileIndex = userPrompt.PromptUserForChoice(prompt, unbindedDtxFiles, file, true, false);

                                // If user chose to leave the value as it already was
                                if (fileIndex == unbindedDtxFiles.Length) {
                                    continue;
                                }

                                if (TryChangeValueForProperty(propertyName, unbindedDtxFiles[fileIndex])) {
                                    
                                    DTXFile dtxFile = new DTXFile(Path.Combine(Path.GetDirectoryName(FilePath),
                                        unbindedDtxFiles[fileIndex]), Path.GetDirectoryName(FilePath));
                                    
                                    if (dtxFile.ProperlyInitialized) {
                                        _dtxFiles.Add(dtxFile);
                                        SaveFile();
                                        Logger.Instance.LogInfo(
                                            $"Changed property '{propertyName}' from '{file}' to '{unbindedDtxFiles[fileIndex]}' in file '{FilePath}'");
                                    }
                                } else {
                                    Logger.Instance.LogError(
                                        $"Couldn't change property '{propertyName}' from '{file}' to '{unbindedDtxFiles[fileIndex]}' in file {Title}.");
                                }
                            } else {
                                int numToDelete = int.Parse(match.Groups["num"].Value);
                                DeleteDtxFromDefinition(numToDelete);
                            }
                        }
                    }
                }
            }

            foreach (DTXFile dtxFile in _dtxFiles) {
                dtxFile.FindProblems(autoFix);
            }
        }

        private void DeleteDtxFromDefinition(int dtxNum) {
            DeleteProperty(string.Format(PROPERTY_LABEL_PRE, dtxNum));
            DeleteProperty(string.Format(PROPERTY_FILE_PRE, dtxNum));
        }

        #region Debug

        public void LogDTXList() {
            foreach (DTXFile dtxFile in _dtxFiles) {
                Logger.Instance.LogInfo("DTXFile '" + dtxFile.Title + "':\n" +
                                        "\tArtist: '" + dtxFile.Artist + "'\n" +
                                        "\tComment: '" + dtxFile.Comment + "'\n" +
                                        "\tBPM: " + dtxFile.Bpm + "\n" +
                                        "\tLevel: " + dtxFile.Level + "\n");
            }
        }

        public void LogDTXLevels() {
            foreach (DTXFile dtxFile in _dtxFiles) {
                Logger.Instance.LogInfo("Level: " + dtxFile.Level + " - " + Title);
            }
        }
        
#endregion
    }
}