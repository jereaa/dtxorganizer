namespace DTXOrganizer {
    
    public class BoxFile : BaseFile {
        
        public string Artist { get; }
        public string Comment { get; }
        public string PreImage { get; }
        public string Preview { get; }
        public string FontColor { get; }

        public BoxFile(string path) : base(path) {
            if (!ProperlyInitialized) {
                return;
            }

            if (TryGetValueForProperty(PROPERTY_ARTIST, out string artist)) {
                Artist = artist;
            }

            if (TryGetValueForProperty(PROPERTY_COMMENT, out string comment)) {
                Comment = comment;
            }

            if (TryGetValueForProperty(PROPERTY_PREIMAGE, out string preImage)) {
                PreImage = preImage;
            }

            if (TryGetValueForProperty(PROPERTY_PREVIEW, out string preview)) {
                Preview = preview;
            }

            if (TryGetValueForProperty(PROPERTY_FONTCOLOR, out string fontColor)) {
                FontColor = fontColor;
            }
        }

        //TODO wipe this method and make all properties with public set
        public void SetTitle(string title) {
            TryChangeValueForProperty(PROPERTY_TITLE, title);
            SaveFile();
        }
        
        public override void FindProblems(bool autoFix) {
            throw new System.NotImplementedException();
        }
    }
}