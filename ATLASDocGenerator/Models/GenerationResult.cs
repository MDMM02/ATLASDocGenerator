using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{
    public class GenerationResult
    {
        public string FolderName { get; set; }
        public string DocumentFolderPath { get; set; }
        public string TocPath { get; set; }
        public string TargetPath { get; set; }

        public List<string> CreatedTopicPaths { get; set; }

        public GenerationResult()
        {
            CreatedTopicPaths = new List<string>();
        }
    }
}