﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using NUnit.Framework;

    [TestFixture]
    internal static class ConfusionMatrixTests
    {
        [Test]
        [TestCaseSource(typeof(TestCaseSource), "PassingTests")]
        public static void Pass(string because)
        {
            Assert.Pass(because);
        }

        [Test]
        [TestCaseSource(typeof(TestCaseSource), "FailingTests")]
        public static void Fail(string because)
        {
            Assert.Fail(because);
        }
    }
}
