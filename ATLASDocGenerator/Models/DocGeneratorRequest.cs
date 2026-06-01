namespace ATLASDocGenerator.Models
{
    public class DocGenerationRequest
    {
        public string ProjectRoot { get; set; }
        public string DocumentType { get; set; }
        public string ShortTitle { get; set; }
        public string DocumentReference { get; set; }
        public string Device { get; set; }
        public string Range { get; set; }
        public string FullTitle { get; set; }
    }
}