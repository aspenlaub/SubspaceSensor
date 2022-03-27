using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public class SubspaceTransmissionFactory {
        private readonly IFolderResolver FolderResolver;

        public SubspaceTransmissionFactory(IFolderResolver folderResolver) {
            FolderResolver = folderResolver;
        }

        public async Task<SubspaceTransmission> CreateAsync(SubspaceFolders folder, string messageId, DateTime created) {
            var transmission = await CreateAsync(folder, messageId);
            transmission.Created = created;
            return transmission;
        }

        public async Task<SubspaceTransmission> CreateAsync(SubspaceFolders folder, string messageId) {
            var transmission = new SubspaceTransmission(FolderResolver, this);
            await transmission.SetFolderAndMessageIdAsync(folder, messageId);
            return transmission;
        }
    }
}
