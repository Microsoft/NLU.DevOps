﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Tests
{
    using System;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;

    internal class BaseCommandMock : BaseCommand<BaseOptions>
    {
        public BaseCommandMock(BaseOptions options)
            : base(options)
        {
        }

        public new ILogger Logger => base.Logger;

        public override int Main()
        {
            throw new NotImplementedException();
        }

        protected override INLUService CreateNLUService()
        {
            return new Mock<INLUService>().Object;
        }
    }
}
