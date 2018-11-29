﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.TestSpeech
{
    using global::CommandLine;

    [Verb("test-speech", HelpText = "Runs test cases against the NLU service.")]
    internal class TestSpeechOptions : BaseOptions
    {
        [Option('u', "utterances", HelpText = "Path to utterances.", Required = true)]
        public string UtterancesPath { get; set; }

        [Option('e', "entity-types", HelpText = "Path to entity type configuration.", Required = false)]
        public string EntityTypesPath { get; set; }

        [Option('d', "directory", HelpText = "Path to recordings directory.", Required = true)]
        public string RecordingsDirectory { get; set; }

        [Option('o', "output", HelpText = "Path to labeled results output.", Required = false)]
        public string OutputPath { get; set; }
    }
}
