using System;
using System.Collections.Generic;
using System.IO;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public enum SubspaceFolders {
        None, Port, Error, Inbox,
    }

    public abstract class SubspaceFolder {
        public static IFolder ConfiguredSubspaceFolder() {
            var componentProvider = new ComponentProvider();
            var resolver = componentProvider.FolderResolver;
            var errorsAndInfos = new ErrorsAndInfos();
            var subspaceFolder = resolver.Resolve(@"$(MainUserFolder)\Documents\Subspace", errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }

            if (!subspaceFolder.Exists()) {
                throw new Exception($"Folder does not exist: {subspaceFolder.FullName}");
            }

            return subspaceFolder;
        }

        public static string FolderPath(SubspaceFolders folder) {
            var path = ConfiguredSubspaceFolder().FullName + '\\';
            switch(folder) {
                case SubspaceFolders.Port : return path + @"07_Port\";
                case SubspaceFolders.Error : return path + @"19_Error\";
                case SubspaceFolders.Inbox : return path + @"24_Inbox\";
                default : throw new Exception("Asked for a folder browser that is not supported");
            }
        }

        public static List<SubspaceTransmission> ScanFolder(SubspaceFolders folder) {
            var path = FolderPath(folder);
            var dirInfo = new DirectoryInfo(path);
            var transmissions = new List<SubspaceTransmission>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(var fileInfo in dirInfo.GetFiles("subspacemsg*.xml")) {
                var s = fileInfo.Name;
                transmissions.Add(
                    new SubspaceTransmission {
                        MessageId = s.Substring(11, s.Length - 15),
                        Folder = folder,
                        Created = fileInfo.CreationTime});
            }

            transmissions.Sort();
            return transmissions;
        }
    }
}
