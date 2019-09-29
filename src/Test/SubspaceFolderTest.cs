using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor.Test {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void CanWorkWithSubspaceFolder() {
            foreach(var subspaceFolder in new[] { SubspaceFolders.Error, SubspaceFolders.Inbox, SubspaceFolders.Port }) {
                VerifyThatSubspaceFolderExists(subspaceFolder);
            }
        }

        private static void VerifyThatSubspaceFolderExists(SubspaceFolders subspaceFolder) {
            var folder = SubspaceFolder.FolderPath(subspaceFolder);
            Assert.IsTrue(Directory.Exists(folder));
        }
    }
}
