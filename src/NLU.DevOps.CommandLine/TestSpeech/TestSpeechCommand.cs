﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.TestSpeech
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;

    internal class TestSpeechCommand : BaseCommand<TestSpeechOptions>
    {
        public TestSpeechCommand(TestSpeechOptions options)
            : base(options)
        {
        }

        public override int Main()
        {
            this.RunAsync().Wait();
            return 0;
        }

        private async Task RunAsync()
        {
            this.Log("Running speech tests against NLU service...");

            var testUtterances = Read<List<LabeledUtteranceWithRecordingId>>(this.Options.UtterancesPath);
            if (testUtterances.Any(utterance => utterance.RecordingId == null))
            {
                throw new InvalidOperationException("Test utterances must have 'recordingID'.");
            }

            var speechFiles = testUtterances
                .Select(utterance => $"{Path.Combine(this.Options.RecordingsDirectory, utterance.RecordingId)}.wav");

            var entityTypes = this.Options.EntityTypesPath != null
                ? Read<IList<EntityType>>(this.Options.EntityTypesPath)
                : Array.Empty<EntityType>();

            var testResults = await speechFiles.SelectAsync(speechFile => this.NLUService.TestSpeechAsync(speechFile, entityTypes)).ConfigureAwait(false);

            var stream = this.Options.OutputPath != null
                ? File.OpenWrite(this.Options.OutputPath)
                : Console.OpenStandardOutput();

            using (stream)
            {
                Write(stream, testResults);
            }
        }

        private class LabeledUtteranceWithRecordingId : LabeledUtterance
        {
            public LabeledUtteranceWithRecordingId(string text, string intent, string recordingId, IReadOnlyList<Entity> entities)
                : base(text, intent, entities)
            {
                this.RecordingId = recordingId;
            }

            public string RecordingId { get; }
        }
    }
}
