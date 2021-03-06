﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Core
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JSON converter for <see cref="LabeledUtterance"/> to recognize LUIS batch test format.
    /// </summary>
    public class LabeledUtteranceConverter : JsonConverter<LabeledUtterance>
    {
        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override LabeledUtterance ReadJson(JsonReader reader, Type objectType, LabeledUtterance existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            if (jsonObject.ContainsKey("query") && !jsonObject.ContainsKey("text"))
            {
                jsonObject.Add("text", jsonObject.Value<string>("query"));
                jsonObject.Remove("query");
            }

            var utterance = jsonObject.Value<string>("text");
            var entityConverter = new EntityConverter(utterance);
            serializer.Converters.Remove(this);
            serializer.Converters.Add(entityConverter);
            try
            {
                return (LabeledUtterance)jsonObject.ToObject(objectType, serializer);
            }
            finally
            {
                serializer.Converters.Add(this);
                serializer.Converters.Remove(entityConverter);
            }
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, LabeledUtterance value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
