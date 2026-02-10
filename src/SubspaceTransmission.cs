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

    public string From { get; private set; } = "";
    public string To { get; private set; } = "";
    public string Cc { get; private set; } = "";
    public string Bcc { get; private set; } = "";
    public string Header { get; private set; } = "";
    public string Text { get; private set; } = "";

    public bool Valid { get; private set; }

    public bool IsPseudo => MessageId.Length == 0 || MessageId[0] == '(';

    public string Description {
        get {
            string s;

            if (Valid) {
                s = Header;
                int pos = 0;
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

    private readonly IFolderResolver _FolderResolver;
    private readonly SubspaceTransmissionFactory _SubspaceTransmissionFactory;

    public SubspaceTransmission(IFolderResolver folderResolver, SubspaceTransmissionFactory subspaceTransmissionFactory) {
        MessageId = "";
        Folder = SubspaceFolders.None;
        _FolderResolver = folderResolver;
        _SubspaceTransmissionFactory = subspaceTransmissionFactory;
        Invalidate();
    }

    public int CompareTo(object o) {
        var transmission = (SubspaceTransmission)o;
        return transmission == null
            ? throw new Exception(nameof(transmission))
            : Created < transmission.Created
                ? 1
                : Created > transmission.Created
                    ? -1
                    : string.CompareOrdinal(transmission.MessageId, MessageId);
    }

    private void Invalidate() {
        From = ""; To = ""; Cc = ""; Bcc = ""; Header = ""; Text = "";    Valid = false;
    }

    public string FileName => "subspacemsg" + MessageId + ".xml";

    public async Task<string> FullFileNameAsync() {
        return await new SubspaceFolder(_FolderResolver, _SubspaceTransmissionFactory).FolderPathAsync(Folder) + FileName;
    }

    private async Task TryReadingAsync() {
        if (MessageId.Length == 0 || Folder == SubspaceFolders.None) {
            Invalidate();
            return;
        }

        string fileName = await FullFileNameAsync();
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
                XmlNodeType nodeType = textReader.NodeType;

                switch (nodeType) {
                    case XmlNodeType.Element when textReader.AttributeCount >= 8: {
                        if (From.Length > 0) {
                            Invalidate();
                            return;
                        }

                        From = textReader.GetAttribute("from") ?? throw new Exception("from");
                        To = textReader.GetAttribute("to") ?? throw new Exception("to");
                        Cc = textReader.GetAttribute("cc") ?? throw new Exception("cc");
                        Bcc = textReader.GetAttribute("bcc") ?? throw new Exception("bcc");
                        Header = textReader.GetAttribute("header") ?? throw new Exception("header");
                        break;
                    }
                    case XmlNodeType.Text when Text.Length > 0:
                        continue;
                    case XmlNodeType.Text:
                        Text = textReader.Value;
                        break;
                    case XmlNodeType.None:
                        break;
                    case XmlNodeType.Attribute:
                        break;
                    case XmlNodeType.CDATA:
                        break;
                    case XmlNodeType.EntityReference:
                        break;
                    case XmlNodeType.Entity:
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        break;
                    case XmlNodeType.Comment:
                        break;
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.DocumentFragment:
                        break;
                    case XmlNodeType.Notation:
                        break;
                    case XmlNodeType.Whitespace:
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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