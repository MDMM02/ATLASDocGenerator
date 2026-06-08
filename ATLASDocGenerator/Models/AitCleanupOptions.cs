namespace ATLASDocGenerator.Models
{
    public class AitCleanupOptions
    {
        public string DocumentationType { get; set; }

        public AitCleanupScope Scope { get; set; }

        public string TargetPath { get; set; }

        public bool ProcessActionResults { get; set; }

        public bool ProcessBulletLists { get; set; }

        public bool ProcessCallouts { get; set; }

        public bool ProcessFigures { get; set; }

        public bool ProcessStyleCleanup { get; set; }

        public bool ProcessIhm { get; set; }
    }
}