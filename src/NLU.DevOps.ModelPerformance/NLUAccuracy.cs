﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ConsoleTables;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class NLUAccuracy
    {
        /// <summary>
        /// Prints to the console the intents, entities performance results
        /// and a confusion table for intents
        /// </summary>
        /// <param name="compareResults"> The comparison results for intents and entities</param>
        /// <param name="baseline">The baseline results the results are benchmarked against.</param>
        public static void PrintResults(this NLUCompareResults compareResults, NLUStatistics baseline)
        {
            var allIntentsPrecision = Print(compareResults.Statistics.Intent.Precision(), baseline?.Intent.Precision());
            var allIntentsRecall = Print(compareResults.Statistics.Intent.Recall(), baseline?.Intent.Recall());
            var allIntentsF1 = Print(compareResults.Statistics.Intent.F1(), baseline?.Intent.F1());
            var allIntentsTotal = Print(compareResults.Statistics.Intent.Total(), baseline?.Intent.Total(), 0);
            var allIntentsFP = Print(compareResults.Statistics.Intent.FalsePositive, baseline?.Intent.FalsePositive, 0);

            Console.WriteLine("## Intents Results");

            var intentTable = new ConsoleTable("Intent", "Precision", "Recall", "F1", "Total", "FP");
            intentTable.AddRow("*", allIntentsPrecision, allIntentsRecall, allIntentsF1, allIntentsTotal, allIntentsFP);

            compareResults.Statistics.ByIntent
                .OrderBy(intentItem => intentItem.Key)
                .ToList()
                .ForEach(intentItem =>
                {
                    var baselineResults = default(ConfusionMatrix);
                    if (baseline != null && !baseline.ByIntent.TryGetValue(intentItem.Key, out baselineResults))
                    {
                        baselineResults = ConfusionMatrix.Default;
                    }

                    var intentPrecision = Print(intentItem.Value.Precision(), baselineResults?.Precision());
                    var intentRecall = Print(intentItem.Value.Recall(), baselineResults?.Recall());
                    var intentF1 = Print(intentItem.Value.F1(), baselineResults?.F1());
                    var intentTotal = Print(intentItem.Value.Total(), baselineResults?.Total(), 0);
                    var intentFP = Print(intentItem.Value.FalsePositive, baselineResults?.FalsePositive, 0);
                    intentTable.AddRow(intentItem.Key, intentPrecision, intentRecall, intentF1, intentTotal, intentFP);
                });

            intentTable.Write(Format.MarkDown);

            var allEntitiesPrecision = Print(compareResults.Statistics.Entity.Precision(), baseline?.Entity.Precision());
            var allEntitiesRecall = Print(compareResults.Statistics.Entity.Recall(), baseline?.Entity.Recall());
            var allEntitiesF1 = Print(compareResults.Statistics.Entity.F1(), baseline?.Entity.F1());
            var allEntitiesTotal = Print(compareResults.Statistics.Entity.Total(), baseline?.Entity.Total(), 0);
            var allEntitiesFP = Print(compareResults.Statistics.Entity.FalsePositive, baseline?.Entity.FalsePositive, 0);

            Console.WriteLine("## Entity Results");

            var entityTable = new ConsoleTable("Entity", "Precision", "Recall", "F1", "Total", "FP");
            entityTable.AddRow("*", allEntitiesPrecision, allEntitiesRecall, allEntitiesF1, allEntitiesTotal, allEntitiesFP);

            compareResults.Statistics.ByEntityType
                .OrderBy(entityItem => entityItem.Key)
                .ToList()
                .ForEach(entityItem =>
                {
                    var baselineResults = default(ConfusionMatrix);
                    if (baseline != null && !baseline.ByEntityType.TryGetValue(entityItem.Key, out baselineResults))
                    {
                        baselineResults = ConfusionMatrix.Default;
                    }

                    var entityPrecision = Print(entityItem.Value.Precision(), baselineResults?.Precision());
                    var entityRecall = Print(entityItem.Value.Recall(), baselineResults?.Recall());
                    var entityF1 = Print(entityItem.Value.F1(), baselineResults?.F1());
                    var entityTotal = Print(entityItem.Value.Total(), baselineResults?.Total(), 0);
                    var entityFP = Print(entityItem.Value.FalsePositive, baselineResults?.FalsePositive, 0);
                    entityTable.AddRow(entityItem.Key, entityPrecision, entityRecall, entityF1, entityTotal, entityFP);
                });

            entityTable.Write(Format.MarkDown);

            PrintIntentConfusionTable(compareResults.TestCases);
        }

        /// <summary>
        /// Calculates the precision from a confusion matrix.
        /// </summary>
        /// <param name="matrix">Confusion matrix.</param>
        /// <returns>Precision value.</returns>
        internal static double Precision(this ConfusionMatrix matrix)
        {
            return Divide(matrix.TruePositive, matrix.TruePositive + matrix.FalsePositive);
        }

        /// <summary>
        /// Calculates the recall from a confusion matrix.
        /// </summary>
        /// <param name="matrix">Confusion matrix.</param>
        /// <returns>Recall value.</returns>
        internal static double Recall(this ConfusionMatrix matrix)
        {
            return Divide(matrix.TruePositive, matrix.TruePositive + matrix.FalseNegative);
        }

        /// <summary>
        /// Calculates the F<sub>1</sub> score from a confusion matrix.
        /// </summary>
        /// <param name="matrix">Confusion matrix.</param>
        /// <returns>F<sub>1</sub> score.</returns>
        internal static double F1(this ConfusionMatrix matrix)
        {
            var precision = matrix.Precision();
            var recall = matrix.Recall();
            var denominator = precision + recall;
            return Math.Abs(denominator) > double.Epsilon ? 2 * (precision * recall) / denominator : 0;
        }

        /// <summary>
        /// Calculates total count for the confusion matrix.
        /// </summary>
        /// <param name="matrix">Confusion matrix.</param>
        /// <returns>Total count.</returns>
        internal static int Total(this ConfusionMatrix matrix)
        {
            return matrix.TruePositive + matrix.TrueNegative + matrix.FalsePositive + matrix.FalseNegative;
        }

        /// <summary>
        /// Divides the dividend input by the divisor.
        /// </summary>
        /// <param name="dividend"> Dividend in the division.</param>
        /// <param name="divisor"> Divisor in the division.</param>
        /// <returns>Division result.</returns>
        private static double Divide(double dividend, int divisor)
        {
            return divisor != 0 ? dividend / divisor : 0;
        }

        /// <summary>
        /// Prints the confusion table for intents.
        /// </summary>
        /// <param name="testCases"> Test cases.</param>
        private static void PrintIntentConfusionTable(IReadOnlyList<TestCase> testCases)
        {
            bool isFalsePositiveIntent(TestCase testCase)
            {
                return testCase.TargetKind == ComparisonTargetKind.Intent
                    && testCase.ResultKind == ConfusionMatrixResultKind.FalsePositive;
            }

            (string Expected, string Actual) keySelector(TestCase testCase)
            {
                return (testCase.ExpectedUtterance.Intent, testCase.ActualUtterance.Intent);
            }

            var falsePositiveIntents = testCases
                .Where(isFalsePositiveIntent)
                .GroupBy(keySelector)
                .ToDictionary(group => group.Key, group => group.Count())
                .OrderByDescending(group => group.Value);

            Console.WriteLine("## Intent Confusion Matrix");

            var confusionMatrix = new ConsoleTable("Expected", "Actual", "FP");
            falsePositiveIntents.ToList().ForEach(kvp =>
            {
                confusionMatrix.AddRow(kvp.Key.Expected, kvp.Key.Actual, kvp.Value);
            });

            confusionMatrix.Write(Format.MarkDown);
        }

        /// <summary>
        /// Prints the current value and difference from the baseline.
        /// </summary>
        /// <param name="current">Current value.</param>
        /// <param name="baseline">Baseline value.</param>
        /// <param name="precision">Rounding precision.</param>
        /// <returns>Printed value and difference with baseline.</returns>
        private static string Print(double current, double? baseline, int precision = 3)
        {
            var format = $"0.{string.Join(string.Empty, Enumerable.Repeat("0", precision))}";
            return baseline.HasValue
                ? string.Format(CultureInfo.CurrentCulture, $"{{0:{format}}} ({{1:{format}}})", current, current - baseline)
                : string.Format(CultureInfo.CurrentCulture, $"{{0:{format}}}", current);
        }
    }
}
