using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public enum SubspaceFolders {
        None, Port, Error, Inbox,
    }

    public class SubspaceFolder {
        private readonly IFolderResolver vFolderResolver;

        public SubspaceFolder(IFolderResolver folderResolver) {
            vFolderResolver = folderResolver;
        }

        public async Task<IFolder> ConfiguredSubspaceFolderAsync() {
            var errorsAndInfos = new ErrorsAndInfos();
            var subspaceFolder = await vFolderResolver.ResolveAsync(@"$(MainUserFolder)\Documents\Subspace", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }

            if (!subspaceFolder.Exists()) {
                throw new Exception($"Folder does not exist: {subspaceFolder.FullName}");
            }

            return subspaceFolder;
        }

        public async Task<string> FolderPathAsync(SubspaceFolders folder) {
            var path = (await ConfiguredSubspaceFolderAsync()).FullName + '\\';
            switch(folder) {
                case SubspaceFolders.Port : return path + @"07_Port\";
                case SubspaceFolders.Error : return path + @"19_Error\";
                case SubspaceFolders.Inbox : return path + @"24_Inbox\";
                default : throw new Exception("Asked for a folder browser that is not supported");
            }
        }

        public async Task<List<SubspaceTransmission>> ScanFolderAsync(SubspaceFolders folder) {
            var path = await FolderPathAsync(folder);
            var dirInfo = new DirectoryInfo(path);
            var transmissions = new List<SubspaceTransmission>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(var fileInfo in dirInfo.GetFiles("subspacemsg*.xml")) {
                var s = fileInfo.Name;
                transmissions.Add(
                    new SubspaceTransmission(vFolderResolver) {
                        MessageId = s.Substring(11, s.Length - 15),
                        Folder = folder,
                        Created = fileInfo.CreationTime});
            }

            transmissions.Sort();
            return transmissions;
        }
    }
}
