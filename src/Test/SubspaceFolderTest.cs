using System.IO;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor.Test {
    [TestClass]
    public class SubspaceFolderTest {
        private readonly IFolderResolver FolderResolver;
        private readonly SubspaceTransmissionFactory SubspaceTransmissionFactory;

        public SubspaceFolderTest() {
            var container = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
            FolderResolver = container.Resolve<IFolderResolver>();
            SubspaceTransmissionFactory = new SubspaceTransmissionFactory(FolderResolver);
        }

        [TestMethod]
        public async Task CanWorkWithSubspaceFolder() {
            foreach(var subspaceFolder in new[] { SubspaceFolders.Error, SubspaceFolders.Inbox, SubspaceFolders.Port }) {
                await VerifyThatSubspaceFolderExistsAsync(subspaceFolder);
            }
        }

        private async Task VerifyThatSubspaceFolderExistsAsync(SubspaceFolders subspaceFolder) {
            var folder = await new SubspaceFolder(FolderResolver, SubspaceTransmissionFactory).FolderPathAsync(subspaceFolder);
            Assert.IsTrue(Directory.Exists(folder));
        }
    }
}
