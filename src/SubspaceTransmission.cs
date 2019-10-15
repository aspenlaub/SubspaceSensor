using System;
using System.IO;
using System.Xml;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public class SubspaceTransmission : IComparable {
        private string vMessageId;
        public string MessageId {
            get => vMessageId;
            set {
                if (vMessageId.Length > 0) {
                    throw new Exception("Attempt to overwrite message ID");
                }

                vMessageId = value;
                TryReading();
            }
        }

        private SubspaceFolders vFolder;
        public SubspaceFolders Folder {
            get => vFolder;
            set {
                if (vFolder != SubspaceFolders.None) {
                    throw new Exception("Attempt to overwrite folder");
                }

                vFolder = value;
                TryReading();
            }
        }

        public DateTime Created { get; set; }

        public string From { get; private set; }
        public string To { get; private set; }
        public string Cc { get; private set; }
        public string Bcc { get; private set; }
        public string Header { get; private set; }
        public string Text { get; private set; }

        public bool Valid { get; private set; }

        public bool IsPseudo => vMessageId.Length == 0 || vMessageId[0] == '(';

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

                    s = vMessageId + ' ' + s;
                } else {
                    s = vMessageId;
                }

                return s;
            }
        }

        private readonly IFolderResolver vFolderResolver;

        public SubspaceTransmission(IFolderResolver folderResolver) {
            vMessageId = "";
            vFolder = SubspaceFolders.None;
            vFolderResolver = folderResolver;
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

            return String.CompareOrdinal(transmission.vMessageId, vMessageId);
        }

        private void Invalidate() {
            From = ""; To = ""; Cc = ""; Bcc = ""; Header = ""; Text = "";    Valid = false;
        }

        public string FileName => "subspacemsg" + vMessageId + ".xml";

        public string FullFileName => new SubspaceFolder(vFolderResolver).FolderPath(vFolder) + FileName;

        private void TryReading() {
            if (vMessageId.Length == 0 || vFolder == SubspaceFolders.None) {
                Invalidate();
                return;
            }

            var fileName = FullFileName;
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
                Valid = From.Length > 0 && To.Length > 0 && Header.Length > 0 && vMessageId.IndexOf("http://www.", StringComparison.InvariantCulture) < 0;
            } catch {
                Invalidate();
            }
        }
    }
}
