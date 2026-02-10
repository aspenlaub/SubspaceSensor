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
    private SubspaceFolders _Folder;

    public string SubFolder {
        get { return Enum.GetName(typeof(SubspaceFolders), _Folder); }
        set {
            if (_Folder != SubspaceFolders.None) {
                throw new Exception("Attempt to update sub folder");
            }

            ChangeSubFolder(value);
        }
    }

    private readonly ScrollViewer _ScrollViewer;
    private readonly ListBox _ListBox;
    private readonly List<SubspaceTransmission> _Transmissions;

    private readonly IFolderResolver _FolderResolver = new ContainerBuilder().UsePegh("SubspaceSensor").Build().Resolve<IFolderResolver>();
    private readonly SubspaceTransmissionFactory _SubspaceTransmissionFactory;

    public bool IsEmpty => _Transmissions.Count == 0;

    private async Task DefaultTransmissionAsync() {
        _Transmissions.Add(await _SubspaceTransmissionFactory.CreateAsync(SubspaceFolders.None, "(no messages)"));
    }

    public SubspaceFolderBrowser() {
        Margin = new Thickness(0, 3, 0, 3);

        _SubspaceTransmissionFactory = new SubspaceTransmissionFactory(_FolderResolver);

        _Transmissions = new List<SubspaceTransmission>();

        _ListBox = new ListBox {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _Transmissions,
            DisplayMemberPath = nameof(SubspaceTransmission.Description)
        };
        _ListBox.SelectionChanged += OnSelectionChanged;
        _ListBox.GotFocus += OnFocus;

        _ScrollViewer = new ScrollViewer {
            Content = _ListBox,
            Margin = new Thickness(0, 8, 0, 0)
        };

        Content = _ScrollViewer;
    }

    private string ExpanderHeader() {
        string s;

        switch(_Folder) {
            case SubspaceFolders.Port : {
                s = "Port";
            } break;
            case SubspaceFolders.Error : {
                s = "Error";
            } break;
            case SubspaceFolders.Inbox : {
                s = "Inbox";
            } break;
            case SubspaceFolders.None:
            default : {
                s = "ERROR";
            } break;
        }

        if (_Transmissions.Count > 0 && !_Transmissions[0].IsPseudo) {
            s = s + " (" + _Transmissions.Count + ')';
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
        if (_Folder == SubspaceFolders.Inbox) {
            height *= 2;
        }
        height += 2;
        return height;
    }

    private void ChangeSubFolder(string newSubFolder) {
        try {
            _Folder = (SubspaceFolders)Enum.Parse(typeof(SubspaceFolders), newSubFolder);
        } catch {
            _Folder = SubspaceFolders.None;
        }
        Header = ExpanderHeader();
        _ScrollViewer.MaxHeight = MaxListBoxHeight();
    }

    public async Task InitialiseAsync(List<SubspaceTransmission> transmissions) {
        while (_Transmissions.Count > transmissions.Count) {
            _Transmissions.RemoveAt(_Transmissions.Count - 1);
        }

        if (transmissions.Count > 0) {
            int i;
            for (i = 0; i < _Transmissions.Count; i ++) {
                if (_Transmissions[i].MessageId != transmissions[i].MessageId) {
                    _Transmissions[i] = transmissions[i];
                }
            }
            for (i = _Transmissions.Count; i < transmissions.Count; i ++) {
                _Transmissions.Add(transmissions[i]);
            }
        } else {
            await DefaultTransmissionAsync();
        }

        _ListBox.ItemsSource = null;
        _ListBox.ItemsSource = _Transmissions;
        Header = ExpanderHeader();
    }

    private void ShowSelection() {
        var selected = (SubspaceTransmission)_ListBox.SelectedItem;
        if (selected?.IsPseudo != false) {
            return;
        }

        var cmd = new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = selected.Folder, MessageId = selected.MessageId };
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

        SubspaceTransmission transmission = _Transmissions[0];
        await MessageGoneAsync(transmission.MessageId, followCommands);
        return transmission;
    }

    public async Task MessageGoneAsync(string messageId, List<SubspaceAppCmd> followCommands) {
        int i;

        for (i = 0; i < _Transmissions.Count && _Transmissions[i].MessageId != messageId; i ++) {
        }

        if (i >= _Transmissions.Count) {
            return;
        }

        bool selected = _ListBox.SelectedIndex == i;
        if (selected) {
            if (i + 1 < _Transmissions.Count) {
                followCommands.Add(new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = _Transmissions[i + 1].Folder, MessageId = _Transmissions[i + 1].MessageId });
            } else if (i > 0) {
                followCommands.Add(new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = _Transmissions[i - 1].Folder, MessageId = _Transmissions[i - 1].MessageId });
            }
        }
        _Transmissions.RemoveAt(i);
        if (_Transmissions.Count == 0) {
            await DefaultTransmissionAsync();
            if (selected) {
                followCommands.Add(new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.MessageSelected, Folder = _Folder });
            }
        }
        _ListBox.ItemsSource = null;
        _ListBox.ItemsSource = _Transmissions;
        Header = ExpanderHeader();
    }

    public async Task NewMessageAsync(string messageId, List<SubspaceAppCmd> followCommands) {
        int i;

        for (i = 0; i < _Transmissions.Count && _Transmissions[i].MessageId != messageId; i ++) {
        }

        if (i < _Transmissions.Count) {
            return;
        }

        while (_Transmissions.Count > 0 && _Transmissions[0].IsPseudo) {
            _Transmissions.RemoveAt(0);
        }

        _Transmissions.Add(await _SubspaceTransmissionFactory.CreateAsync(_Folder, messageId));
        _Transmissions.Sort();
        _ListBox.ItemsSource = null;
        _ListBox.ItemsSource = _Transmissions;
        Header = ExpanderHeader();
    }

    public void SelectTransmission(SubspaceTransmission transmission) {
        int i;

        for (i = 0; i < _Transmissions.Count && _Transmissions[i].MessageId != transmission.MessageId; i ++) {
        }

        if (i >= _Transmissions.Count || _ListBox.SelectedIndex == i) {
            return;
        }

        if (_Transmissions[i].MessageId != ((SubspaceTransmission)_ListBox.Items[i]).MessageId) {
            throw new Exception("Transmissions and items out of synchronization!");
        }

        _ListBox.SelectedIndex = i;
        Focus();
    }
}