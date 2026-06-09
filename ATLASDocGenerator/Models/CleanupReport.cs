using System;
using System.Collections.Generic;

namespace ATLASDocGenerator.Models
{
    public class CleanupReport
    {
        public CleanupReport()
        {
            StartedAt = DateTime.Now;
            Errors = new List<string>();
            Warnings = new List<string>();
            ActionResultDetectionDetails = new List<string>();
            BulletListTransformationDetails = new List<string>();
            CalloutTransformationDetails = new List<string>();
        }

        public DateTime StartedAt { get; set; }

        public DateTime FinishedAt { get; set; }

        public string ScanRoot { get; set; }

        public int FilesScanned { get; set; }

        public int ActionResultListsTransformed { get; set; }

        public int BulletListsTransformed { get; set; }

        public int CalloutsTransformed { get; set; }

        public int FiguresTransformed { get; set; }

        public int StylesCleaned { get; set; }

        public int IhmItemsDetected { get; set; }

        public string LogFilePath { get; set; }

        public List<string> Errors { get; set; }

        public List<string> Warnings { get; set; }

        public int ActionNumParagraphsDetected { get; set; }

        public int ActionBulletParagraphsDetected { get; set; }

        public int ResultParagraphsDetected { get; set; }

        public List<string> ActionResultDetectionDetails { get; set; }

        public int BulletParagraphsDetected { get; set; }

        public int NoPageBreakBlocksCreated { get; set; }

        public List<string> BulletListTransformationDetails { get; set; }

        public List<string> CalloutTransformationDetails { get; set; }


    }
}