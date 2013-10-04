// Guids.cs
// MUST match guids.h
using System;

namespace Rio.CleanupSolutionConfiguration
{
    internal static class GuidList
    {
        public const string guidCleanupSolutionConfigurationCmdSetString = "c0d89b50-69ae-48ff-9eb4-6a2940f33ecb";
        public const string guidCleanupSolutionConfigurationPkgString = "610646e1-4032-4b2f-ae1b-fa847d18659f";
        public const string guidToolWindowPersistanceString = "c37c5f0b-ef9a-44c3-ac1f-96f6350b258e";
        public static readonly Guid guidCleanupSolutionConfigurationCmdSet = new Guid(guidCleanupSolutionConfigurationCmdSetString);
    };
}