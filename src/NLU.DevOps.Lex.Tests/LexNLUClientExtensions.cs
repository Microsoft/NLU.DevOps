﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex.Tests
{
    using System.Threading.Tasks;
    using Core;
    using Models;
    using Newtonsoft.Json.Linq;

    internal static class LexNLUClientExtensions
    {
        public static Task<LabeledUtterance> TestAsync(this LexNLUTestClient client, string utterance)
        {
            return client.TestAsync(new JObject { { "text", utterance } });
        }
    }
}
