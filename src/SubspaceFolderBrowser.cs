using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor;

public class SubspaceFolderBrowser : Expander {
    private SubspaceFolders Folder;

    public string SubFolder {
        get => Enum.GetName(typeof(SubspaceFolders), Folder);
        set {
            if (Folder != SubspaceFolders.None) {
                throw new Exception("Attempt to update sub folder");
            }

            ChangeSubFolder(value);
        }
    }

    private readonly ScrollViewer ScrollViewer;
    private readonly ListBox ListBox;
    private readonly List<SubspaceTransmission> Transmissions;

    private readonly IFolderResolver FolderResolver = new ContainerBuilder().UsePegh("SubspaceSensor", new DummyCsArgumentPrompter()).Build().Resolve<IFolderResolver>();
    private readonly SubspaceTransmissionFactory SubspaceTransmissionFactory;

    public bool IsEmpty => Transmissions.Count == 0;

    private async Task DefaultTransmissionAsync() {
        Transmissions.Add(await SubspaceTransmissionFactory.CreateAsync(SubspaceFolders.None, "(no messages)"));
    }

    public SubspaceFolderBrowser() {
        Margin = new Thickness(0, 3, 0, 3);

        SubspaceTransmissionFactory = new SubspaceTransmissionFactory(FolderResolver);

        Transmissions = new List<SubspaceTransmission>();

        ListBox = new ListBox {
            SelectionMode = SelectionMode.Single,
            ItemsSource = Transmissions,
            DisplayMemberPath = nameof(SubspaceTransmission.Description)
        };
        ListBox.SelectionChanged += OnSelectionChanged;
        ListBox.GotFocus += OnFocus;

        ScrollViewer = new ScrollViewer {
            Content = ListBox,
            Margin = new Thickness(0, 8, 0, 0)
        };

        Content = ScrollViewer;
    }

    private string ExpanderHeader() {
        string s;

        switch(Folder) {
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

        if (Transmissions.Count > 0 && !Transmissions[0].IsPseudo) {
            s = s + " (" + Transmissions.Count + ')';
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
        if (Folder == SubspaceFolders.Inbox) {
            height = height * 2;
        }
        height = height + 2;
        return height;
    }

    private void ChangeSubFolder(string newSubFolder) {
        try {
            Folder = (SubspaceFolders)Enum.Parse(typeof(SubspaceFolders), newSubFolder);
        } catch {
            Folder = SubspaceFolders.None;
        }
        Header = ExpanderHeader();
        ScrollViewer.MaxHeight = MaxListBoxHeight();
    }

    public async Task InitialiseAsync(List<SubspaceTransmission> transmissions) {
        while (Transmissions.Count > transmissions.Count) {
            Transmissions.RemoveAt(Transmissions.Count - 1);
        }

        if (transmissions.Count > 0) {
            int i;
            for (i = 0; i < Transmissions.Count; i ++) {
                if (Transmissions[i].MessageId != transmissions[i].MessageId) {
                    Transmissions[i] = transmissions[i];
                }
            }
            for (i = Transmissions.Count; i < transmissions.Count; i ++) {
                Transmissions.Add(transmissions[i]);
            }
        } else {
            await DefaultTransmissionAsync();
        }

        ListBox.ItemsSource = null;
        ListBox.ItemsSource = Transmissions;
        Header = ExpanderHeader();
    }

    private void ShowSelection() {
        var selected = (SubspaceTransmission)ListBox.SelectedItem;
        if (selected?.IsPseudo != false) {
            return;
        }

        var cmd = new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = selected.Folder, MessageId = selected.MessageId };
        ((SubspaceStationApp)Application.Current).AddCommand(cmd);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        ShowSelection();
    }

    private void OnFocus(object sender, RoutedEventArgs e) {
        ShowSelection();
    }

    public async Task<SubspaceTransmission> PopAsync(List<SubspaceAppCmd> followCommands) {
        if (IsEmpty) {
            return null;
        }

        var transmission = Transmissions[0];
        await MessageGoneAsync(transmission.MessageId, followCommands);
        return transmission;
    }

    public async Task MessageGoneAsync(string messageId, List<SubspaceAppCmd> followCommands) {
        int i;

        for (i = 0; i < Transmissions.Count && Transmissions[i].MessageId != messageId; i ++) {
        }

        if (i >= Transmissions.Count) {
            return;
        }

        var selected = ListBox.SelectedIndex == i;
        if (selected) {
            if (i + 1 < Transmissions.Count) {
                followCommands.Add(new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = Transmissions[i + 1].Folder, MessageId = Transmissions[i + 1].MessageId });
            } else if (i > 0) {
                followCommands.Add(new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = Transmissions[i - 1].Folder, MessageId = Transmissions[i - 1].MessageId });
            }
        }
        Transmissions.RemoveAt(i);
        if (Transmissions.Count == 0) {
            await DefaultTransmissionAsync();
            if (selected) {
                followCommands.Add(new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = Folder });
            }
        }
        ListBox.ItemsSource = null;
        ListBox.ItemsSource = Transmissions;
        Header = ExpanderHeader();
    }

    public async Task NewMessageAsync(string messageId, List<SubspaceAppCmd> followCommands) {
        int i;

        for (i = 0; i < Transmissions.Count && Transmissions[i].MessageId != messageId; i ++) {
        }

        if (i < Transmissions.Count) {
            return;
        }

        while (Transmissions.Count > 0 && Transmissions[0].IsPseudo) {
            Transmissions.RemoveAt(0);
        }

        Transmissions.Add(await SubspaceTransmissionFactory.CreateAsync(Folder, messageId));
        Transmissions.Sort();
        ListBox.ItemsSource = null;
        ListBox.ItemsSource = Transmissions;
        Header = ExpanderHeader();
    }

    public void SelectTransmission(SubspaceTransmission transmission) {
        int i;

        for (i = 0; i < Transmissions.Count && Transmissions[i].MessageId != transmission.MessageId; i ++) {
        }

        if (i >= Transmissions.Count || ListBox.SelectedIndex == i) {
            return;
        }

        if (Transmissions[i].MessageId != ((SubspaceTransmission)ListBox.Items[i]).MessageId) {
            throw new Exception("Transmissions and items out of synchronization!");
        }

        ListBox.SelectedIndex = i;
        Focus();
    }
}