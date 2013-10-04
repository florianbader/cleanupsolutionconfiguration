using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;

namespace Rio.CleanupSolutionConfiguration.Logic
{
    public class SolutionFile
    {
        private Dictionary<string, List<string>> _configurationPlatforms;
        private Dictionary<string, Dictionary<string, List<string>>> _projectConfigurationPlatforms;
        private Solution _solution;

        public SolutionFile(Solution solution)
        {
            _solution = solution;
        }

        public void Cleanup(string solutionFileName)
        {
            _solution.Close();

            bool solutionConfigurationPlatformsPreSolution = false;
            bool solutionConfigurationPlatformsPostSolution = false;

            var newSolutionFileContent = new List<string>();
            string[] solutionFileContent = File.ReadAllLines(solutionFileName);

            _configurationPlatforms = new Dictionary<string, List<string>>();
            _projectConfigurationPlatforms = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (string line in solutionFileContent)
            {
                if (line.Contains("EndGlobalSection"))
                {
                    if (solutionConfigurationPlatformsPreSolution)
                    {
                        newSolutionFileContent.AddRange(AddMissingSolutionConfigurationPlatforms());

                        solutionConfigurationPlatformsPreSolution = false;
                    }
                    else if (solutionConfigurationPlatformsPostSolution)
                    {
                        newSolutionFileContent.AddRange(AddMissingProjectConfigurationPlatforms());

                        solutionConfigurationPlatformsPostSolution = false;
                    }

                    newSolutionFileContent.Add(line);
                }

                else if (solutionConfigurationPlatformsPreSolution)
                {
                    string newLine = MatchSolutionConfigurationPlatforms(line);
                    newSolutionFileContent.Add(newLine);
                }

                else if (solutionConfigurationPlatformsPostSolution)
                {
                    string newLine = ProjectConfigurationPlatforms(line);
                    newSolutionFileContent.Add(newLine);
                }

                else if (line.Contains("GlobalSection(SolutionConfigurationPlatforms) = preSolution"))
                {
                    solutionConfigurationPlatformsPreSolution = true;

                    newSolutionFileContent.Add(line);
                }

                else if (line.Contains("GlobalSection(ProjectConfigurationPlatforms) = postSolution"))
                {
                    solutionConfigurationPlatformsPostSolution = true;

                    newSolutionFileContent.Add(line);
                }

                else
                {
                    newSolutionFileContent.Add(line);
                }
            }

            File.WriteAllLines(solutionFileName, newSolutionFileContent);

            _solution.Open(solutionFileName);
        }

        private IEnumerable<string> AddMissingProjectConfigurationPlatforms()
        {
            foreach (string projectId in _projectConfigurationPlatforms.Keys)
            {
                var missingConfigurations = CleanupConfiguration.GetMissingConfigurations(_projectConfigurationPlatforms[projectId].Keys);
                foreach (string missingConfiguration in missingConfigurations)
                {
                    foreach (string platform in CleanupConfiguration.Platforms)
                    {
                        yield return string.Format("{2}.{0}|{1}.ActiveCfg = {0}|{1}", missingConfiguration, platform, projectId);
                        yield return string.Format("{2}.{0}|{1}.Build.0 = {0}|{1}", missingConfiguration, platform, projectId);
                    }
                }

                foreach (string configuration in _projectConfigurationPlatforms[projectId].Keys)
                {
                    var missingPlatforms = CleanupConfiguration.GetMissingPlatforms(_projectConfigurationPlatforms[projectId][configuration]);
                    foreach (string missingPlatform in missingPlatforms)
                    {
                        yield return string.Format("{2}.{0}|{1}.ActiveCfg = {0}|{1}", configuration, missingPlatform, projectId);
                        yield return string.Format("{2}.{0}|{1}.Build.0 = {0}|{1}", configuration, missingPlatform, projectId);
                    }
                }
            }
        }

        private IEnumerable<string> AddMissingSolutionConfigurationPlatforms()
        {
            var missingConfigurations = CleanupConfiguration.GetMissingConfigurations(_configurationPlatforms.Keys);
            foreach (string missingConfiguration in missingConfigurations)
            {
                foreach (string platform in CleanupConfiguration.Platforms)
                {
                    yield return string.Format("{0}|{1} = {0}|{1}", missingConfiguration, platform);
                }
            }

            foreach (string configuration in _configurationPlatforms.Keys)
            {
                var missingPlatforms = CleanupConfiguration.GetMissingPlatforms(_configurationPlatforms[configuration]);
                foreach (string missingPlatform in missingPlatforms)
                {
                    yield return string.Format("{0}|{1} = {0}|{1}", configuration, missingPlatform);
                }
            }
        }

        private string MatchSolutionConfigurationPlatforms(string line)
        {
            var lineMatch = Regex.Match(line, @"\s*(.+)\|(.+) = (.+)\|(.+)");
            if (!lineMatch.Success)
                return line;

            string firstConfigurationName = lineMatch.Groups[1].Value;
            string firstPlatformName = lineMatch.Groups[2].Value;

            string secondConfigurationName = lineMatch.Groups[3].Value;
            string secondPlatformName = lineMatch.Groups[4].Value;

            if (string.Compare(firstConfigurationName, secondConfigurationName) != 0
                || string.Compare(firstPlatformName, secondPlatformName) != 0)
            {
                firstConfigurationName = secondConfigurationName;
                firstPlatformName = secondPlatformName;
            }

            if (!_configurationPlatforms.ContainsKey(firstConfigurationName))
                _configurationPlatforms.Add(firstConfigurationName, new List<string>());
            _configurationPlatforms[firstConfigurationName].Add(firstPlatformName);

            if (CleanupConfiguration.ConfigurationsToRemove.Contains(firstConfigurationName)
                || CleanupConfiguration.PlatformsToRemove.Contains(firstPlatformName))
                return null;

            return string.Format("{0}|{1} = {0}|{1}", firstConfigurationName, firstPlatformName);
        }

        private string ProjectConfigurationPlatforms(string line)
        {
            Match lineMatch = Regex.Match(line, @"\s*(\{.+\})\.(.+)\|(.+).(ActiveCfg|Build.\d+) = (.+)\|(.+)");
            if (!lineMatch.Success)
            {
                return line;
            }

            string projectId = lineMatch.Groups[1].Value;

            string firstConfigurationName = lineMatch.Groups[2].Value;
            string firstPlatformName = lineMatch.Groups[3].Value;

            string suffix = lineMatch.Groups[4].Value;

            string secondConfigurationName = lineMatch.Groups[5].Value;
            string secondPlatformName = lineMatch.Groups[6].Value;

            if (string.Compare(firstConfigurationName, secondConfigurationName) != 0
                || string.Compare(firstPlatformName, secondPlatformName) != 0)
            {
                firstConfigurationName = secondConfigurationName;
                firstPlatformName = secondPlatformName;
            }

            if (!_projectConfigurationPlatforms.ContainsKey(projectId))
                _projectConfigurationPlatforms.Add(projectId, new Dictionary<string, List<string>>());

            if (!_projectConfigurationPlatforms[projectId].ContainsKey(firstConfigurationName))
                _projectConfigurationPlatforms[projectId].Add(firstConfigurationName, new List<string>());

            _projectConfigurationPlatforms[projectId][firstConfigurationName].Add(firstPlatformName);

            if (CleanupConfiguration.ConfigurationsToRemove.Contains(firstConfigurationName)
                || CleanupConfiguration.PlatformsToRemove.Contains(firstPlatformName))
                return null;

            return string.Format("{2}.{0}|{1}.{3} = {0}|{1}", firstConfigurationName, firstPlatformName, projectId, suffix);
        }
    }
}