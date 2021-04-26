using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public partial class SubspaceStationApp {
        private List<SubspaceAppCmd> vCommands;
        private int vRotator;

        private readonly IContainer vContainer;

        public SubspaceStationApp() {
            vContainer = new ContainerBuilder().UsePegh(new DummyCsArgumentPrompter()).Build();
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            vCommands = new List<SubspaceAppCmd> {
                new(vContainer.Resolve<IFolderResolver>()) { CmdType = SubspaceAppCmdType.Initialise },
                new(vContainer.Resolve<IFolderResolver>()) { CmdType = SubspaceAppCmdType.Scan }
            };

            if (Current.Dispatcher == null) {
                throw new NullReferenceException(nameof(Current.Dispatcher));
            }

            var idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(.5), DispatcherPriority.ApplicationIdle, SubspaceIdleCallbackAsync, Current.Dispatcher);
            idleTimer.Start();
        }

        public void AddCommand(SubspaceAppCmd cmd) {
            int i;

            for (i = 0; i < vCommands.Count; ) {
                if (vCommands[i].CmdType == cmd.CmdType) {
                    vCommands.RemoveAt(i);
                } else {
                    i ++;
                }
            }
            vCommands.Add(cmd);
        }

        private async void SubspaceIdleCallbackAsync(object sender, EventArgs e) {
            if (Current.MainWindow == null) {
                throw new NullReferenceException(nameof(Current.MainWindow));
            }

            if (vCommands.Count == 0) {
                Current.MainWindow.Cursor = Cursors.Arrow;
                vRotator = (vRotator + 1) % 20;
                if (vRotator != 0) {
                    return;
                }

                vCommands.Add(new SubspaceAppCmd(vContainer.Resolve<IFolderResolver>()) { CmdType = SubspaceAppCmdType.Initialise });
                vCommands.Add(new SubspaceAppCmd(vContainer.Resolve<IFolderResolver>()) { CmdType = SubspaceAppCmdType.Scan });
                return;
            }

            Current.MainWindow.Cursor = Cursors.Wait;
            uint maxCommands = 10;
            do {
                var applicationCommand = vCommands[0];
                vCommands.RemoveAt(0);
                var station = (SubspaceStation)Current.MainWindow;
                var followCommands = new List<SubspaceAppCmd>();
                await applicationCommand.ExecuteAsync(station, followCommands);
                foreach(var followCmd in followCommands) {
                    AddCommand(followCmd);
                }
                maxCommands --;
            } while (maxCommands > 0 && vCommands.Count > 0);
        }
    }
}