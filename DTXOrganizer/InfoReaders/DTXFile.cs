using System;
using System.Collections;
using System.Collections.Generic;

namespace DTXOrganizer {
    
    public class DTXFile : BaseFile, IComparable<DTXFile> {

        private static readonly string PROPERTY_ARTIST = "#ARTIST";
        private static readonly string PROPERTY_LEVEL = "#DLEVEL";
        private static readonly string PROPERTY_COMMENT = "#COMMENT";
        private static readonly string PROPERTY_PREVIEW = "#PREVIEW";
        private static readonly string PROPERTY_PREIMAGE = "#PREIMAGE";
        private static readonly string PROPERTY_RESULTIMAGE = "#RESULTIMAGE";
        private static readonly string PROPERTY_BPM = "#BPM";
        
        private static readonly string PROPERTY_AVI = "#AVI";
        private static readonly string PROPERTY_WAV = "#WAV";
        
        public string Artist { get; }
        public string Comment { get; }
        public string PreviewFile { get; }
        public string PreImageFile { get; }
        public string ResultImageFile { get; }
        public float Level { get; }
        public int Bpm { get; }

        public DTXFile(string path) : base(path) {
            if (!ProperlyInitialized) {
                return;
            }

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
            throw new NotImplementedException();
        }
    }
}