using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Aspenlaub.Net.GitHub.CSharp.SubspaceSensor {
    public partial class SubspaceStationApp {
        private List<SubspaceAppCmd> vCommands;
        private int vRotator;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            vCommands = new List<SubspaceAppCmd> {
                new SubspaceAppCmd {CmdType = SubspaceAppCmdType.Initialise}, new SubspaceAppCmd {CmdType = SubspaceAppCmdType.Scan}
            };

            if (Current.Dispatcher == null) {
                throw new NullReferenceException(nameof(Current.Dispatcher));
            }

            var idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(.5), DispatcherPriority.ApplicationIdle, SubspaceIdleCallback, Current.Dispatcher);
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

        private void SubspaceIdleCallback(object sender, EventArgs e) {
            if (Current.MainWindow == null) {
                throw new NullReferenceException(nameof(Current.MainWindow));
            }

            if (vCommands.Count == 0) {
                Current.MainWindow.Cursor = Cursors.Arrow;
                vRotator = (vRotator + 1) % 20;
                if (vRotator != 0) {
                    return;
                }

                vCommands.Add(new SubspaceAppCmd { CmdType = SubspaceAppCmdType.Initialise });
                vCommands.Add(new SubspaceAppCmd { CmdType = SubspaceAppCmdType.Scan });
                return;
            }

            Current.MainWindow.Cursor = Cursors.Wait;
            uint maxCommands = 10;
            do {
                var applicationCommand = vCommands[0];
                vCommands.RemoveAt(0);
                var station = (SubspaceStation)Current.MainWindow;
                applicationCommand.Execute(station, out var followCommands);
                foreach(var followCmd in followCommands) {
                    AddCommand(followCmd);
                }
                maxCommands --;
            } while (maxCommands > 0 && vCommands.Count > 0);
        }
    }
}