using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public enum SubspaceAppCmdType {
        None,
        Initialise,         // Initialise everything
        Scan,               // Scan Port (vFolder) for new messages
        Scanned,            // Message (vMessageId) found at Port (vFolder)
        Received,           // Message (vMessageId) moved from Port (vFolder) to Inbox (vToFolder)
        ReceivedError,      // Message (vMessageId) moved from Port (vFolder) to Error (vToFolder)
        Delete,             // Delete message (vMessageId) from (vFolder)
        DeleteAll,          // Delete all messages from Inbox
        MessageSelected     // Message (vMessageId) in folder (vFolder) has been selected
    }

    public class SubspaceAppCmd {
        private SubspaceAppCmdType vCmdType;
        public SubspaceAppCmdType CmdType {
            get => vCmdType;
            set {
                var cmdType = value;

                if (vCmdType == cmdType) {
                    return;
                }

                if (vCmdType != SubspaceAppCmdType.None) {
                    throw new Exception("Attempt to update subspace application command type.");
                }

                switch(cmdType) {
                    case SubspaceAppCmdType.Scan :
                    case SubspaceAppCmdType.Scanned : {
                        vFolder = SubspaceFolders.Port;
                    } break;
                    case SubspaceAppCmdType.Received : {
                        vFolder = SubspaceFolders.Port;
                        ToFolder = SubspaceFolders.Inbox;
                    } break;
                    case SubspaceAppCmdType.ReceivedError : {
                        vFolder = SubspaceFolders.Port;
                        ToFolder = SubspaceFolders.Error;
                    } break;
                    case SubspaceAppCmdType.DeleteAll : {
                        vFolder = SubspaceFolders.Inbox;
                    } break;
                    case SubspaceAppCmdType.Initialise : {
                    } break;
                }

                vCmdType = cmdType;
            }
        }

        private SubspaceFolders vFolder;
        public SubspaceFolders Folder {
            get => vFolder;
            set {
                var folder = value;

                if (vFolder == folder) {
                    return;
                }

                if (vFolder != SubspaceFolders.None) {
                    throw new Exception("Attempt to update subspace application command folder.");
                }
                if (vCmdType != SubspaceAppCmdType.Delete && vCmdType != SubspaceAppCmdType.MessageSelected)  {
                    throw new Exception("Attempt to update subspace application command folder without appropriate command type.");
                }

                vFolder = folder;
            }
        }

        private string vMessageId;
        public string MessageId {
            get => vMessageId;
            set {
                var messageId = value;

                if (vMessageId == messageId) {
                    return;
                }

                if (vMessageId != "") {
                    throw new Exception("Attempt to update subspace application command message ID.");
                }
                if (vCmdType == SubspaceAppCmdType.Scan || vCmdType == SubspaceAppCmdType.Initialise) {
                    throw new Exception("Attempt to update subspace application command message ID without appropriate command.");
                }
                if (vFolder == SubspaceFolders.None) {
                    throw new Exception("Attempt to update subspace application command message ID without command folder.");
                }

                vMessageId = messageId;
            }
        }

        public SubspaceFolders ToFolder { get; private set; }

        private readonly IFolderResolver vFolderResolver;

        public SubspaceAppCmd(IFolderResolver folderResolver) {
            vCmdType = SubspaceAppCmdType.None;
            vFolder = SubspaceFolders.None;
            ToFolder = SubspaceFolders.None;
            vMessageId = "";
            vFolderResolver = folderResolver;
        }

        private async Task InitialiseAsync(SubspaceStation station) {
            foreach (var folder in Enum.GetValues(typeof(SubspaceFolders)).Cast<SubspaceFolders>().Where(folder => folder != SubspaceFolders.None)) {
                var folderBrowser = station.FolderBrowser(folder);
                folderBrowser.Initialise(await new SubspaceFolder(vFolderResolver).ScanFolderAsync(folder));
            }
        }

        private async Task ScanAsync(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            var folderBrowser = station.FolderBrowser(vFolder);
            if (folderBrowser.IsEmpty) {
                folderBrowser.Initialise(await new SubspaceFolder(vFolderResolver).ScanFolderAsync(vFolder));
            }
            if (folderBrowser.IsEmpty) {
                return;
            }

            var transmission = folderBrowser.Pop(followCommands);
            if (transmission.IsPseudo) {
                return;
            }

            var newFolder = transmission.Valid ? SubspaceFolders.Inbox : SubspaceFolders.Error;
            File.Move(transmission.FullFileName, await new SubspaceFolder(vFolderResolver).FolderPathAsync(newFolder) + transmission.FileName);
            followCommands.Add(new SubspaceAppCmd(vFolderResolver) { CmdType = SubspaceAppCmdType.Scanned, MessageId = transmission.MessageId });
            followCommands.Add(transmission.Valid
                ? new SubspaceAppCmd(vFolderResolver) { CmdType = SubspaceAppCmdType.Received, MessageId = transmission.MessageId}
                : new SubspaceAppCmd(vFolderResolver) { CmdType = SubspaceAppCmdType.ReceivedError, MessageId = transmission.MessageId});
            followCommands.Add(new SubspaceAppCmd(vFolderResolver) { CmdType = SubspaceAppCmdType.Scan });
        }

        private void Scanned(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            station.FolderBrowser(vFolder).MessageGone(vMessageId, followCommands);
        }

        private void Received(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            station.FolderBrowser(ToFolder).NewMessage(vMessageId, followCommands);
        }

        private void ReceivedError(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            station.FolderBrowser(ToFolder).NewMessage(vMessageId, followCommands);
        }

        private void Delete(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            var folderBrowser = station.FolderBrowser(vFolder);
            if (folderBrowser.IsEmpty) {
                return;
            }

            var transmission = new SubspaceTransmission(vFolderResolver) { Folder = vFolder, MessageId = vMessageId };
            if (transmission.IsPseudo) {
                return;
            }

            File.Delete(transmission.FullFileName);
            folderBrowser.MessageGone(vMessageId, followCommands);
        }

        private void DeleteAll(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            var folderBrowser = station.FolderBrowser(vFolder);
            do {
                var transmission = folderBrowser.Pop(followCommands);
                // ReSharper disable once UseNullPropagationWhenPossible
                if (transmission == null) { return; }
                if (!transmission.Valid) { return; }

                File.Delete(transmission.FullFileName);
                folderBrowser.MessageGone(transmission.MessageId, followCommands);
            } while (true);
        }

        private void MessageSelected(SubspaceStation station) {
            var transmission = new SubspaceTransmission(vFolderResolver) { Folder = vFolder, MessageId = vMessageId };
            station.SetTransmission(transmission);
        }


        public async Task ExecuteAsync(SubspaceStation station, List<SubspaceAppCmd> followCommands) {
            followCommands.Clear();

            switch(vCmdType) {
                case SubspaceAppCmdType.None : {
                } break;
                case SubspaceAppCmdType.Initialise : {
                    await InitialiseAsync(station);
                } break;
                case SubspaceAppCmdType.Scan : {
                    await ScanAsync(station, followCommands);
                } break;
                case SubspaceAppCmdType.Scanned : {
                    Scanned(station, followCommands);
                } break;
                case SubspaceAppCmdType.Received : {
                    Received(station, followCommands);
                } break;
                case SubspaceAppCmdType.ReceivedError : {
                    ReceivedError(station, followCommands);
                } break;
                case SubspaceAppCmdType.Delete : {
                    Delete(station, followCommands);
                } break;
                case SubspaceAppCmdType.DeleteAll : {
                    DeleteAll(station, followCommands);
                } break;
                case SubspaceAppCmdType.MessageSelected : {
                    MessageSelected(station);
                } break;
            }
        }
    }
}
