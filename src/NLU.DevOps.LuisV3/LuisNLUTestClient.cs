﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Test a LUIS model with text and speech.
    /// Implementation of <see cref="INLUTestClient"/>
    /// </summary>
    public sealed class LuisNLUTestClient : INLUTestClient
    {
        private const double Epsilon = 1e-6;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisNLUTestClient"/> class.
        /// </summary>
        /// <param name="luisSettings">LUIS settings.</param>
        /// <param name="luisClient">LUIS client.</param>
        public LuisNLUTestClient(LuisSettings luisSettings, ILuisTestClient luisClient)
        {
            this.LuisSettings = luisSettings ?? throw new ArgumentNullException(nameof(luisSettings));
            this.LuisClient = luisClient ?? throw new ArgumentNullException(nameof(luisClient));
        }

        private LuisSettings LuisSettings { get; }

        private ILuisTestClient LuisClient { get; }

        /// <inheritdoc />
        public async Task<LabeledUtterance> TestAsync(
            JToken query,
            CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var predictionRequest = query.ToObject<PredictionRequest>();
            predictionRequest.Query = predictionRequest.Query ?? query.Value<string>("text");
            var luisResult = await this.LuisClient.QueryAsync(predictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(new SpeechPredictionResponse(luisResult, 0));
        }

        /// <inheritdoc />
        public async Task<LabeledUtterance> TestSpeechAsync(
            string speechFile,
            JToken query,
            CancellationToken cancellationToken)
        {
            if (speechFile == null)
            {
                throw new ArgumentNullException(nameof(speechFile));
            }

            var predictionRequest = query?.ToObject<PredictionRequest>();
            if (predictionRequest != null)
            {
                predictionRequest.Query = predictionRequest.Query ?? query.Value<string>("text");
            }

            var luisResult = await this.LuisClient.RecognizeSpeechAsync(speechFile, predictionRequest, cancellationToken).ConfigureAwait(false);
            return this.LuisResultToLabeledUtterance(luisResult);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LuisClient.Dispose();
        }

        private static IEnumerable<Entity> GetEntities(string utterance, IDictionary<string, object> entities, IDictionary<string, string> mappedTypes)
        {
            if (entities == null)
            {
                return null;
            }

            Entity getEntity(string entityType, JToken entityJson, JToken entityMetadata)
            {
                var startIndex = entityMetadata.Value<int>("startIndex");
                var length = entityMetadata.Value<int>("length");
                var score = entityMetadata.Value<double?>("score");
                var matchText = utterance.Substring(startIndex, length);
                var matchIndex = 0;
                var currentStart = 0;
                var nextStart = 0;
                while ((nextStart = utterance.IndexOf(matchText, currentStart, StringComparison.Ordinal)) != startIndex)
                {
                    ++matchIndex;
                    currentStart = nextStart + 1;
                }

                var entityValue = PruneMetadata(entityJson);

                var modifiedEntityType = entityType;
                if (mappedTypes.TryGetValue(entityType, out var mappedEntityType))
                {
                    modifiedEntityType = mappedEntityType;
                }

                return score.HasValue
                    ? new PredictedEntity(modifiedEntityType, entityValue, matchText, matchIndex, score.Value)
                    : new Entity(modifiedEntityType, entityValue, matchText, matchIndex);
            }

            var instanceMetadata = default(JObject);
            if (entities.TryGetValue("$instance", out var instanceJson))
            {
                instanceMetadata = instanceJson as JObject;
            }

            return entities
                .Where(pair => pair.Key != "$instance")
                .Select(pair =>
                    new
                    {
                        EntityType = pair.Key,
                        Entities = ((JArray)pair.Value).Zip(
                            instanceMetadata?[pair.Key],
                            (entityValue, entityMetadata) =>
                                new
                                {
                                    EntityValue = entityValue,
                                    EntityMetadata = entityMetadata
                                })
                    })
                .SelectMany(entityInfo =>
                    entityInfo.Entities.Select(entity =>
                        getEntity(entityInfo.EntityType, entity.EntityValue, entity.EntityMetadata)));
        }

        private static JToken PruneMetadata(JToken json)
        {
            if (json is JObject jsonObject)
            {
                var prunedObject = new JObject();
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name != "$instance")
                    {
                        prunedObject.Add(property.Name, PruneMetadata(property.Value));
                    }
                }

                return prunedObject;
            }

            return json;
        }

        private LabeledUtterance LuisResultToLabeledUtterance(SpeechPredictionResponse speechPredictionResponse)
        {
            if (speechPredictionResponse == null)
            {
                return new LabeledUtterance(null, null, null);
            }

            var mappedTypes = this.LuisSettings.PrebuiltEntityTypes
                .ToDictionary(pair => $"builtin.{pair.Value}", pair => pair.Key);

            var intent = speechPredictionResponse.PredictionResponse.Prediction.TopIntent;
            var entities = GetEntities(
                    speechPredictionResponse.PredictionResponse.Query,
                    speechPredictionResponse.PredictionResponse.Prediction.Entities,
                    mappedTypes)?
                .ToList();

            var query = speechPredictionResponse.PredictionResponse.Query;
            var intentData = default(Intent);
            speechPredictionResponse.PredictionResponse.Prediction.Intents?.TryGetValue(intent, out intentData);
            var context = LabeledUtteranceContext.CreateDefault();
            return (intentData != null && intentData.Score.HasValue) || Math.Abs(speechPredictionResponse.TextScore) > Epsilon
                ? new PredictedLabeledUtterance(query, intent, intentData?.Score ?? 0, speechPredictionResponse.TextScore, entities, context)
                : new LabeledUtterance(query, intent, entities);
        }
    }
}
