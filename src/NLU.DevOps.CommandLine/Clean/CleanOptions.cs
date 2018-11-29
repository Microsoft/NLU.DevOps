﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Clean
{
    using global::CommandLine;

    [Verb("clean", HelpText = "Cleans up the NLU service.")]
    internal class CleanOptions : BaseOptions
    {
        [Option('c', "delete-config", HelpText = "Flag to delete NLU service configuration.", Required = false)]
        public bool DeleteConfig { get; set; }
    }
}
