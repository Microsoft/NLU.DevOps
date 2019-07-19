﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests.Clean
{
    using CommandLine.Clean;
    using Models;

    internal class CleanCommandMock : CleanCommand
    {
        public CleanCommandMock(CleanOptions options)
            : base(options)
        {
        }

        protected override INLUService CreateNLUService() =>
            new NLUServiceMock();
    }
}
