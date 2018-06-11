using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DTXOrganizer {
    
    public class DTXFile : BaseFile, IComparable<DTXFile> {

#region Constants
        private static readonly string PROPERTY_ARTIST = "#ARTIST";
        private static readonly string PROPERTY_LEVEL = "#DLEVEL";
        private static readonly string PROPERTY_COMMENT = "#COMMENT";
        private static readonly string PROPERTY_PREVIEW = "#PREVIEW";
        private static readonly string PROPERTY_PREIMAGE = "#PREIMAGE";
        private static readonly string PROPERTY_RESULTIMAGE = "#RESULTIMAGE";
        private static readonly string PROPERTY_BPM = "#BPM";
        
        private static readonly string REGEX_AVI = @"(?<prop>#AVI\w{2}\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{3}))";
        private static readonly string REGEX_WAV = @"(?<prop>#WAV\w{2}\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{2,3}))";
        private static readonly string REGEX_PREVIEW = @"(?<prop>#PREVIEW\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{2,3}))";
        private static readonly string REGEX_PREMOVIE = @"(?<prop>#PREMOVIE\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{3}))";
        private static readonly string REGEX_PREIMAGE = @"(?<prop>#PREIMAGE\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{3}))";
        private static readonly string REGEX_RESULTIMAGE = @"(?<prop>#RESULTIMAGE\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{3}))";
        private static readonly string REGEX_STAGEFILE = @"(?<prop>#STAGEFILE\s*:?)\s*(?<file>[^.]*\.(?<ext>\w{3}))";
#endregion
        
        public string SongDirPath { get; }
        
        public string Artist { get; }
        public string Comment { get; }
        public string PreviewFile { get; }
        public string PreImageFile { get; }
        public string ResultImageFile { get; }
        public float Level { get; }
        public int Bpm { get; }

        public DTXFile(string path, string songDirectoryPath) : base(path) {
            if (!ProperlyInitialized) {
                return;
            }

            SongDirPath = songDirectoryPath;

            TryGetValueForProperty(PROPERTY_ARTIST, out string artist);
            Artist = artist;

            TryGetValueForProperty(PROPERTY_LEVEL, out string level);
            Level = float.Parse(level);
            while (Level >= 10) {
                Level /= 10;
            }

            TryGetValueForProperty(PROPERTY_BPM, out string bpm);
            Bpm = (int) float.Parse(bpm);

            if (TryGetValueForProperty(PROPERTY_COMMENT, out string comment)) {
                Comment = comment;
            }

            if (TryGetValueForProperty(PROPERTY_PREVIEW, out string preview)) {
                PreviewFile = preview;
            }

            if (TryGetValueForProperty(PROPERTY_PREIMAGE, out string preImage)) {
                PreImageFile = preImage;
            }

            if (TryGetValueForProperty(PROPERTY_RESULTIMAGE, out string resultImage)) {
                ResultImageFile = resultImage;
            }
        }

        // TODO this shouldn't be here. Should use a custom comparator when sorting the DTXFile list in DefFiles
        // TODO Should implement a real comparator to evaluate if songs are equal between them.
        public int CompareTo(DTXFile other) {
            if (other == null || !other.ProperlyInitialized) {
                if (!ProperlyInitialized) {
                    return 0;
                }

                return 1;
            }

            if (!ProperlyInitialized) {
                return -1;
            }

            return Level.CompareTo(other.Level);
        }

        public override void FindProblems(bool autoFix) {
            CheckPropertyFile(REGEX_PREVIEW, autoFix);
            CheckPropertyFile(REGEX_PREIMAGE, autoFix);
            CheckPropertyFile(REGEX_PREMOVIE, autoFix);
            CheckPropertyFile(REGEX_RESULTIMAGE, autoFix);
            CheckPropertyFile(REGEX_STAGEFILE, autoFix);
            CheckPropertyFile(REGEX_AVI, autoFix);
            CheckPropertyFile(REGEX_WAV, autoFix);
        }

        /// <summary>
        /// Checks if file is correctly referenced in the given property
        /// </summary>
        /// <param name="propertyRegex">Propery regex to look. Needs named groups 'file', 'ext' and 'prop' to
        /// get relative file path, file extension and the property searched respectively</param>
        /// <param name="autoFix">True if we want to fix the problems, or false just to log them</param>
        private void CheckPropertyFile(string propertyRegex, bool autoFix) {
            Regex regex = new Regex(propertyRegex);
            MatchCollection matches = regex.Matches(rawValue);

            string dirPath = Path.GetDirectoryName(FilePath);

            using (UserPrompt userPrompt = new UserPrompt()) {
                foreach (Match match in matches) {
                    string file = match.Groups["file"].Value;
                    string property = match.Groups["prop"].Value;
                    string filePath = Path.Combine(dirPath, file);

                    if (!File.Exists(filePath)) {

                        if (!autoFix) {
                            Logger.Instance.LogWarning($"Couldn't find file '{file}' for property '{property}' in '{FilePath}'");
                            
                        } else {
                            
                            string extension = match.Groups["ext"].Value;
                            
                            // Get all file paths in song directory which have the same extension as the desired file
                            List<string> allFiles = Directory
                                .GetFiles(SongDirPath, $"*.{extension}", SearchOption.AllDirectories).ToList();
                            allFiles.RemoveAll(possibleFile => rawValue.IndexOf(Path.GetFileName(possibleFile), StringComparison.Ordinal) != -1);

                            // Show shorter paths to user
                            string[] choices = allFiles.Select(fullPath => fullPath.Remove(0, SongDirPath.Length + 1)).ToArray();

                            if (choices.Length > 0) {
                                int choice = userPrompt.PromptUserForChoice(
                                    $"Please select which file should be binded to property {property} in song {Title}",
                                    choices, file);

                                // If user wants to keep binding as it is
                                if (choice == choices.Length) {
                                    continue;
                                }

                                // If user wants to delete the property
                                if (choice == choices.Length + 1) {
                                    DeleteProperty(property);
                                    continue;
                                }
                                
                                var currentPath = allFiles[choice];
                                
                                // Change filename to name used in DTX file
                                string newPath =
                                    currentPath.Replace(Path.GetFileName(currentPath),
                                        Path.GetFileName(file));
                                File.Move(currentPath, newPath);
                                
                                Uri dtxUri = new Uri(FilePath);
                                Uri bindingFileUri = new Uri(newPath);

                                // Create relative path string to binded file from DTX file.
                                string relativePath = dtxUri.MakeRelativeUri(bindingFileUri).ToString().Replace('/', Path.DirectorySeparatorChar);
                                
                                // Log results of changing file binding
                                if (TryChangeValueForProperty(property, relativePath)) {
                                    Logger.Instance.LogInfo(
                                        $"Changed '{property}' from '{file}' to '{relativePath}' for song '{Title}' in file '{FilePath}'");
                                } else {
                                    Logger.Instance.LogError(
                                        $"Couldn't change property '{property}' from '{file}' to '{relativePath}' for song '{Title}' in file '{FilePath}'");
                                }
                            } else { // If there are no possible files for this property, then we should remove it
                                
                                // Log results of removing property from file
                                if (DeleteProperty(property)) {
                                    Logger.Instance.LogInfo($"Removed property '{property}' from song '{Title}' in file '{FilePath}'");
                                } else {
                                    Logger.Instance.LogError(
                                        $"Couldn't remove property '{property}' from song '{Title}' in file '{FilePath}'");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}