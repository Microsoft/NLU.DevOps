﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using System.Collections.Generic;
    using System.Linq;
    using ModelPerformance.Tests;
    using NUnitLite;

    internal static class CompareCommand
    {
        public static int Run(CompareOptions options)
        {
            var parameters = CreateParameters(
                (ConfigurationConstants.ExpectedUtterancesPathKey, options.ExpectedUtterancesPath),
                (ConfigurationConstants.ActualUtterancesPathKey, options.ActualUtterancesPath),
                (ConfigurationConstants.TestLabelKey, options.TestLabel));

            var arguments = new List<string> { $"-p:{parameters}" };
            if (options.OutputFolder != null)
            {
                arguments.Add($"--work={options.OutputFolder}");
            }

            return new AutoRun(typeof(ConfigurationConstants).Assembly).Execute(arguments.ToArray());
        }

        private static string CreateParameters(params (string, string)[] parameters)
        {
            var filteredParameters = parameters
                .Where(p => p.Item2 != null)
                .Select(p => $"{p.Item1}={p.Item2}");

            return string.Join(';', filteredParameters);
        }
    }
}
