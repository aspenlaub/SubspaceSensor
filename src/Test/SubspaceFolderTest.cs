using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor.Test {
    [TestClass]
    public class SubspaceFolderTest {
        private readonly IContainer vContainer;

        public SubspaceFolderTest() {
            vContainer = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
        }

        [TestMethod]
        public void CanWorkWithSubspaceFolder() {
            foreach(var subspaceFolder in new[] { SubspaceFolders.Error, SubspaceFolders.Inbox, SubspaceFolders.Port }) {
                VerifyThatSubspaceFolderExists(subspaceFolder);
            }
        }

        private void VerifyThatSubspaceFolderExists(SubspaceFolders subspaceFolder) {
            var folder = new SubspaceFolder(vContainer.Resolve<IFolderResolver>()).FolderPath(subspaceFolder);
            Assert.IsTrue(Directory.Exists(folder));
        }
    }
}
