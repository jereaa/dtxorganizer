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

            //TODO we should also look in subfolders here
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(path), "*.dtx")) {
                DTXFile dtxFile = new DTXFile(file);
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
                if (TryChangeValueForProperty(TITLE_PROPERTY, _dtxFiles[0].Title)) {
                    Title = _dtxFiles[0].Title;
                    SaveFile();
                }
            }

            SortDTXFileListByLevel();
            string newInfo = "\r\n";

            for (int i = 0; i < DTX_LABELS.Length; i++) {
                if (_dtxFiles[i] != null) {
                    newInfo += string.Format(PROPERTY_LABEL_PRE + DTX_LABELS[i] + "\r\n", i + 1);
                    newInfo += string.Format(PROPERTY_FILE_PRE + Path.GetFileName(_dtxFiles[i].FilePath) + "\r\n\r\n", i + 1);
                }
            }

            rawValue += newInfo;
            File.AppendAllText(FilePath, newInfo, Encoding.GetEncoding("shift_jis"));
            _dtxFiles.RemoveAll(file => file == null);
            
            Logger.Instance.LogInfo("Created new SET.DEF file for '" + title + "' in '" + Path.GetDirectoryName(path) + "'.");
        }

        private void InitializeDtxFiles() {
            Regex regex = new Regex(@"#L\dFILE\s*:?\s*(?<file>[^.]*\.dtx)\s*\n?");
            MatchCollection matches = regex.Matches(rawValue);

            foreach (Match match in matches) {
                string file = match.Groups["file"].Value;

                DTXFile dtxFile = new DTXFile(Path.Combine(new [] {Path.GetDirectoryName(FilePath), file}));
                if (dtxFile.ProperlyInitialized) {
                    _dtxFiles.Add(dtxFile);
                } else {
                    Logger.Instance.LogWarning("DTX file '" + file + "' not found for '" + Title + "' in folder '" +
                                               Path.GetDirectoryName(FilePath) + "'.");
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