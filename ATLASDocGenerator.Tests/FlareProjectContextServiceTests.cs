using System;
using System.IO;
using ATLASDocGenerator.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ATLASDocGenerator.Tests
{
    [TestClass]
    public class FlareProjectContextServiceTests
    {
        [TestMethod]
        public void ResolveProjectRootFromPath_ClimbsPastMissingStaleTopicFolder()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "ATLASDocGenerator.Tests",
                Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "Content"));
                Directory.CreateDirectory(Path.Combine(root, "Project"));
                File.WriteAllText(Path.Combine(root, "Test.flprj"), "<CatapultProject />");
                string staleTopic = Path.Combine(
                    root, "Content", "DeletedChecklist", "Checklist.htm");

                string resolved = FlareProjectContextService.ResolveProjectRootFromPath(staleTopic);

                Assert.AreEqual(root, resolved);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
    }
}
