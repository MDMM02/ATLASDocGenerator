using ATLASDocGenerator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class FileNameSanitizerTests
    {
        [TestMethod]
        [DataRow("Réglages système", "Reglages_systeme")]
        [DataRow("BT-001 (Révision A)", "BT-001_Revision_A")]
        [DataRow("Nom_de-fichier", "Nom_de-fichier")]
        [DataRow("  ", "")]
        [DataRow(null, "")]
        public void ToSafeName_AppliesAtlasNamingRules(string input, string expected)
        {
            string actual = FileNameSanitizer.ToSafeName(input);

            Assert.AreEqual(expected, actual);
        }
    }
}
