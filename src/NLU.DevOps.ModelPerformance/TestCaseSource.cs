﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    internal static class TestCaseSource
    {
        private const string AppSettingsPath = "appsettings.json";
        private const string AppSettingsLocalPath = "appsettings.local.json";

        public static IEnumerable<TestCaseData> PassingTests => TestCases
            .Where(IsTrue)
            .Select(ToTestCaseData);

        public static IEnumerable<TestCaseData> FailingTests => TestCases
            .Where(IsFalse)
            .Select(ToTestCaseData);

        /// <summary>
        /// Gets the test label.
        /// </summary>
        /// <remarks>
        /// The test label is useful for discriminating between tests, e.g., for audio and text.
        /// </remarks>
        internal static string TestLabel => TestContext.Parameters.Get(ConfigurationConstants.TestLabelKey) ?? Configuration[ConfigurationConstants.TestLabelKey];

        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsPath, true)
            .AddJsonFile(AppSettingsLocalPath, true)
            .AddEnvironmentVariables()
            .Build();

        private static IReadOnlyList<TestCase> TestCases { get; } = LoadTestCases();

        internal static TestCase ToTextTestCase(LabeledUtterance[] utterances)
        {
            var expected = utterances[0].Text;
            var actual = utterances[1].Text;

            if (expected == null && actual == null)
            {
                return TrueNegative(
                    $"TrueNegativeText",
                    "Both utterances are 'null'.",
                    "Text");
            }

            if (actual == null)
            {
                return FalseNegative(
                    $"FalseNegativeText('{expected}')",
                    $"Actual text is 'null', expected '{expected}'",
                    "Text");
            }

            if (EqualsNormalized(expected, actual))
            {
                return TruePositive(
                    $"TruePositiveText('{expected}')",
                    "Utterances have matching text.",
                    "Text");
            }

            return FalsePositive(
                $"FalsePositiveText('{expected}', '{actual}')",
                $"Expected text '{expected}', actual text '{actual}'.",
                "Text");
        }

        internal static TestCase ToIntentTestCase(LabeledUtterance[] utterances)
        {
            var text = utterances[0].Text;
            var expected = utterances[0].Intent;
            var actual = utterances[1].Intent;

            if (actual == null || actual == "None")
            {
                return expected == null || expected == "None"
                    ? TrueNegative(
                        $"TrueNegativeIntent('{text}')",
                        "Both intents are 'None'.",
                        "Intent")
                    : FalseNegative(
                        $"FalseNegativeIntent('{expected}', '{text}')",
                        $"Actual intent is 'None', expected '{expected}'",
                        "Intent");
            }

            if (expected == actual)
            {
                return TruePositive(
                    $"TruePositiveIntent('{expected}', '{text}')",
                    "Utterances have matching intent.",
                    "Intent");
            }

            return FalsePositive(
                $"FalsePositiveIntent('{expected}', '{actual}', '{text}')",
                $"Expected intent '{expected}', actual intent '{actual}'.",
                "Intent");
        }

        internal static IEnumerable<TestCase> ToEntityTestCases(LabeledUtterance[] utterances)
        {
            var text = utterances[0].Text;
            var expected = utterances[0].Entities;
            var actual = utterances[1].Entities;

            if ((expected == null || expected.Count == 0) && (actual == null || actual.Count == 0))
            {
                yield return TrueNegative(
                    $"TrueNegativeEntity('{text}')",
                    "Neither utterances have entities.",
                    "Entity");

                yield break;
            }

            bool isEntityMatch(Entity expectedEntity, Entity actualEntity)
            {
                return expectedEntity.EntityType == actualEntity.EntityType
                    && ((EqualsNormalized(expectedEntity.MatchText, actualEntity.MatchText)
                    && expectedEntity.MatchIndex == actualEntity.MatchIndex)
                    /* Required case to support NLU providers that do not specify matched text */
                    || EqualsNormalizedJson(expectedEntity.MatchText, actualEntity.EntityValue)
                    || EqualsNormalizedJson(expectedEntity.EntityValue, actualEntity.EntityValue)
                    /* Required case to support FalsePositiveEntity scenarios */
                    || EqualsNormalizedJson(expectedEntity.EntityValue, actualEntity.MatchText));
            }

            if (expected != null)
            {
                foreach (var entity in expected)
                {
                    // "Soft" entity match test cases
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    if (actual == null || !actual.Any(actualEntity => isEntityMatch(entity, actualEntity)))
                    {
                        yield return FalseNegative(
                            $"FalseNegativeEntity('{entity.EntityType}', '{entityValue}', '{text}')",
                            $"Actual utterance does not have entity matching '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }
                    else
                    {
                        yield return TruePositive(
                            $"TruePositiveEntity('{entity.EntityType}', '{entityValue}', '{text}')",
                            $"Both utterances have entity '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }

                    if (entity.EntityValue != null && entity.EntityValue.Type != JTokenType.Null)
                    {
                        // "Semantic" entity value match test cases
                        bool isEntityValueMatch(Entity actualEntity)
                        {
                            return entity.EntityType == actualEntity.EntityType
                                && ContainsSubtree(entity.EntityValue, actualEntity.EntityValue);
                        }

                        var formattedEntityValue = entity.EntityValue.ToString(Formatting.None);
                        if (actual == null || !actual.Any(isEntityValueMatch))
                        {
                            yield return FalseNegative(
                                $"FalseNegativeEntityValue('{entity.EntityType}', '{formattedEntityValue}', '{text}')",
                                $"Actual utterance does not have entity value matching '{formattedEntityValue}'.",
                                "Entity",
                                entity.EntityType);
                        }
                        else
                        {
                            yield return TruePositive(
                                 $"TruePositiveEntityValue('{entity.EntityType}', '{formattedEntityValue}', '{text}')",
                                 $"Both utterances have entity value '{formattedEntityValue}'.",
                                 "Entity",
                                 entity.EntityType);
                        }
                    }

                    if (entity.EntityResolution != null && entity.EntityResolution.Type != JTokenType.Null)
                    {
                        bool isEntityResolutionMatch(Entity actualEntity)
                        {
                            return ContainsSubtree(entity.EntityResolution, actualEntity.EntityResolution);
                        }

                        var formattedEntityResolution = entity.EntityResolution.ToString(Formatting.None);
                        if (actual == null || !actual.Any(isEntityResolutionMatch))
                        {
                            yield return FalseNegative(
                                $"FalseNegativeEntityResolution('{entity.EntityType}', '{formattedEntityResolution}', '{text}')",
                                $"Actual utterance does not have entity resolution matching '{formattedEntityResolution}'.",
                                "Entity",
                                entity.EntityType);
                        }
                        else
                        {
                            yield return TruePositive(
                            $"TruePositiveEntityResolution('{entity.EntityType}', '{formattedEntityResolution}', '{text}')",
                                $"Both utterances contain expected resolution '{formattedEntityResolution}'.",
                                "Entity",
                                entity.EntityType);
                        }
                    }
                }
            }

            if (actual != null)
            {
                foreach (var entity in actual)
                {
                    var entityValue = entity.MatchText ?? entity.EntityValue;
                    if (expected == null || !expected.Any(expectedEntity => isEntityMatch(entity, expectedEntity)))
                    {
                        yield return FalsePositive(
                            $"FalsePositiveEntity('{entity.EntityType}', '{entityValue}, '{text}')",
                            $"Expected utterance does not have entity matching '{entityValue}'.",
                            "Entity",
                            entity.EntityType);
                    }
                }
            }
        }

        private static TestCaseData ToTestCaseData(this TestCase testCase)
        {
            var testCaseData = new TestCaseData(testCase.Because)
            {
                TestName = testCase.TestName,
            };

            testCase.Categories.ForEach(category => testCaseData.SetCategory(category));
            return testCaseData;
        }

        private static bool IsTrue(this TestCase testCase)
        {
            return testCase.Kind == TestResultKind.TruePositive
                || testCase.Kind == TestResultKind.TrueNegative;
        }

        private static bool IsFalse(this TestCase testCase)
        {
            return testCase.Kind == TestResultKind.FalsePositive
                || testCase.Kind == TestResultKind.FalseNegative;
        }

        private static IReadOnlyList<TestCase> LoadTestCases()
        {
            var expectedPath = TestContext.Parameters.Get(ConfigurationConstants.ExpectedUtterancesPathKey) ?? Configuration[ConfigurationConstants.ExpectedUtterancesPathKey];
            var actualPath = TestContext.Parameters.Get(ConfigurationConstants.ActualUtterancesPathKey) ?? Configuration[ConfigurationConstants.ActualUtterancesPathKey];

            if (string.IsNullOrEmpty(expectedPath) || string.IsNullOrEmpty(actualPath))
            {
                throw new InvalidOperationException("Could not find configuration for expected or actual utterances.");
            }

            var expectedUtterances = Read(expectedPath);
            var actualUtterances = Read(actualPath);

            if (expectedUtterances.Count != actualUtterances.Count)
            {
                throw new InvalidOperationException("Expected the same number of utterances in the expected and actual sources.");
            }

            var zippedUtterances = expectedUtterances
                .Zip(actualUtterances, (expected, actual) => new[] { expected, actual })
                .ToList();

            return zippedUtterances.Select(ToTextTestCase)
                .Concat(zippedUtterances.Select(ToIntentTestCase))
                .Concat(zippedUtterances.SelectMany(ToEntityTestCases))
                .ToList();
        }

        private static List<LabeledUtterance> Read(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<List<LabeledUtterance>>(jsonReader);
            }
        }

        private static bool EqualsNormalizedJson(JToken x, JToken y)
        {
            // Entity is not a match if both values are null
            if (x == null && y == null)
            {
                return false;
            }

            return x?.Type == JTokenType.String
                ? EqualsNormalizedJson(x.Value<string>(), y)
                : false;
        }

        private static bool EqualsNormalizedJson(string x, JToken y)
        {
            // Entity is not a match if both values are null
            if (x == null && y == null)
            {
                return false;
            }

            return y?.Type == JTokenType.String
                ? EqualsNormalized(x, y.Value<string>())
                : false;
        }

        private static bool EqualsNormalized(string x, string y)
        {
            string normalize(string s)
            {
                if (s == null)
                {
                    return null;
                }

                var normalizedSpace = Regex.Replace(s, @"\s+", " ");
                var withoutPunctuation = Regex.Replace(normalizedSpace, @"[^\w ]", string.Empty);
                return withoutPunctuation.Trim();
            }

            return string.Equals(normalize(x), normalize(y), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsSubtree(JToken expected, JToken actual)
        {
            if (expected == null)
            {
                return true;
            }

            if (actual == null)
            {
                return false;
            }

            switch (expected)
            {
                case JObject expectedObject:
                    var actualObject = actual as JObject;
                    if (actualObject == null)
                    {
                        return false;
                    }

                    foreach (var expectedProperty in expectedObject.Properties())
                    {
                        var actualProperty = actualObject.Property(expectedProperty.Name, StringComparison.Ordinal);
                        if (!ContainsSubtree(expectedProperty.Value, actualProperty?.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                case JArray expectedArray:
                    var actualArray = actual as JArray;
                    if (actualArray == null)
                    {
                        return false;
                    }

                    foreach (var expectedItem in expectedArray)
                    {
                        // Order is not asserted
                        if (!actualArray.Any(actualItem => ContainsSubtree(expectedItem, actualItem)))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return JToken.DeepEquals(expected, actual);
            }
        }

        private static TestCase TruePositive(string message, string because, params string[] categories)
        {
            return new TestCase(TestResultKind.TruePositive, message, because, categories.Append("TruePositive"));
        }

        private static TestCase TrueNegative(string message, string because, params string[] categories)
        {
            return new TestCase(TestResultKind.TrueNegative, message, because, categories.Append("TrueNegative"));
        }

        private static TestCase FalsePositive(string message, string because, params string[] categories)
        {
            return new TestCase(TestResultKind.FalsePositive, message, because, categories.Append("FalsePositive"));
        }

        private static TestCase FalseNegative(string message, string because, params string[] categories)
        {
            return new TestCase(TestResultKind.FalseNegative, message, because, categories.Append("FalseNegative"));
        }
    }
}
