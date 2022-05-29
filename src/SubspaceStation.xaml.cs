using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor;

public partial class SubspaceStation {
    private SubspaceFolders Folder;
    private string MessageId;

    private readonly IFolderResolver FolderResolver;
    private readonly SubspaceTransmissionFactory SubspaceTransmissionFactory;

    public SubspaceStation() {
        InitializeComponent();
        Folder = SubspaceFolders.None;
        MessageId = "";
        var container = new ContainerBuilder().UsePegh("SubspaceSensor", new DummyCsArgumentPrompter()).Build();
        FolderResolver = container.Resolve<IFolderResolver>();
        SubspaceTransmissionFactory = new SubspaceTransmissionFactory(FolderResolver);
    }

    public SubspaceFolderBrowser FolderBrowser(SubspaceFolders folder) {
        switch(folder) {
            case SubspaceFolders.Port : return PortBrowser;
            case SubspaceFolders.Error : return ErrorBrowser;
            case SubspaceFolders.Inbox : return InboxBrowser;
            default : throw new Exception("Asked for a folder browser that is not supported");
        }
    }

    private int UrlLength(string s) {
        s = s + ' ';
        const string delimiters = " );,\n";
        return s.IndexOfAny(delimiters.ToCharArray());
    }

    private Block Message2Paragraph(string s) {
        var paragraph = new Paragraph();

        while (s.Length != 0) {
            var pos = s.IndexOf("http://", StringComparison.InvariantCulture);
            if (pos >= 0) {
                if (pos > 0) {
                    paragraph.Inlines.Add(s.Substring(0, pos));
                    s = s.Substring(pos, s.Length - pos);
                }
                var length = UrlLength(s);
                if (length <= 0) {
                    throw new Exception("Url length could not be detected: " + s);
                }

                var hyperlink = new Hyperlink(new Run(s.Substring(0, length))) {
                    NavigateUri = new Uri(s.Substring(0, length)), Cursor = Cursors.Arrow
                };
                hyperlink.RequestNavigate += OnNavigationRequest;
                paragraph.Inlines.Add(hyperlink);
                s = s.Substring(length, s.Length - length);
            } else {
                paragraph.Inlines.Add(s);
                s = "";
            }
        }
        return paragraph;
    }

    private FlowDocument Message2Rtf(string s) {
        var document = new FlowDocument();

        s = s.Replace("\r", "");
        s = s + "\n\n";
        while (s.Length != 0) {
            var pos = s.IndexOf("\n\n", StringComparison.InvariantCulture);
            document.Blocks.Add(Message2Paragraph(s.Substring(0, pos)));
            s = s.Substring(pos + 2, s.Length - pos - 2);
            while (s.Length != 0 && s[0] == '\n') {
                s = s.Remove(0, 1);
            }
        }
        return document;
    }

    public void SetTransmission(SubspaceTransmission transmission) {
        TextFrom.Text = transmission.From;
        TextTo.Text = transmission.To   ;
        TextCc.Text = transmission.Cc;
        TextBcc.Text = transmission.Bcc;
        TextMessage.Document = Message2Rtf(transmission.Text);
        if (transmission.Valid) {
            TextHeader.Text = transmission.Header;
        } else if (transmission.IsPseudo) {
            TextHeader.Text = "";
        } else {
            TextHeader.Text = "(selected message is invalid and cannot be displayed)";
        }
        if (!transmission.IsPseudo) {
            TextCreated.Text = transmission.Created.ToLongDateString() + ", " + transmission.Created.ToLongTimeString();
        } else {
            TextCreated.Text = "";
        }
        ButtonDelete.IsEnabled = transmission.Valid && transmission.Folder != SubspaceFolders.Port;
        ButtonDeleteAll.IsEnabled = transmission.Valid && transmission.Folder == SubspaceFolders.Inbox;
        Folder = transmission.Folder;
        MessageId = transmission.MessageId;
        FolderBrowser(Folder).SelectTransmission(transmission);
    }

    private void OnNavigationRequest(object sender, RequestNavigateEventArgs e) {
        Process.Start(e.Uri.ToString());
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e) {
        var applicationCommand = new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Delete, Folder = Folder, MessageId = MessageId };
        ((SubspaceStationApp)Application.Current).AddCommand(applicationCommand);
    }

    private void OnDeleteAllClick(object sender, RoutedEventArgs e) {
        var applicationCommand = new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.DeleteAll };
        ((SubspaceStationApp)Application.Current).AddCommand(applicationCommand);
    }

    private void OnUpdatePortClick(object sender, RoutedEventArgs e) {
        var applicationCommand = new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Initialise };
        ((SubspaceStationApp)Application.Current).AddCommand(applicationCommand);
        applicationCommand = new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Scan };
        ((SubspaceStationApp)Application.Current).AddCommand(applicationCommand);
    }
}