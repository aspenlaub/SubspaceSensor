using System.IO;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor.Test;

[TestClass]
public class SubspaceFolderTest {
    private readonly SubspaceFolderHelper _SubspaceFolderHelper;

    public SubspaceFolderTest() {
        IContainer container = new ContainerBuilder().UsePegh("SubspaceSensor").Build();
        IFolderResolver folderResolver = container.Resolve<IFolderResolver>();
        var subspaceTransmissionFactory = new SubspaceTransmissionFactory(folderResolver);
        _SubspaceFolderHelper = new SubspaceFolderHelper(folderResolver, subspaceTransmissionFactory);
    }

    [TestMethod]
    public async Task CanWorkWithSubspaceFolder() {
        foreach(SubspaceFolders subspaceFolder in new[] { SubspaceFolders.Error, SubspaceFolders.Inbox, SubspaceFolders.Port }) {
            await VerifyThatSubspaceFolderExistsAsync(subspaceFolder);
        }
    }

    private async Task VerifyThatSubspaceFolderExistsAsync(SubspaceFolders subspaceFolder) {
        string folder = await _SubspaceFolderHelper.FolderPathAsync(subspaceFolder);
        Assert.IsTrue(Directory.Exists(folder));
    }
}