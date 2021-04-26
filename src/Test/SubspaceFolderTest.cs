using System.IO;
using System.Threading.Tasks;
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
        public async Task CanWorkWithSubspaceFolder() {
            foreach(var subspaceFolder in new[] { SubspaceFolders.Error, SubspaceFolders.Inbox, SubspaceFolders.Port }) {
                await VerifyThatSubspaceFolderExistsAsync(subspaceFolder);
            }
        }

        private async Task VerifyThatSubspaceFolderExistsAsync(SubspaceFolders subspaceFolder) {
            var folder = await new SubspaceFolder(vContainer.Resolve<IFolderResolver>()).FolderPathAsync(subspaceFolder);
            Assert.IsTrue(Directory.Exists(folder));
        }
    }
}
