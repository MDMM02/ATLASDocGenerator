namespace ATLASDocGenerator.Models.AitImportFinalizer
{
    /// <summary>
    /// Liste les profils documentaires disponbiles pour la finalisation d'un import AIT
    /// 
    /// Le type sélectionné permet de charger les bons paramètres (CSS,layout, entrées de TOC...)
    /// </summary>
    public enum AitDocumentType
    {
        TechnicalBulletin,
        UserNotice,
        Addenda,
        ReferenceManual,
        TechnicalDocument,
        MultiInstrumentTechnicalDocument
    }
}