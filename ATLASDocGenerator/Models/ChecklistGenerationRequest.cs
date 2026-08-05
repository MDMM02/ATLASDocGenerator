namespace ATLASDocGenerator.Models
{
    public class ChecklistGenerationRequest
    {
        public string ProjectRoot { get; set; }
        public string SourceTargetPath { get; set; }
        public bool CreateNewDocument { get; set; }
        public string NewDocumentReference { get; set; }
    }

    public class ChecklistTargetInfo
    {
        public string TargetPath { get; set; }
        public string TocPath { get; set; }
        public string DisplayName { get; set; }
        public string DocumentReference { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class ChecklistGenerationResult
    {
        public int SectionCount { get; set; }
        public string ChecklistTopicPath { get; set; }
        public string TocPath { get; set; }
        public string TargetPath { get; set; }
    }
}
