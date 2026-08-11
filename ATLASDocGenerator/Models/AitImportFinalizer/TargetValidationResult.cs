using System.Collections.Generic;

namespace ATLASDocGenerator.Models.AitImportFinalizer
{
    public class TargetValidationResult
    {
        public TargetValidationResult()
        {
            Differences = new List<TargetConfigurationDifference>();
        }

        public bool IsValid
        {
            get { return Differences.Count == 0; }
        }

        public List<TargetConfigurationDifference> Differences { get; set; }
    }

    public class TargetConfigurationDifference
    {
        public string SettingName { get; set; }
        public string CurrentValue { get; set; }
        public string ExpectedValue { get; set; }
    }
}
