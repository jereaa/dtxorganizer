using System;
using System.IO;
using System.Text;

namespace DTXOrganizer {
    
    public abstract class BaseFile {
        
        protected static readonly string TITLE_PROPERTY = "#TITLE";

        public bool ProperlyInitialized { get; protected set; }
        
        public string FilePath { get; private set; }
        protected string rawValue;
        
        public string Title { get; protected set; }

        /// <summary>
        /// Create file instance from real file
        /// </summary>
        /// <param name="path">Where file is located in the system</param>
        protected BaseFile(string path) {
            FilePath = path;
            ProperlyInitialized = false;

            try {
                rawValue = File.ReadAllText(path, Encoding.GetEncoding("shift_jis"));

                if (!rawValue.Contains(TITLE_PROPERTY)) {
                    rawValue = File.ReadAllText(path, Encoding.Unicode);

                    if (!rawValue.Contains(TITLE_PROPERTY)) {
                        Logger.Instance.LogWarning("Couldn't read TITLE property in file '" + Path.GetFileName(path) + "'");
                        return;
                    }
                }

                ProperlyInitialized = true;
                TryGetValueForProperty(TITLE_PROPERTY, out string title);
                Title = title.Trim();

            } catch (Exception e) {
                if (e is FileNotFoundException) {
                    Logger.Instance.LogError("Couldn't find file '" + Path.GetFileName(path) + "' in '" + Path.GetDirectoryName(path) + "'.");
                } else {
                    Logger.Instance.LogError("Couldn't open file '" + path + "'. Error was:\n" + e + "\n");
                }
            }
        }

        /// <summary>
        /// Creates new file
        /// </summary>
        /// <param name="title">Title property's value</param>
        /// <param name="path">Where file will be created. Must include filename</param>
        protected BaseFile(string title, string path) {
            FilePath = path;
            Title = title;
            ProperlyInitialized = false;
            rawValue = "; File created with Jere's DTXOrganizer\r\n\r\n" +
                       TITLE_PROPERTY + ": " + title + "\r\n";

            try {
                File.WriteAllText(path, rawValue, Encoding.GetEncoding("shift_jis"));
                ProperlyInitialized = true;
            } catch (Exception e) {
                Logger.Instance.LogError("Couldn't create file for song '" + title + "' in '" + path + "'");
                Logger.Instance.LogError(e.ToString());
            }
        }

        public virtual bool RenameSongFolderToTitle() {
            if (!ProperlyInitialized) {
                return false;
            }

            string newDirName = Title;
            
            char[] invalidChars = Path.GetInvalidFileNameChars();
            int indexOfInvalidChars = newDirName.IndexOfAny(invalidChars);
            while (indexOfInvalidChars != -1) {
                newDirName = newDirName.Remove(indexOfInvalidChars, 1);
                indexOfInvalidChars = newDirName.IndexOfAny(invalidChars);
            }

            newDirName = newDirName.TrimEnd('.');
            
            // If names are already the same, then bail
            if (Path.GetFileName(Path.GetDirectoryName(FilePath)).Equals(newDirName)) {
                return true;
            }
            
            string oldDirectoryName = Path.GetDirectoryName(FilePath);
            string newDirectoryName =
                Path.Combine(new[] {Path.GetDirectoryName(Path.GetDirectoryName(FilePath)), newDirName});
            
            if (oldDirectoryName == null) {
                Logger.Instance.LogError("Couldn't get '" + Title + "' directory to rename it. Path: '" + FilePath +
                                         "'.");
                return false;
            }
            
            Directory.Move(oldDirectoryName, newDirectoryName);
            FilePath = Path.Combine(new[] {newDirectoryName, Path.GetFileName(FilePath)});

            Logger.Instance.LogInfo("Renamed '" + Title + "' - Path: '" + oldDirectoryName + "' -> '" +
                                     newDirectoryName + "'.");
            
            return true;
        }

        protected bool TryGetValueForProperty(string property, out string value) {
            int valueStartIndex = GetValueIndexForProperty(property);
            if (valueStartIndex == -1) {
                value = "";
                return false;
            }

            int endIndex = rawValue.IndexOfAny(new []{'\r', '\n'}, valueStartIndex);
            value = rawValue.Substring(valueStartIndex, endIndex - valueStartIndex);
            return true;
        }

        protected bool TryChangeValueForProperty(string property, string newValue) {
            int valueStartIndex = GetValueIndexForProperty(property);
            if (valueStartIndex == -1) {
                return false;
            }

            int endIndex = rawValue.IndexOfAny(new []{'\r', '\n'}, valueStartIndex);

            rawValue = rawValue.Remove(valueStartIndex, endIndex - valueStartIndex);
            rawValue = rawValue.Insert(valueStartIndex, newValue);
            return true;
        }

        private int GetValueIndexForProperty(string property) {
            int propertyIndex = rawValue.IndexOf(property, StringComparison.Ordinal);

            if (propertyIndex == -1) {
                return -1;
            }

            CharEnumerator it = rawValue.GetEnumerator();
            int counter = 0;
            while (counter <= propertyIndex + property.Length) {
                it.MoveNext();
                counter++;
            }
            while (it.Current == ' ' || it.Current == ':') {
                it.MoveNext();
                counter++;
            }

            counter--;
            it.Dispose();
            return counter;
        }

        protected void SaveFile() {
            try {
                File.WriteAllText(FilePath, rawValue, Encoding.GetEncoding("shift_jis"));
            } catch (Exception e) {
                Logger.Instance.LogError("Couldn't create file for song '" + Title + "' in '" + FilePath + "'");
                Logger.Instance.LogError(e.ToString());
            }
        }

    }
}