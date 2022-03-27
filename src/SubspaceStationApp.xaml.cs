using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Threading;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public partial class SubspaceStationApp {
        private List<SubspaceAppCmd> Commands;
        private int Rotator;

        private readonly IFolderResolver FolderResolver;
        private readonly SubspaceTransmissionFactory SubspaceTransmissionFactory;

        public SubspaceStationApp() {
            var container = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
            FolderResolver = container.Resolve<IFolderResolver>();
            SubspaceTransmissionFactory = new SubspaceTransmissionFactory(FolderResolver);

        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            Commands = new List<SubspaceAppCmd> {
                new(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Initialise },
                new(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Scan }
            };

            if (Current.Dispatcher == null) {
                throw new NullReferenceException(nameof(Current.Dispatcher));
            }

            var idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(.5), DispatcherPriority.ApplicationIdle, SubspaceIdleCallbackAsync, Current.Dispatcher);
            idleTimer.Start();
        }

        public void AddCommand(SubspaceAppCmd cmd) {
            int i;

            for (i = 0; i < Commands.Count; ) {
                if (Commands[i].CmdType == cmd.CmdType) {
                    Commands.RemoveAt(i);
                } else {
                    i ++;
                }
            }
            Commands.Add(cmd);
        }

        private async void SubspaceIdleCallbackAsync(object sender, EventArgs e) {
            if (Current.MainWindow == null) {
                throw new NullReferenceException(nameof(Current.MainWindow));
            }

            if (Commands.Count == 0) {
                Current.MainWindow.Cursor = Cursors.Arrow;
                Rotator = (Rotator + 1) % 20;
                if (Rotator != 0) {
                    return;
                }

                Commands.Add(new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Initialise });
                Commands.Add(new SubspaceAppCmd(FolderResolver, SubspaceTransmissionFactory) { CmdType = SubspaceAppCmdType.Scan });
                return;
            }

            Current.MainWindow.Cursor = Cursors.Wait;
            uint maxCommands = 10;
            do {
                var applicationCommand = Commands[0];
                Commands.RemoveAt(0);
                var station = (SubspaceStation)Current.MainWindow;
                var followCommands = new List<SubspaceAppCmd>();
                await applicationCommand.ExecuteAsync(station, followCommands);
                foreach(var followCmd in followCommands) {
                    AddCommand(followCmd);
                }
                maxCommands --;
            } while (maxCommands > 0 && Commands.Count > 0);
        }
    }
}