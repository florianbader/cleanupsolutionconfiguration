using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Rio.CleanupSolutionConfiguration.Logic
{
    public class FrameworkVersions
    {
        private int _visualStudioVersion;

        public FrameworkVersions(string visualStudioVersion)
        {
            var versionMatch = Regex.Match(visualStudioVersion, @"(\d+)\.\d+");

            if (!versionMatch.Success)
                _visualStudioVersion = 10;
            else
            {
                int version = 10;
                if (!int.TryParse(versionMatch.Groups[1].Value, out version))
                    _visualStudioVersion = 10;
                else
                    _visualStudioVersion = version;
            }
        }

        public FrameworkName GetFrameworkByFullname(string currentFrameworkName)
        {
            if (string.IsNullOrEmpty(currentFrameworkName))
                return null;

            try
            {
                var frameworkName = new FrameworkName(currentFrameworkName); ;
                return frameworkName;
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<FrameworkName> GetFrameworks()
        {
            yield return new FrameworkName(".NETFramework", new Version(2, 0));
            yield return new FrameworkName(".NETFramework", new Version(3, 0));
            yield return new FrameworkName(".NETFramework", new Version(3, 5));
            yield return new FrameworkName(".NETFramework", new Version(3, 5), "Client");

            if (_visualStudioVersion >= 10)
            {
                yield return new FrameworkName(".NETFramework", new Version(4, 0));
                yield return new FrameworkName(".NETFramework", new Version(4, 0), "Client");
            }

            if (_visualStudioVersion >= 12)
            {
                yield return new FrameworkName(".NETFramework", new Version(4, 5));
                yield return new FrameworkName(".NETFramework", new Version(4, 5), "Client");
            }
        }
    }
}