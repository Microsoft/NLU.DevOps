// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class TestCaseSourceTests
    {
        private const string FalseNegativeEntityValueRegex = @"FalseNegativeEntityValue\(.*?\)";
        private const string TruePositiveEntityValueRegex = @"TruePositiveEntityValue\(.*?\)";
        private const string FalseNegativeEntityRegex = @"FalseNegativeEntity\(.*?\)";
        private const string FalsePositiveEntityRegex = @"FalsePositiveEntity\(.*?\)";
        private const string TruePositiveEntityRegex = @"TruePositiveEntity\(.*?\)";
        private const string FalsePositiveIntentRegex = @"FalsePositiveIntent\(.*?\)";
        private const string FalseNegativeIntentRegex = @"FalseNegativeIntent\(.*?\)";

        [Test]
        public static void TestToIntentTestCaseExpectingNoneActualDayTime()
        {
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("FOO", "None", null),
                new LabeledUtterance("FOO", "DayTime", null)
            });

            var tests = TestCaseSource.ToIntentTestCases(utterances);
            tests.Count().Should().Be(1);
            tests.Single().TestName.Should().MatchRegex(FalsePositiveIntentRegex);
            tests.Single().ResultKind.Should().Be(ConfusionMatrixResultKind.FalsePositive);
        }

        [Test]
        public static void TestToIntentTestCaseActualNoneExpectingDayTime()
        {
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("FOO", "DayTime", null),
                new LabeledUtterance("FOO", "None", null)
            });

            var tests = TestCaseSource.ToIntentTestCases(utterances);
            tests.Count().Should().Be(1);
            tests.Single().TestName.Should().MatchRegex(FalseNegativeIntentRegex);
            tests.Single().ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
        }

        [Test]
        public static void TestToEntityTestCasesMissingAnActualEntity()
        {
            var entity = CreateEntityList("EntityType");
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("FOO", "DayTime", entity),
                new LabeledUtterance("FOO", "DayTime", null)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            actualTestResult.First().ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
            actualTestResult.First().TestName.Should().MatchRegex(FalseNegativeEntityRegex);
        }

        [Test]
        public static void TestToEntityTestCasesWrongEntityType()
        {
            var actualEntity = CreateEntityList("EntityType");
            var expectedEntity = CreateEntityList("WrongType");
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
        }

        [Test]
        public static void TestToEntityTestCasesMissingEntityInExpectedFile()
        {
            var actualEntity = CreateEntityList("EntityType");
            actualEntity.Add(new Entity("RecognizedEntity", "value", "text", 2));
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("FOO", "DayTime", expectedEntity),
                new LabeledUtterance("FOO", "DayTime", actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(3);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualTruePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.TruePositive);
            var actualTruePositiveEntityValue = testResult.FirstOrDefault(t => Matches(t.TestName, TruePositiveEntityValueRegex));
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualTruePositive.Should().NotBeNull();
            actualTruePositive.TestName.Should().MatchRegex(TruePositiveEntityRegex);
            actualTruePositiveEntityValue.Should().NotBeNull();
        }

        [Test]
        public static void TestToEntityTestCasesMissingEntityInActualResult()
        {
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("FOO", "DayTime", expectedEntity),
                new LabeledUtterance("FOO", "DayTime", null)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(1);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
        }

        [Test]
        public static void TestToEntityTestCasesWithDifferentEntityValueInActualVersusExpected()
        {
            var actualEntity = new[] { new Entity("EntityType", "differentEntityValue", "differentMatchedText", 1) };
            var expectedEntity = CreateEntityList("EntityType");
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            var actualFalsePositive = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalsePositive);
            var actualFalseNegative = testResult.FirstOrDefault(t => t.ResultKind == ConfusionMatrixResultKind.FalseNegative);
            actualFalsePositive.Should().NotBeNull();
            actualFalsePositive.TestName.Should().MatchRegex(FalsePositiveEntityRegex);
            actualFalseNegative.Should().NotBeNull();
            actualFalseNegative.TestName.Should().MatchRegex(FalseNegativeEntityRegex);
        }

        [Test]
        public static void ToTextTestCaseTrueNegative()
        {
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance(null, "DayTime", null).WithProperty("speechFile", "speechFile"),
                new LabeledUtterance(null, "DayTime", null)
            });

            var actualResult = TestCaseSource.ToTextTestCases(utterances).Single();
            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.TrueNegative);
        }

        [Test]
        public static void ToTextTestCaseFalseNegative()
        {
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance("foo", "DayTime", null).WithProperty("speechFile", "speechFile"),
                 new LabeledUtterance(null, "DayTime", null)
             });

            var actualResult = TestCaseSource.ToTextTestCases(utterances).Single();
            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.FalseNegative);
        }

        [Test]
        public static void ToTextTestCaseTruePositive()
        {
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("foo", "DayTime", null).WithProperty("speechFile", "speechFile"),
                new LabeledUtterance("FOO", "DayTime", null)
            });

            var actualResult = TestCaseSource.ToTextTestCases(utterances).Single();
            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.TruePositive);
        }

        [Test]
        public static void ToTextTestCaseFalsePositive()
        {
            var utterances = CreatePair(new[]
            {
                new LabeledUtterance("foo", "DayTime", null).WithProperty("speechFile", "speechFile"),
                new LabeledUtterance("baz", "DayTime", null)
            });

            var actualResult = TestCaseSource.ToTextTestCases(utterances).Single();
            actualResult.ResultKind.Should().Be(ConfusionMatrixResultKind.FalsePositive);
        }

        [Test]
        [TestCase("{}", null, FalseNegativeEntityValueRegex)]
        [TestCase("{}", "null", FalseNegativeEntityValueRegex)]
        [TestCase("[]", "null", FalseNegativeEntityValueRegex)]
        [TestCase("42", "null", FalseNegativeEntityValueRegex)]
        [TestCase("{\"foo\":42}", "{}", FalseNegativeEntityValueRegex)]
        [TestCase("[\"foo\"]", "[]", FalseNegativeEntityValueRegex)]
        [TestCase("{\"foo\":42, \"bar\":42}", "{\"bar\":42, \"foo\":42}", TruePositiveEntityValueRegex)]
        [TestCase("{\"foo\":42}", "{\"bar\":42, \"foo\":42}", TruePositiveEntityValueRegex)]
        [TestCase("[1,2]", "[2,1]", TruePositiveEntityValueRegex)]
        [TestCase("[2]", "[2,1]", TruePositiveEntityValueRegex)]
        public static void ToEntityTestCasesWithEntityValue(string expectedJson, string actualJson, string testMessageRegex)
        {
            var actualEntity = new List<Entity> { new Entity("EntityType", ParseEntityValueJson(actualJson), "foo", 1) };
            var expectedEntity = new List<Entity> { new Entity("EntityType", ParseEntityValueJson(expectedJson), "foo", 1) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            var actual = testResult.FirstOrDefault(t => Matches(t.TestName, testMessageRegex));
            actual.Should().NotBeNull();
        }

        [Test]
        public static void ToEntityTestCasesWithNullExpectedEntityValue()
        {
            foreach (var expectedEntityValue in new[] { JValue.CreateNull(), null })
            {
                var actualEntity = new[] { new Entity("EntityType", 42, "differentMatchedText", 1) };
                var expectedEntity = new[] { new Entity("EntityType", expectedEntityValue, "differentMatchedText", 1) };
                var utterances = CreatePair(new[]
                {
                     new LabeledUtterance("FOO", "DayTime", expectedEntity),
                     new LabeledUtterance("FOO", "DayTime", actualEntity)
                });

                var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

                var testResult = actualTestResult.ToList();
                testResult.Count().Should().Be(1);
                testResult.Should().NotContain(t => Matches(t.TestName, TruePositiveEntityValueRegex));
            }
        }

        [Test]
        public static void ToEntityTestCasesWithIncorrectMatchIndex()
        {
            var actualEntity = new[] { new Entity("EntityType", "EntityValue", "differentMatchedText", 1) };
            var expectedEntity = new[] { new Entity("EntityType", null, "differentMatchedText", 2) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance("FOO", "DayTime", expectedEntity),
                 new LabeledUtterance("FOO", "DayTime", actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, FalsePositiveEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, FalseNegativeEntityRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithNullActualMatchText()
        {
            var actualEntity = new[] { new Entity("EntityType", "foo", null, 0) };
            var expectedEntity = new[] { new Entity("EntityType", null, "foo", 0) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance(null, null, expectedEntity),
                 new LabeledUtterance(null, null, actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(1);
            testResult.Should().Contain(t => Matches(t.TestName, TruePositiveEntityRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithNullActualMatchTextByValue()
        {
            var actualEntity = new[] { new Entity("EntityType", "bar", null, 0) };
            var expectedEntity = new[] { new Entity("EntityType", "bar", "foo", 0) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance(null, null, expectedEntity),
                 new LabeledUtterance(null, null, actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, TruePositiveEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, TruePositiveEntityValueRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithMatchingValueButMismatchedMatchText()
        {
            var actualEntity = new[] { new Entity("EntityType", "bar", "bar", 0) };
            var expectedEntity = new[] { new Entity("EntityType", "bar", "foo", 0) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance(null, null, expectedEntity),
                 new LabeledUtterance(null, null, actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, FalseNegativeEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, FalsePositiveEntityRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithFalseNegativeEntityValueNull()
        {
            var actualEntity = new[] { new Entity("EntityType", null, "foo", 0) };
            var expectedEntity = new[] { new Entity("EntityType", "bar", "foo", 0) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance(null, null, expectedEntity),
                 new LabeledUtterance(null, null, actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, TruePositiveEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, FalseNegativeEntityValueRegex));
        }

        [Test]
        public static void ToEntityTestCasesWithFalseNegativeEntityValueMismatch()
        {
            var actualEntity = new[] { new Entity("EntityType", "qux", "foo", 0) };
            var expectedEntity = new[] { new Entity("EntityType", "bar", "foo", 0) };
            var utterances = CreatePair(new[]
            {
                 new LabeledUtterance(null, null, expectedEntity),
                 new LabeledUtterance(null, null, actualEntity)
            });

            var actualTestResult = TestCaseSource.ToEntityTestCases(utterances);

            var testResult = actualTestResult.ToList();
            testResult.Count().Should().Be(2);
            testResult.Should().Contain(t => Matches(t.TestName, TruePositiveEntityRegex));
            testResult.Should().Contain(t => Matches(t.TestName, FalseNegativeEntityValueRegex));
        }

        [Test]
        [TestCase("foo", "foo", 1, 0, 0, 0)]
        [TestCase(null, null, 0, 1, 0, 0)]
        [TestCase(null, "foo", 0, 0, 1, 0)]
        [TestCase("foo", "bar", 0, 0, 1, 0)]
        [TestCase("foo", null, 0, 0, 0, 1)]
        public static void GetNLUCompareResultsTextStatistics(
            string expected,
            string actual,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var expectedUtterance = new LabeledUtterance(expected, null, null).WithProperty("speechFile", "speechFile");
            var actualUtterance = new LabeledUtterance(actual, null, null);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.Statistics.Text.TruePositive.Should().Be(truePositive);
            compareResults.Statistics.Text.TrueNegative.Should().Be(trueNegative);
            compareResults.Statistics.Text.FalsePositive.Should().Be(falsePositive);
            compareResults.Statistics.Text.FalseNegative.Should().Be(falseNegative);
        }

        [Test]
        [TestCase("foo", "foo", 1, 0, 0, 0)]
        [TestCase(null, null, 0, 1, 0, 0)]
        [TestCase(null, "None", 0, 1, 0, 0)]
        [TestCase("None", null, 0, 1, 0, 0)]
        [TestCase(null, "foo", 0, 0, 1, 0)]
        [TestCase("None", "foo", 0, 0, 1, 0)]
        [TestCase("foo", "bar", 0, 0, 1, 1)]
        [TestCase("foo", null, 0, 0, 0, 1)]
        [TestCase("foo", "None", 0, 0, 0, 1)]
        public static void GetNLUCompareResultsIntentStatistics(
            string expected,
            string actual,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var expectedUtterance = new LabeledUtterance(null, expected, null);
            var actualUtterance = new LabeledUtterance(null, actual, null);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.Statistics.Intent.TruePositive.Should().Be(truePositive);
            compareResults.Statistics.Intent.TrueNegative.Should().Be(trueNegative);
            compareResults.Statistics.Intent.FalsePositive.Should().Be(falsePositive);
            compareResults.Statistics.Intent.FalseNegative.Should().Be(falseNegative);
            if (expected != null && expected != "None")
            {
                compareResults.Statistics.ByIntent[expected].TruePositive.Should().Be(truePositive);
                compareResults.Statistics.ByIntent[expected].FalseNegative.Should().Be(falseNegative);
            }
            else if (actual != null && actual != "None")
            {
                compareResults.Statistics.ByIntent[actual].FalsePositive.Should().Be(falsePositive);
            }
        }

        [Test]
        [TestCase("foo", "foo", 1, 0, 0, 0)]
        [TestCase(null, null, 0, 1, 0, 0)]
        [TestCase(null, "foo", 0, 0, 1, 0)]
        [TestCase("foo", "bar", 0, 0, 1, 1)]
        [TestCase("foo", null, 0, 0, 0, 1)]
        public static void GetNLUCompareResultsEntityStatistics(
            string expected,
            string actual,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var entityType = Guid.NewGuid().ToString();
            var expectedEntity = expected != null ? new[] { new Entity(entityType, null, expected, 0) } : null;
            var actualEntity = actual != null ? new[] { new Entity(entityType, null, actual, 0) } : null;
            var expectedUtterance = new LabeledUtterance(null, null, expectedEntity);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.Statistics.Entity.TruePositive.Should().Be(truePositive);
            compareResults.Statistics.Entity.TrueNegative.Should().Be(trueNegative);
            compareResults.Statistics.Entity.FalsePositive.Should().Be(falsePositive);
            compareResults.Statistics.Entity.FalseNegative.Should().Be(falseNegative);
            if (expected != null || actual != null)
            {
                compareResults.Statistics.ByEntityType[entityType].TruePositive.Should().Be(truePositive);
                compareResults.Statistics.ByEntityType[entityType].TrueNegative.Should().Be(trueNegative);
                compareResults.Statistics.ByEntityType[entityType].FalsePositive.Should().Be(falsePositive);
                compareResults.Statistics.ByEntityType[entityType].FalseNegative.Should().Be(falseNegative);
            }
        }

        [Test]
        [TestCase("foo", "foo", 1, 0, 0, 0)]
        [TestCase(null, null, 0, 0, 0, 0)]
        [TestCase(null, "foo", 0, 0, 0, 0)]
        [TestCase("foo", "bar", 0, 0, 0, 1)]
        [TestCase("foo", null, 0, 0, 0, 1)]
        public static void GetNLUCompareResultsEntityValueStatistics(
            string expected,
            string actual,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var entityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var expectedJson = expected == null ? JValue.CreateNull() : (JValue)expected;
            var actualJson = actual == null ? JValue.CreateNull() : (JValue)actual;
            var expectedEntity = new[] { new Entity(entityType, expectedJson, matchText, 0) };
            var actualEntity = new[] { new Entity(entityType, actualJson, matchText, 0) };
            var expectedUtterance = new LabeledUtterance(null, null, expectedEntity);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.Statistics.EntityValue.TruePositive.Should().Be(truePositive);
            compareResults.Statistics.EntityValue.TrueNegative.Should().Be(trueNegative);
            compareResults.Statistics.EntityValue.FalsePositive.Should().Be(falsePositive);
            compareResults.Statistics.EntityValue.FalseNegative.Should().Be(falseNegative);
            if (expected != null)
            {
                compareResults.Statistics.ByEntityValueType[entityType].TruePositive.Should().Be(truePositive);
                compareResults.Statistics.ByEntityValueType[entityType].TrueNegative.Should().Be(trueNegative);
                compareResults.Statistics.ByEntityValueType[entityType].FalsePositive.Should().Be(falsePositive);
                compareResults.Statistics.ByEntityValueType[entityType].FalseNegative.Should().Be(falseNegative);
            }
        }

        [Test]
        public static void GetNLUCompareResultsFalsePositiveEntityDifferentType()
        {
            var expectedEntityType = Guid.NewGuid().ToString();
            var actualEntityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var expectedEntity = new[] { new Entity(expectedEntityType, null, matchText, 0) };
            var actualEntity = new[] { new Entity(actualEntityType, null, matchText, 0) };
            var expectedUtterance = new LabeledUtterance(null, null, expectedEntity);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.Statistics.Entity.TruePositive.Should().Be(0);
            compareResults.Statistics.Entity.TrueNegative.Should().Be(0);
            compareResults.Statistics.Entity.FalsePositive.Should().Be(1);
            compareResults.Statistics.Entity.FalseNegative.Should().Be(1);
            compareResults.Statistics.ByEntityType[expectedEntityType].TruePositive.Should().Be(0);
            compareResults.Statistics.ByEntityType[expectedEntityType].TrueNegative.Should().Be(0);
            compareResults.Statistics.ByEntityType[expectedEntityType].FalsePositive.Should().Be(0);
            compareResults.Statistics.ByEntityType[expectedEntityType].FalseNegative.Should().Be(1);
            compareResults.Statistics.ByEntityType[actualEntityType].TruePositive.Should().Be(0);
            compareResults.Statistics.ByEntityType[actualEntityType].TrueNegative.Should().Be(0);
            compareResults.Statistics.ByEntityType[actualEntityType].FalsePositive.Should().Be(1);
            compareResults.Statistics.ByEntityType[actualEntityType].FalseNegative.Should().Be(0);
        }

        [Test]
        public static void GetNLUCompareResultsNullScores()
        {
            var entityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var expectedEntity = new[] { new Entity(entityType, null, matchText, 0) };
            var actualEntity = new[] { new Entity(entityType, null, matchText, 0) };
            var expectedUtterance = new LabeledUtterance(null, null, expectedEntity);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.TestCases.Select(t => t.Score).Any(score => score != null).Should().BeFalse();
        }

        [Test]
        public static void GetNLUCompareResultsExtractsIntentAndTextScore()
        {
            var expectedUtterance = new LabeledUtterance(null, null, null).WithProperty("speechFile", "speechFile");
            var actualUtterance = new LabeledUtterance(null, null, null).WithScore(0.5).WithTextScore(0.1);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            var intentTestCase = compareResults.TestCases.FirstOrDefault(t => t.TargetKind == ComparisonTargetKind.Intent);
            intentTestCase.Should().NotBeNull();
            intentTestCase.Score.Should().Be(0.5);
            var textTestCase = compareResults.TestCases.FirstOrDefault(t => t.TargetKind == ComparisonTargetKind.Text);
            textTestCase.Should().NotBeNull();
            textTestCase.Score.Should().Be(0.1);
        }

        [Test]
        public static void GetNLUCompareResultsExtractsEntityScore()
        {
            var entityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var expectedEntity = new[] { new Entity(entityType, null, matchText, 0) };
            var actualEntity = new[] { new Entity(entityType, null, matchText, 0).WithScore(0.5) };
            var expectedUtterance = new LabeledUtterance(null, null, expectedEntity);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            var testCase = compareResults.TestCases.FirstOrDefault(t => t.TargetKind == ComparisonTargetKind.Entity);
            testCase.Should().NotBeNull();
            testCase.Score.Should().Be(0.5);
        }

        [Test]
        public static void GetNLUCompareResultsExtractsFalsePositiveEntityScore()
        {
            var entityType = Guid.NewGuid().ToString();
            var matchText = Guid.NewGuid().ToString();
            var actualEntity = new[] { new Entity(entityType, null, matchText, 0).WithScore(0.5) };
            var expectedUtterance = new LabeledUtterance(null, null, null);
            var actualUtterance = new LabeledUtterance(null, null, actualEntity);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            var testCase = compareResults.TestCases.FirstOrDefault(t => t.TargetKind == ComparisonTargetKind.Entity);
            testCase.Should().NotBeNull();
            testCase.Score.Should().Be(0.5);
        }

        [Test]
        public static void UsesIndexAsUtteranceId()
        {
            var expectedUtterance = new LabeledUtterance(null, "Greeting", null);
            var actualUtterance = new LabeledUtterance(null, "Greeting", null);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance, expectedUtterance },
                new[] { actualUtterance, actualUtterance },
                GetDefaultTestSettings());
            compareResults.TestCases.Count.Should().Be(4);
            compareResults.TestCases.Where(t => t.UtteranceId == "0").Count().Should().Be(2);
            compareResults.TestCases.Where(t => t.UtteranceId == "1").Count().Should().Be(2);
        }

        [Test]
        public static void UsesInputUtteranceId()
        {
            var utteranceId = Guid.NewGuid().ToString();
            var expectedUtterance = new LabeledUtterance(null, "Greeting", null);
            expectedUtterance.AdditionalProperties.Add("utteranceId", utteranceId);
            var actualUtterance = new LabeledUtterance(null, "Greeting", null);
            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                GetDefaultTestSettings());
            compareResults.TestCases.Count.Should().Be(2);
            compareResults.TestCases.Where(t => t.UtteranceId == utteranceId).Count().Should().Be(2);
        }

        [Test]
        [TestCase("x", "x", "", false, 1, 0, 0, 0)]
        [TestCase("a", "x", "", false, 0, 0, 1, 1)]
        [TestCase("a", "b", "", false, 0, 0, 0, 1)]
        [TestCase("a", "d", "", false, 0, 0, 0, 1)]
        [TestCase("a", "f", "", false, 0, 0, 1, 1)]
        [TestCase("x", "x", "foo", false, 0, 0, 1, 1)]
        [TestCase("a", "a", "foo", false, 0, 0, 1, 1)]
        [TestCase("b", "b", "foo", false, 0, 0, 0, 1)]
        [TestCase("f", "f", "foo", false, 0, 0, 1, 1)]
        [TestCase("x", "x", "", true, 1, 0, 0, 0)]
        [TestCase("a", "x", "", true, 0, 0, 0, 1)]
        [TestCase("a", "b", "", true, 0, 0, 0, 1)]
        [TestCase("a", "d", "", true, 0, 0, 0, 1)]
        [TestCase("a", "e", "", true, 0, 0, 0, 1)]
        [TestCase("x", "x", "foo", true, 0, 0, 0, 1)]
        [TestCase("a", "a", "foo", true, 0, 0, 1, 1)]
        [TestCase("b", "b", "foo", true, 0, 0, 0, 1)]
        [TestCase("e", "e", "foo", true, 0, 0, 0, 1)]
        public static void GlobalLocalStrictIgnoreEntities(
            string expected,
            string actual,
            string value,
            bool unitTestMode,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var globalSettingsFile = Guid.NewGuid().ToString();
            var globalSettings = new JObject
            {
                { "strictEntities", new JArray { "c", "e" } },
                { "ignoreEntities", new JArray { "d", "f" } },
            };

            File.WriteAllText(globalSettingsFile, globalSettings.ToString());

            try
            {
                var expectedEntity = new Entity(expected, null, string.Empty, 0);
                var actualEntity = new Entity(actual, null, value, 0);
                var expectedUtterance = new LabeledUtterance(null, null, new[] { expectedEntity })
                    .WithProperty("strictEntities", new JArray { "a", "f" })
                    .WithProperty("ignoreEntities", new JArray { "b", "e" });
                var actualUtterance = new LabeledUtterance(null, null, new[] { actualEntity });
                var testSettings = new TestSettings(globalSettingsFile, unitTestMode);

                var compareResults = TestCaseSource.GetNLUCompareResults(
                    new[] { expectedUtterance },
                    new[] { actualUtterance },
                    testSettings);

                compareResults.Statistics.Entity.TruePositive.Should().Be(truePositive);
                compareResults.Statistics.Entity.TrueNegative.Should().Be(trueNegative);
                compareResults.Statistics.Entity.FalsePositive.Should().Be(falsePositive);
                compareResults.Statistics.Entity.FalseNegative.Should().Be(falseNegative);
            }
            finally
            {
                File.Delete(globalSettingsFile);
            }
        }

        [Test]
        public static void NoFalsePositiveIntentsUnitTestMode()
        {
            var expectedUtterance = new JsonLabeledUtterance(new JsonEntities(Array.Empty<HierarchicalEntity>()));
            var actualUtterance = new LabeledUtterance(null, "foo", null);
            var testSettings = new TestSettings(default(string), true);

            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                testSettings);

            compareResults.Statistics.Intent.TruePositive.Should().Be(0);
            compareResults.Statistics.Intent.TrueNegative.Should().Be(0);
            compareResults.Statistics.Intent.FalsePositive.Should().Be(0);
            compareResults.Statistics.Intent.FalseNegative.Should().Be(0);
        }

        [Test]
        public static void HasFalsePositiveIntentsUnitTestMode()
        {
            var jsonEntities = new JsonEntities(Array.Empty<HierarchicalEntity>())
            {
                AdditionalProperties =
                {
                    { "intent", null },
                },
            };

            var expectedUtterance = new JsonLabeledUtterance(jsonEntities);
            var actualUtterance = new LabeledUtterance(null, "foo", null);
            var testSettings = new TestSettings(default(string), true);

            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                testSettings);

            compareResults.Statistics.Intent.TruePositive.Should().Be(0);
            compareResults.Statistics.Intent.TrueNegative.Should().Be(0);
            compareResults.Statistics.Intent.FalsePositive.Should().Be(1);
            compareResults.Statistics.Intent.FalseNegative.Should().Be(0);
        }

        [Test]
        [TestCase("foo", "foo", 0, 1, 0, 0)]
        [TestCase(null, null, 0, 1, 0, 0)]
        [TestCase(null, "None", 0, 0, 1, 0)]
        [TestCase("None", null, 0, 0, 0, 1)]
        [TestCase(null, "foo", 0, 1, 0, 0)]
        [TestCase("None", "foo", 0, 0, 0, 1)]
        [TestCase("foo", "bar", 0, 0, 1, 0)]
        [TestCase("foo", null, 0, 1, 0, 0)]
        [TestCase("foo", "None", 0, 0, 1, 0)]
        public static void CustomTrueNegativeIntent(
            string expected,
            string actual,
            int truePositive,
            int trueNegative,
            int falsePositive,
            int falseNegative)
        {
            var expectedUtterance = new LabeledUtterance(null, expected, null);
            var actualUtterance = new LabeledUtterance(null, actual, null);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "trueNegativeIntent", "foo" },
                })
                .Build();

            var testSettings = new TestSettings(configuration, false);

            var compareResults = TestCaseSource.GetNLUCompareResults(
                new[] { expectedUtterance },
                new[] { actualUtterance },
                testSettings);
            compareResults.Statistics.Intent.TruePositive.Should().Be(truePositive);
            compareResults.Statistics.Intent.TrueNegative.Should().Be(trueNegative);
            compareResults.Statistics.Intent.FalsePositive.Should().Be(falsePositive);
            compareResults.Statistics.Intent.FalseNegative.Should().Be(falseNegative);
            if (expected != null && expected != "foo")
            {
                compareResults.Statistics.ByIntent[expected].TruePositive.Should().Be(truePositive);
                compareResults.Statistics.ByIntent[expected].FalseNegative.Should().Be(falseNegative);
            }
            else if (actual != null && actual != "foo")
            {
                compareResults.Statistics.ByIntent[actual].FalsePositive.Should().Be(falsePositive);
            }
        }

        private static List<Entity> CreateEntityList(string type)
        {
            return new List<Entity> { new Entity(type, "EntityValue", "matchedText", 1) };
        }

        private static bool Matches(string input, string regEx)
        {
            return Regex.IsMatch(input, regEx);
        }

        private static JToken ParseEntityValueJson(string json)
        {
            return json != null ? JToken.Parse(json) : null;
        }

        private static TestCaseSource.LabeledUtteranceTestInput CreatePair(params ILabeledUtterance[] pair)
        {
            return new TestCaseSource.LabeledUtteranceTestInput(string.Empty, pair[0], pair[1], GetDefaultTestSettings());
        }

        private static TestSettings GetDefaultTestSettings()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "trueNegativeIntent", "None" },
                })
                .Build();

            return new TestSettings(configuration, false);
        }
    }
}
