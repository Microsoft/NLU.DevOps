﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// This class contains utils function for performance calculations
    /// and printing
    /// </summary>
    public static class NLUAccuracy
    {
       /// <summary>
       /// Divides the dividend input by the diviso
       /// </summary>
       /// <param name="dividend"> The dividend in the division</param>
       /// <param name="divisor"> The divisor in the division</param>
       /// <returns>The division result</returns>
        public static double Calculate(double dividend, double divisor)
        {
            return divisor != 0 ? dividend / divisor : 0;
        }

        /// <summary>
        /// Calculates the precision from a confusion matrix
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>The precision result</returns>
        public static double CalcPrecision(ConfusionMatrix cm)
        {
            return Calculate(cm.TruePositive, cm.TruePositive + cm.FalsePositive);
        }

        /// <summary>
        ///  Calculates the recall from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusin matrix metrics</param>
        /// <returns> The recall result</returns>
        public static double CalcRecall(ConfusionMatrix cm)
        {
            return Calculate(cm.TruePositive, cm.TruePositive + cm.FalseNegative);
        }

        /// <summary>
        /// Calculates the f1 score from a confusion matrix
        /// </summary>
        /// <param name="cm"> confusion matrix metrics</param>
        /// <returns> the f1 result</returns>
        public static double CalcF1(ConfusionMatrix cm)
        {
            var precision = CalcPrecision(cm);
            var recall = CalcRecall(cm);
            var denominator = precision + recall;
            return denominator != 0 ? 2 * (precision * recall) / denominator : 0;
        }

        /// <summary>
        /// Calculates Precision, Recall and F1 Score
        /// </summary>
        /// <param name="cm">confusion matrix metrics</param>
        /// <returns>List that contains the results calculated</returns>
        public static List<double> CalcMetrics(ConfusionMatrix cm)
        {
            List<double> metrics = new List<double>
            {
                CalcPrecision(cm),
                CalcRecall(cm),
                CalcF1(cm)
            };
            return metrics;
        }

        /// <summary>
        /// Prints to the console the intents and entities performance results in a table
        /// </summary>
        /// <param name="statistics"> The computed data for intents and entities</param>
        public static void PrintResults(NLUStatistics statistics)
        {
            Console.WriteLine("== Intents results == ");
            Console.WriteLine("Intent          | Precision | Recall    | F1        |");
            Console.WriteLine("=====================================================");
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-15} |", "*"));

            List<double> intentsTotalResults = NLUAccuracy.CalcMetrics(statistics.Intent);
            intentsTotalResults.ForEach(entry => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", Math.Round(entry, 4))));
            Console.WriteLine();

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByIntent)
            {
                Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-15} |", kvp.Key));
                NLUAccuracy.CalcMetrics(kvp.Value).ForEach(intent => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", Math.Round(intent, 4))));
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("== Entity results == ");
            Console.WriteLine("Entity            | Precision | Recall    | F1        |");
            Console.WriteLine("=======================================================");
            Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-17} |", "*"));

            List<double> entityTotalResults = NLUAccuracy.CalcMetrics(statistics.Entity);
            entityTotalResults.ForEach(entry => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", Math.Round(entry, 4))));
            Console.WriteLine();

            foreach (KeyValuePair<string, ConfusionMatrix> kvp in statistics.ByEntityType)
            {
                Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-17} |", kvp.Key));
                var entityResults = NLUAccuracy.CalcMetrics(kvp.Value);
                entityResults.ForEach(entity => Console.Out.Write(string.Format(CultureInfo.InvariantCulture, "{0,-10} |", Math.Round(entity, 4))));
                Console.WriteLine();
            }
        }
    }
}
