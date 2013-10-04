using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Rio.CleanupSolutionConfiguration.Windows;

namespace Rio.CleanupSolutionConfiguration.Commands
{
    public class OptionWindowCommand : OleMenuCommand
    {
        private CleanupSolutionConfigurationPackage _package;

        public OptionWindowCommand(CleanupSolutionConfigurationPackage package)
            : base(CommandExecute, new CommandID(GuidList.guidCleanupSolutionConfigurationCmdSet, (int)PkgCmdIDList.cmdidToolsConfiguration))
        {
            this._package = package;

            Enabled = true;
        }

        public void Execute()
        {
            new ConfigurationWindow().ShowDialog();
        }

        private static void CommandExecute(object sender, EventArgs e)
        {
            (sender as OptionWindowCommand).Execute();
        }
    }
}