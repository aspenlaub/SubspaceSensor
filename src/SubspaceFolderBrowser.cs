using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public class SubspaceFolderBrowser : Expander {
        private SubspaceFolders vFolder;
        public string SubFolder {
            get => Enum.GetName(typeof(SubspaceFolders), vFolder);
            set {
                if (vFolder != SubspaceFolders.None) {
                    throw new Exception("Attempt to update sub folder");
                }

                ChangeSubFolder(value);
            }
        }

        private readonly ScrollViewer vScrollViewer;
        private readonly ListBox vListBox;
        private readonly List<SubspaceTransmission> vTransmissions;

        public bool IsEmpty => vTransmissions.Count == 0;

        private void DefaultTransmission() {
            vTransmissions.Add(new SubspaceTransmission { MessageId = "(no messages)" });
        }

        public SubspaceFolderBrowser() {
            Margin = new Thickness(0, 3, 0, 3);

            vTransmissions = new List<SubspaceTransmission>();
            DefaultTransmission();

            vListBox = new ListBox {
                SelectionMode = SelectionMode.Single,
                ItemsSource = vTransmissions,
                DisplayMemberPath = nameof(SubspaceTransmission.Description)
            };
            vListBox.SelectionChanged += OnSelectionChanged;
            vListBox.GotFocus += OnFocus;

            vScrollViewer = new ScrollViewer {
                Content = vListBox,
                Margin = new Thickness(0, 8, 0, 0)
            };

            Content = vScrollViewer;
        }

        private string ExpanderHeader() {
            string s;

            switch(vFolder) {
                case SubspaceFolders.Port : {
                    s = "Port";
                } break;
                case SubspaceFolders.Error : {
                    s = "Error";
                } break;
                case SubspaceFolders.Inbox : {
                    s = "Inbox";
                } break;
                default : {
                    s = "ERROR";
                } break;
            }

            if (vTransmissions.Count > 0 && !vTransmissions[0].IsPseudo) {
                s = s + " (" + vTransmissions.Count + ')';
            }
            return s;
        }

        private uint MaxListBoxHeight() {
            uint height;

            if (Application.Current.MainWindow == null) {
                throw new NullReferenceException(nameof(Application.Current.MainWindow));
            }

            try  {
                height = (uint)Application.Current.MainWindow.Height - 200;
                height = height / 15 / 5;
            } catch {
                height = 1;
            }
            height = 15 * height;
            if (vFolder == SubspaceFolders.Inbox) {
                height = height * 2;
            }
            height = height + 2;
            return height;
        }

        private void ChangeSubFolder(string newSubFolder) {
            try {
                vFolder = (SubspaceFolders)Enum.Parse(typeof(SubspaceFolders), newSubFolder);
            } catch {
                vFolder = SubspaceFolders.None;
            }
            Header = ExpanderHeader();
            vScrollViewer.MaxHeight = MaxListBoxHeight();
        }

        public void Initialise(List<SubspaceTransmission> transmissions) {
            while (vTransmissions.Count > transmissions.Count) {
                vTransmissions.RemoveAt(vTransmissions.Count - 1);
            }

            if (transmissions.Count > 0) {
                int i;
                for (i = 0; i < vTransmissions.Count; i ++) {
                    if (vTransmissions[i].MessageId != transmissions[i].MessageId) {
                        vTransmissions[i] = transmissions[i];
                    }
                }
                for (i = vTransmissions.Count; i < transmissions.Count; i ++) {
                    vTransmissions.Add(transmissions[i]);
                }
            } else {
                DefaultTransmission();
            }

            vListBox.ItemsSource = null;
            vListBox.ItemsSource = vTransmissions;
            Header = ExpanderHeader();
        }

        private void ShowSelection() {
            var selected = (SubspaceTransmission)vListBox.SelectedItem;
            if (selected?.IsPseudo != false) {
                return;
            }

            var cmd = new SubspaceAppCmd { CmdType = SubspaceAppCmdType.MessageSelected, Folder = selected.Folder, MessageId = selected.MessageId };
            ((SubspaceStationApp)Application.Current).AddCommand(cmd);
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            ShowSelection();
        }

        private void OnFocus(object sender, RoutedEventArgs e) {
            ShowSelection();
        }

        public SubspaceTransmission Pop(List<SubspaceAppCmd> followCommands) {
            if (IsEmpty) {
                return null;
            }

            var transmission = vTransmissions[0];
            MessageGone(transmission.MessageId, followCommands);
            return transmission;
        }

        public void MessageGone(string messageId, List<SubspaceAppCmd> followCommands) {
            int i;

            for (i = 0; i < vTransmissions.Count && vTransmissions[i].MessageId != messageId; i ++) {
            }

            if (i >= vTransmissions.Count) {
                return;
            }

            var selected = vListBox.SelectedIndex == i;
            if (selected) {
                if (i + 1 < vTransmissions.Count) {
                    followCommands.Add(new SubspaceAppCmd { CmdType = SubspaceAppCmdType.MessageSelected, Folder = vTransmissions[i + 1].Folder, MessageId = vTransmissions[i + 1].MessageId });
                } else if (i > 0) {
                    followCommands.Add(new SubspaceAppCmd { CmdType = SubspaceAppCmdType.MessageSelected, Folder = vTransmissions[i - 1].Folder, MessageId = vTransmissions[i - 1].MessageId });
                }
            }
            vTransmissions.RemoveAt(i);
            if (vTransmissions.Count == 0) {
                DefaultTransmission();
                if (selected) {
                    followCommands.Add(new SubspaceAppCmd { CmdType = SubspaceAppCmdType.MessageSelected, Folder = vFolder });
                }
            }
            vListBox.ItemsSource = null;
            vListBox.ItemsSource = vTransmissions;
            Header = ExpanderHeader();
        }

        public void NewMessage(string messageId, List<SubspaceAppCmd> followCommands) {
            int i;

            for (i = 0; i < vTransmissions.Count && vTransmissions[i].MessageId != messageId; i ++) {
            }

            if (i < vTransmissions.Count) {
                return;
            }

            while (vTransmissions.Count > 0 && vTransmissions[0].IsPseudo) {
                vTransmissions.RemoveAt(0);
            }

            vTransmissions.Add(new SubspaceTransmission { Folder = vFolder, MessageId = messageId });
            vTransmissions.Sort();
            vListBox.ItemsSource = null;
            vListBox.ItemsSource = vTransmissions;
            Header = ExpanderHeader();
        }

        public void SelectTransmission(SubspaceTransmission transmission) {
            int i;

            for (i = 0; i < vTransmissions.Count && vTransmissions[i].MessageId != transmission.MessageId; i ++) {
            }

            if (i >= vTransmissions.Count || vListBox.SelectedIndex == i) {
                return;
            }

            if (vTransmissions[i].MessageId != ((SubspaceTransmission)vListBox.Items[i]).MessageId) {
                throw new Exception("Transmissions and items out of synchronization!");
            }

            vListBox.SelectedIndex = i;
            Focus();
        }
    }
}
