using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Rio.CleanupSolutionConfiguration.Commands;
using Rio.CleanupSolutionConfiguration.Logic;
using Rio.CleanupSolutionConfiguration.Properties;
using Rio.CleanupSolutionConfiguration.Windows;

namespace Rio.CleanupSolutionConfiguration
{
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]// This attribute registers a tool window exposed by this package.
    [Guid(GuidList.guidCleanupSolutionConfigurationPkgString)]
    public sealed class CleanupSolutionConfigurationPackage : Package
    {
        private DTE2 _ide;

        public CleanupSolutionConfigurationPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            InitializeDefaultConfiguration();
        }

        public DTE2 IDE
        {
            get { return _ide ?? (_ide = (DTE2)GetService(typeof(DTE))); }
        }

        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            OleMenuCommandService menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != menuCommandService)
            {
                menuCommandService.AddCommand(new OptionWindowCommand(this));
                menuCommandService.AddCommand(new CleanupSolutionConfigurationCommand(this));
            }
        }

        private void InitializeDefaultConfiguration()
        {
            if (!Settings.Default.WasInitialized)
            {
                Settings.Default.Configurations = new string[] { "Debug", "Release" };
                Settings.Default.ConfigurationsToRemove = new string[0];
                Settings.Default.Platforms = new string[] { "Any CPU", "x86", "x64" };
                Settings.Default.PlatformsToRemove = new string[] { "Mixed Platforms" };

                Settings.Default.WasInitialized = true;

                Settings.Default.Save();
            }
        }
    }
}