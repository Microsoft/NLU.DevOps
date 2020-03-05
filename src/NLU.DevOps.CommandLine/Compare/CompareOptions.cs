﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using global::CommandLine;

    [Verb("compare", HelpText = "Compare test results.")]
    internal class CompareOptions
    {
        [Option('e', "expected", HelpText = "Path to expected utterances.", Required = true)]
        public string ExpectedUtterancesPath { get; set; }

        [Option('a', "actual", HelpText = "Path to actual utterances.", Required = true)]
        public string ActualUtterancesPath { get; set; }

        [Option('l', "label", HelpText = "Label for differentiating comparison runs.", Required = false)]
        public string TestLabel { get; set; }

        [Option('m', "metadata", HelpText = "Return test case metadata in addition to NUnit test results.", Required = false)]
        public bool Metadata { get; set; }

        [Option('o', "output-folder", HelpText = "Output path for test results.", Required = false)]
        public string OutputFolder { get; set; }

        [Option("strict", HelpText = "Return false positive results for all unexpected entities.", Required = false)]
        public bool Strict { get; set; }

        [Option('n', "true-negative-intent", HelpText = "Specifies the name of the intent that used for true negatives.", Required = false)]
        public string TrueNegativeIntent { get; set; }
    }
}
