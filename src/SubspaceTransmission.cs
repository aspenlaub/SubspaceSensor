using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor;

public class SubspaceTransmission : IComparable {
    public string MessageId { get; private set; }
    public SubspaceFolders Folder { get; private set; }

    public DateTime Created { get; set; }

    public string From { get; private set; }
    public string To { get; private set; }
    public string Cc { get; private set; }
    public string Bcc { get; private set; }
    public string Header { get; private set; }
    public string Text { get; private set; }

    public bool Valid { get; private set; }

    public bool IsPseudo => MessageId.Length == 0 || MessageId[0] == '(';

    public string Description {
        get {
            string s;

            if (Valid) {
                s = Header;
                var pos = 0;
                while (s.Length > 24 && pos >= 0) {
                    pos = s.LastIndexOf(' ');
                    if (pos >= 0) {
                        s = s.Substring(0, pos);
                    }
                }

                s = MessageId + ' ' + s;
            } else {
                s = MessageId;
            }

            return s;
        }
    }

    private readonly IFolderResolver FolderResolver;
    private readonly SubspaceTransmissionFactory SubspaceTransmissionFactory;

    public SubspaceTransmission(IFolderResolver folderResolver, SubspaceTransmissionFactory subspaceTransmissionFactory) {
        MessageId = "";
        Folder = SubspaceFolders.None;
        FolderResolver = folderResolver;
        SubspaceTransmissionFactory = subspaceTransmissionFactory;
        Invalidate();
    }

    public int CompareTo(object o) {
        var transmission = (SubspaceTransmission)o;

        if (Created < transmission.Created) {
            return 1;
        }

        if (Created > transmission.Created) {
            return -1;
        }

        return String.CompareOrdinal(transmission.MessageId, MessageId);
    }

    private void Invalidate() {
        From = ""; To = ""; Cc = ""; Bcc = ""; Header = ""; Text = "";    Valid = false;
    }

    public string FileName => "subspacemsg" + MessageId + ".xml";

    public async Task<string> FullFileNameAsync() {
        return await new SubspaceFolder(FolderResolver, SubspaceTransmissionFactory).FolderPathAsync(Folder) + FileName;
    }

    private async Task TryReadingAsync() {
        if (MessageId.Length == 0 || Folder == SubspaceFolders.None) {
            Invalidate();
            return;
        }

        var fileName = await FullFileNameAsync();
        if (!File.Exists(fileName)) {
            Invalidate();
            return;
        }

        Created = File.GetCreationTime(fileName);

        try {
            var textReader = new XmlTextReader(fileName);
            textReader.Read();
            while (textReader.Read()) {
                textReader.MoveToElement();
                var nodeType = textReader.NodeType;

                switch (nodeType) {
                    case XmlNodeType.Element when textReader.AttributeCount >= 8: {
                        if (From.Length > 0) {
                            Invalidate();
                            return;
                        }

                        From = textReader.GetAttribute("from");
                        To = textReader.GetAttribute("to");
                        Cc = textReader.GetAttribute("cc");
                        Bcc = textReader.GetAttribute("bcc");
                        Header = textReader.GetAttribute("header");
                        break;
                    }
                    case XmlNodeType.Text when Text.Length > 0:
                        continue;
                    case XmlNodeType.Text:
                        Text = textReader.Value;
                        break;
                }
            }

            textReader.Close();
            Valid = From.Length > 0 && To.Length > 0 && Header.Length > 0 && MessageId.IndexOf("http://www.", StringComparison.InvariantCulture) < 0;
        } catch {
            Invalidate();
        }
    }

    public async Task SetFolderAndMessageIdAsync(SubspaceFolders folder, string messageId) {
        if (MessageId.Length > 0) {
            throw new Exception("Attempt to overwrite message ID");
        }
        if (Folder != SubspaceFolders.None) {
            throw new Exception("Attempt to overwrite folder");
        }


        Folder = folder;
        MessageId = messageId;

        await TryReadingAsync();
    }
}