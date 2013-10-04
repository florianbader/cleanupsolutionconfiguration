using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace Rio.CleanupSolutionConfiguration.Logic
{
    public class SolutionUtility
    {
        public static Project GetFirstStartupProject(Solution solution)
        {
            var startupProjectPaths = (solution.SolutionBuild.StartupProjects as object[]).Cast<string>();

            string firstStartupProjectPath = startupProjectPaths.FirstOrDefault();
            if (string.IsNullOrEmpty(firstStartupProjectPath))
                return null;

            foreach (Project project in solution.Projects)
            {
                if (string.Compare(project.UniqueName, firstStartupProjectPath, false) == 0)
                    return project;
            }

            return null;
        }

        public static T GetProjectProperty<T>(Project project, string propertyName, T defaultValue)
            where T : class
        {
            try
            {
                T value = project.Properties.Item(propertyName).Value as T;

                if (value == null)
                    return defaultValue;
                return value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T GetProjectPropertyForValueType<T>(Project project, string propertyName, T defaultValue)
            where T : struct
        {
            try
            {
                T? value = project.Properties.Item(propertyName).Value as T?;

                if (!value.HasValue)
                    return defaultValue;
                return value.Value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool IsSetupProject(Project project)
        {
            return project.Kind.Contains("{54435603-DBB4-11D2-8724-00A0C9A8B90C}");
        }

        public static bool IsTestProject(Project project)
        {
            return project.Kind.Contains("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
        }

        public static void TrySetProjectProperty<T>(Project project, string propertyName, T propertyValue)
        {
            try
            {
                project.Properties.Item(propertyName).Value = propertyValue.ToString();
            }
            catch { }
        }
    }
}