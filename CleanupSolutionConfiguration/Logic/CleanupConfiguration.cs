using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using EnvDTE;
using Rio.CleanupSolutionConfiguration.Properties;
using Rio.CleanupSolutionConfiguration.Windows;

namespace Rio.CleanupSolutionConfiguration.Logic
{
    public class CleanupConfiguration
    {
        public static readonly string[] Configurations = Settings.Default.Configurations;
        public static readonly string[] ConfigurationsToRemove = Settings.Default.ConfigurationsToRemove;

        public static readonly string[] Platforms = Settings.Default.Platforms;
        public static readonly string[] PlatformsToRemove = Settings.Default.PlatformsToRemove;

        private string _currentFrameworkName;
        private string _currentProductName;
        private CleanupSolutionConfigurationPackage _package;
        private SolutionUtility _solutionUtility;

        public CleanupConfiguration(CleanupSolutionConfigurationPackage package)
        {
            _package = package;

            _solutionUtility = new SolutionUtility();
        }

        public static IEnumerable<string> GetMissingConfigurations(IEnumerable<string> configurations)
        {
            IEnumerable<string> missingConfigurations = configurations.Except(Configurations).Union(Configurations.Except(configurations));
            return missingConfigurations;
        }

        public static IEnumerable<string> GetMissingPlatforms(IEnumerable<string> platforms)
        {
            IEnumerable<string> missingPlatforms = platforms.Except(Platforms).Union(Platforms.Except(platforms));
            return missingPlatforms;
        }

        public void Cleanup(string frameworkName, string productName)
        {
            try
            {
                var solution = _package.IDE.Solution;

                string solutionFileName = solution.FullName;
                if (string.IsNullOrEmpty(solutionFileName))
                    return;

                _currentFrameworkName = frameworkName;
                _currentProductName = productName;

                if (!solution.Saved)
                    solution.SaveAs(solutionFileName);

                if (Settings.Default.ShouldSaveBackupsBeforeCleanup)
                    SaveBackup(solutionFileName);

                foreach (Project project in solution.Projects)
                    CleanupProject(project);

                CleanupSolution(solution);

                _package.IDE.Solution.SaveAs(solution.FileName);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(string.Format("{0}\nStacktrace:\n{1", exception.Message, exception.StackTrace));
                Debug.WriteLine(string.Format("{0}\nStacktrace:\n{1", exception.Message, exception.StackTrace));

                MessageBox.Show("An error occured while cleaning up the solution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string ReplaceOutputPathVariables(string projectName, string configurationName, string platformName, string outputPath)
        {
            outputPath = outputPath.Replace("${ProjectName}", projectName);
            outputPath = outputPath.Replace("${ConfigurationName}", configurationName);
            outputPath = outputPath.Replace("${PlatformName}", platformName);
            return outputPath;
        }

        private void AddMissingConfigurations(ConfigurationManager configurationManager)
        {
            if (configurationManager == null)
                return;

            var configurations = (configurationManager.ConfigurationRowNames as object[]).Cast<string>();
            IEnumerable<string> missingConfigurations = GetMissingConfigurations(configurations);

            if (missingConfigurations.Any())
            {
                string firstExistingConfiguration = configurations.FirstOrDefault();
                if (string.IsNullOrEmpty(firstExistingConfiguration))
                    return;

                foreach (string configuration in missingConfigurations)
                    configurationManager.AddConfigurationRow(configuration, firstExistingConfiguration, true);
            }
        }

        private void AddMissingPlatforms(ConfigurationManager configurationManager)
        {
            if (configurationManager == null)
                return;

            var platforms = (configurationManager.PlatformNames as object[]).Cast<string>();
            IEnumerable<string> missingPlatforms = GetMissingPlatforms(platforms);

            if (missingPlatforms.Any())
            {
                string firstExistingPlatform = platforms.FirstOrDefault();
                if (string.IsNullOrEmpty(firstExistingPlatform))
                    return;

                foreach (string platform in missingPlatforms)
                    configurationManager.AddPlatform(platform, firstExistingPlatform, true);
            }
        }

        private void CleanupProject(Project project)
        {
            if (project == null)
                return;

            if (Settings.Default.ShouldSaveBackupsBeforeCleanup)
                SaveBackup(project.FullName);

            var configurationManager = project.ConfigurationManager;

            AddMissingConfigurations(configurationManager);

            AddMissingPlatforms(configurationManager);

            UpdateDebugConfigurations(configurationManager);

            UpdateConfigurationsOutputPath(configurationManager);

            UpdateTargetFramework(project);

            UpdateAssemblyInformation(project);
        }

        private void CleanupSolution(Solution solution)
        {
            var solutionBuild = solution.SolutionBuild;
            var solutionConfigurations = solutionBuild.SolutionConfigurations;

            SolutionFile solutionFile = new SolutionFile(solution);
            solutionFile.Cleanup(solution.FullName);

            Dictionary<string, Project> projectByUniqueName = new Dictionary<string, Project>();
            foreach (Project project in solution.Projects)
                projectByUniqueName.Add(project.UniqueName, project);

            foreach (dynamic solutionConfiguration in solutionConfigurations)
            {
                string solutionConfigurationName = solutionConfiguration.Name;
                string platformName = solutionConfiguration.PlatformName;

                foreach (SolutionContext solutionContext in solutionConfiguration.SolutionContexts)
                {
                    Project project = null;
                    string projectName = solutionContext.ProjectName;

                    if (projectByUniqueName.ContainsKey(projectName))
                        project = projectByUniqueName[projectName];

                    if (project != null)
                    {
                        bool? shouldBuild = ProjectShouldBuildInSpecificConfiguration(solutionContext.ConfigurationName, solutionContext.PlatformName, project);
                        if (shouldBuild.HasValue)
                            solutionContext.ShouldBuild = shouldBuild.Value;

                        bool? shouldDeploy = ProjectShouldDeployInSpecificConfiguration(solutionContext.ConfigurationName, solutionContext.PlatformName, project);
                        if (shouldDeploy.HasValue)
                            solutionContext.ShouldDeploy = shouldDeploy.Value;
                    }
                }
            }
        }

        private string GetOutputPathByConfiguration(string projectName, string configurationName, string platformName)
        {
            string configuratioNameCapitalized = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(configurationName);
            string outputPath = Settings.Default.OutputDirectory;

            outputPath = ReplaceOutputPathVariables(projectName, configurationName, platformName, outputPath);

            return outputPath;
        }

        private bool? ProjectShouldBuildInSpecificConfiguration(string configurationName, string platformName, Project project)
        {
            if (project == null)
                return null;

            if (string.Compare(configurationName, "Debug", true) == 0)
            {
                if (SolutionUtility.IsSetupProject(project))
                {
                    if (Settings.Default.ShouldNotBuildSetupProjectsInDebugMode)
                        return false;
                    else
                        return null;
                }

                if (SolutionUtility.IsTestProject(project))
                {
                    if (Settings.Default.ShouldBuildTestProjectsInDebugMode)
                        return true;
                    else
                        return null;
                }
            }
            else if (string.Compare(configurationName, "Release", true) == 0)
            {
                if (SolutionUtility.IsSetupProject(project))
                {
                    if (Settings.Default.ShouldBuildSetupProjectsInReleaseMode)
                        return true;
                    else
                        return null;
                }

                if (SolutionUtility.IsTestProject(project))
                {
                    if (Settings.Default.ShouldNotBuildTestProjectsInReleaseMode)
                        return false;
                    else
                        return null;
                }
            }

            return null;
        }

        private bool? ProjectShouldDeployInSpecificConfiguration(string configurationName, string platformName, Project project)
        {
            if (project == null)
                return null;

            return null;
        }

        private void SaveBackup(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string backupFileName = string.Format("{0}.cleanup_backup");

            try
            {
                string backupFilePath = Path.Combine(Path.GetDirectoryName(filePath), backupFileName);
                File.Copy(fileName, backupFileName, true);
            }
            catch { }
        }

        private void UpdateAssemblyInformation(Project project)
        {
            if (Settings.Default.ShouldSetProductName)
                SolutionUtility.TrySetProjectProperty(project, "Product", _currentProductName);

            if (Settings.Default.ShouldSetCompanyName)
                SolutionUtility.TrySetProjectProperty(project, "Company", Settings.Default.CompanyName);

            if (Settings.Default.ShouldSetCopyright)
                SolutionUtility.TrySetProjectProperty(project, "Copyright", Settings.Default.Copyright);

            if (Settings.Default.ShouldSetTrademark)
                SolutionUtility.TrySetProjectProperty(project, "Trademark", Settings.Default.Trademark);

            if (Settings.Default.ShouldSetNeutralLanguage)
                SolutionUtility.TrySetProjectProperty(project, "NeutralLanguage", Settings.Default.NeutralLanguage);
        }

        private void UpdateConfigurationsOutputPath(ConfigurationManager configurationManager)
        {
            if (configurationManager == null)
                return;

            Project project = configurationManager.Parent as Project;

            string projectName = string.Empty;
            if (project != null)
                projectName = project.Name;

            var configurations = (configurationManager.ConfigurationRowNames as object[]).Cast<string>();
            foreach (string configurationName in configurations)
            {
                foreach (Configuration configuration in configurationManager.ConfigurationRow(configurationName))
                    configuration.Properties.Item("OutputPath").Value = GetOutputPathByConfiguration(projectName, configurationName, configuration.PlatformName);
            }
        }

        private void UpdateDebugConfigurations(ConfigurationManager configurationManager)
        {
            if (configurationManager == null)
                return;

            Configurations debugConfigurations = configurationManager.ConfigurationRow("Debug");

            foreach (Configuration debugConfiguration in debugConfigurations)
            {
                debugConfiguration.Properties.Item("DebugSymbols").Value = true;
                debugConfiguration.Properties.Item("Optimize").Value = false;
                debugConfiguration.Properties.Item("DefineConstants").Value = "DEBUG;TRACE";
                debugConfiguration.Properties.Item("DefineDebug").Value = true;
                debugConfiguration.Properties.Item("DefineTrace").Value = true;
            }
        }

        private void UpdateMissingSolutionConfigurations(SolutionConfigurations solutionConfigurations, Dictionary<string, List<string>> solutionConfigurationPlatform)
        {
            var missingConfigurations = GetMissingConfigurations(solutionConfigurationPlatform.Keys);
            if (missingConfigurations.Any())
            {
                string firstExistingConfiguration = solutionConfigurationPlatform.Keys.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstExistingConfiguration))
                {
                    foreach (string missingConfiguration in missingConfigurations)
                    {
                        solutionConfigurations.Add(missingConfiguration, firstExistingConfiguration, true);
                    }
                }
            }
        }

        private void UpdateTargetFramework(Project project)
        {
            try
            {
                if (Settings.Default.ShouldSetNETFrameworkVersion)
                    project.Properties.Item("TargetFrameworkMoniker").Value = _currentFrameworkName;
            }
            catch
            {
                // not a valid project type
            }
        }
    }
}