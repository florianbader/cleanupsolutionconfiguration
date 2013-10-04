using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Rio.CleanupSolutionConfiguration.Logic;
using Rio.CleanupSolutionConfiguration.Properties;
using Rio.CleanupSolutionConfiguration.Windows;

namespace Rio.CleanupSolutionConfiguration.Commands
{
    public class CleanupSolutionConfigurationCommand : OleMenuCommand
    {
        private CleanupConfiguration _cleanupConfiguration;

        private CleanupSolutionConfigurationPackage _package;

        public CleanupSolutionConfigurationCommand(CleanupSolutionConfigurationPackage package)
            : base(CommandExecute, new CommandID(GuidList.guidCleanupSolutionConfigurationCmdSet, (int)PkgCmdIDList.cmdidCleanUpSolution))
        {
            this._package = package;

            _cleanupConfiguration = new CleanupConfiguration(package);

            Enabled = true;
            BeforeQueryStatus += (sender, args) =>
            {
                Enabled = package.IDE.Solution.IsOpen;
            };
        }

        public CleanupConfiguration CleanupConfiguration
        {
            get { return _cleanupConfiguration; }
        }

        public void Execute()
        {
            string currentFrameworkName = string.Empty;
            string currentProductName = string.Empty;

            if (ShouldPrompt())
            {
                Project firstStartupProject = SolutionUtility.GetFirstStartupProject(_package.IDE.Solution);

                string visualStudioVersion = _package.IDE.Version;
                currentFrameworkName = string.Empty;
                currentProductName = string.Empty;

                if (firstStartupProject != null)
                {
                    currentFrameworkName = SolutionUtility.GetProjectProperty(firstStartupProject, "TargetFrameworkMoniker", string.Empty);
                    currentProductName = SolutionUtility.GetProjectProperty(firstStartupProject, "Product", string.Empty);
                }

                var prompt = new CleanupSolutionConfigurationPrompt(visualStudioVersion,
                    currentFrameworkName,
                    currentProductName,
                    Settings.Default.ShouldSetNETFrameworkVersion & Settings.Default.ShouldPromptNETFrameworkVersionOnCleanup,
                    Settings.Default.ShouldSetProductName & Settings.Default.ShouldPromptProductNameOnCleanup);

                prompt.ShowDialog();

                if (!prompt.DialogResult.HasValue || prompt.DialogResult.Value == false)
                    return;
            }

            _cleanupConfiguration.Cleanup(currentFrameworkName, currentProductName);
        }

        private static void CommandExecute(object sender, EventArgs e)
        {
            (sender as CleanupSolutionConfigurationCommand).Execute();
        }

        private bool ShouldPrompt()
        {
            return Settings.Default.ShouldPromptNETFrameworkVersionOnCleanup
                    | Settings.Default.ShouldPromptProductNameOnCleanup;
        }
    }
}