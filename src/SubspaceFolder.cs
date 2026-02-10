using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor;

public enum SubspaceFolders {
    None, Port, Error, Inbox,
}

public class SubspaceFolder(IFolderResolver folderResolver, SubspaceTransmissionFactory subspaceTransmissionFactory) {
    public async Task<IFolder> ConfiguredSubspaceFolderAsync() {
        var errorsAndInfos = new ErrorsAndInfos();
        IFolder subspaceFolder = await folderResolver.ResolveAsync(@"$(MainUserFolder)\Documents\Subspace", errorsAndInfos);
        return errorsAndInfos.AnyErrors()
            ? throw new Exception(errorsAndInfos.ErrorsToString())
            : !subspaceFolder.Exists()
                ? throw new Exception($"Folder does not exist: {subspaceFolder.FullName}")
                : subspaceFolder;
    }

    public async Task<string> FolderPathAsync(SubspaceFolders folder) {
        string path = (await ConfiguredSubspaceFolderAsync()).FullName + '\\';
        switch(folder) {
            case SubspaceFolders.Port : return path + @"07_Port\";
            case SubspaceFolders.Error : return path + @"19_Error\";
            case SubspaceFolders.Inbox : return path + @"24_Inbox\";
            case SubspaceFolders.None:
            default : throw new Exception("Asked for a folder browser that is not supported");
        }
    }

    public async Task<List<SubspaceTransmission>> ScanFolderAsync(SubspaceFolders folder) {
        string path = await FolderPathAsync(folder);
        var dirInfo = new DirectoryInfo(path);
        var transmissions = new List<SubspaceTransmission>();
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach(FileInfo fileInfo in dirInfo.GetFiles("subspacemsg*.xml")) {
            string s = fileInfo.Name;
            transmissions.Add(
                await subspaceTransmissionFactory.CreateAsync(folder, s.Substring(11, s.Length - 15), fileInfo.CreationTime)
            );
        }

        transmissions.Sort();
        return transmissions;
    }
}