namespace ATLASDocGenerator.Models
{
    /// <summary>
    /// Définit une règle de copie pour un topic template.
    /// 
    /// Une règle indique:
    ///     - quel fichier source doit être copié depuis les templates du plugin
    ///     - sous quel nom le fichier doit être créé dans le projet Flare
    /// </summary>
    public class TopicCopyRule
    {
        public string SourceRelativePath { get; set; }
        public string TargetFileNamePattern { get; set; }
    }
}