using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Threading;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor;

public partial class SubspaceStationApp {
    private List<SubspaceAppCmd> _Commands;
    private int _Rotator;

    private readonly IFolderResolver _FolderResolver;
    private readonly SubspaceTransmissionFactory _SubspaceTransmissionFactory;

    public SubspaceStationApp() {
        IContainer container = new ContainerBuilder().UsePegh("SubspaceSensor").Build();
        _FolderResolver = container.Resolve<IFolderResolver>();
        _SubspaceTransmissionFactory = new SubspaceTransmissionFactory(_FolderResolver);

    }

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        _Commands = [
            new(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Initialise },
            new(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Scan }
        ];

        if (Current.Dispatcher == null) {
            throw new NullReferenceException(nameof(Current.Dispatcher));
        }

        var idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(.5), DispatcherPriority.ApplicationIdle, SubspaceIdleCallbackAsync, Current.Dispatcher);
        idleTimer.Start();
    }

    public void AddCommand(SubspaceAppCmd cmd) {
        int i;

        for (i = 0; i < _Commands.Count; ) {
            if (_Commands[i].CmdType == cmd.CmdType) {
                _Commands.RemoveAt(i);
            } else {
                i ++;
            }
        }
        _Commands.Add(cmd);
    }

    private async void SubspaceIdleCallbackAsync(object sender, EventArgs e) {
        if (Current.MainWindow == null) {
            throw new NullReferenceException(nameof(Current.MainWindow));
        }

        if (_Commands.Count == 0) {
            Current.MainWindow.Cursor = Cursors.Arrow;
            _Rotator = (_Rotator + 1) % 20;
            if (_Rotator != 0) {
                return;
            }

            _Commands.Add(new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Initialise });
            _Commands.Add(new SubspaceAppCmd(_FolderResolver, _SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Scan });
            return;
        }

        Current.MainWindow.Cursor = Cursors.Wait;
        uint maxCommands = 10;
        do {
            SubspaceAppCmd applicationCommand = _Commands[0];
            _Commands.RemoveAt(0);
            var station = (SubspaceStation)Current.MainWindow;
            var followCommands = new List<SubspaceAppCmd>();
            await applicationCommand.ExecuteAsync(station, followCommands);
            foreach(SubspaceAppCmd followCmd in followCommands) {
                AddCommand(followCmd);
            }
            maxCommands --;
        } while (maxCommands > 0 && _Commands.Count > 0);
    }
}