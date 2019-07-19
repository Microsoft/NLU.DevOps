﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Models
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Entity appearing in utterance.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class.
        /// </summary>
        /// <param name="entityType">Entity type name.</param>
        /// <param name="entityValue">Entity value, generally a canonical form of the entity.</param>
        /// <param name="entityResolution">Entity resolution, generally additional details about the entity.</param>
        /// <param name="matchText">Matching text in the utterance.</param>
        /// <param name="matchIndex">Occurrence index of matching token in the utterance.</param>
        public Entity(string entityType, JToken entityValue, JToken entityResolution, string matchText, int matchIndex)
        {
            this.EntityType = entityType;
            this.EntityValue = entityValue;
            this.EntityResolution = entityResolution;
            this.MatchText = matchText;
            this.MatchIndex = matchIndex;
        }

        /// <summary>
        /// Gets the entity type name.
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the entity value, generally a canonical form of the entity.
        /// </summary>
        public JToken EntityValue { get; }

        /// <summary>
        /// Gets the entitiy resolution, generally additional details about the entity.
        /// </summary>
        /// <value>The entity resolution.</value>
        public JToken EntityResolution { get; }

        /// <summary>
        /// Gets the matching text in the utterance.
        /// </summary>
        public string MatchText { get; }

        /// <summary>
        /// Gets the occurrence index of matching token in the utterance.
        /// </summary>
        public int MatchIndex { get; }
    }
}
