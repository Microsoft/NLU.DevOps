﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis
{
    using System;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Factory for creating <see cref="LuisNLUTrainClient"/> and <see cref="LuisNLUTestClient"/> instances.
    /// </summary>
    [Export("luis", typeof(INLUClientFactory))]
    public class LuisNLUClientFactory : INLUClientFactory
    {
        private const string LuisAppNamePrefixConfigurationKey = "luisAppNamePrefix";
        private const string LuisAuthoringKeyConfigurationKey = "luisAuthoringKey";
        private const string LuisEndpointKeyConfigurationKey = "luisEndpointKey";
        private const string LuisAuthoringRegionConfigurationKey = "luisAuthoringRegion";
        private const string LuisEndpointRegionConfigurationKey = "luisEndpointRegion";
        private const string SpeechKeyConfigurationKey = "speechKey";
        private const string CustomSpeechAppIdConfigurationKey = "customSpeechAppId";
        private const string SpeechRegionConfigurationKey = "speechRegion";
        private const string LuisIsStagingConfigurationKey = "luisIsStaging";
        private const string LuisAppNameConfigurationKey = "luisAppName";
        private const string ArmTokenConfigurationKey = "ARM_TOKEN";
        private const string LuisSubscriptionIdConfigurationKey = "azureSubscriptionId";
        private const string LuisResourceGroupConfigurationKey = "azureResourceGroup";
        private const string LuisAzureAppNameConfigurationKey = "azureLuisResourceName";
        private const string LuisVersionIdConfigurationKey = "luisVersionId";
        private const string CustomSpeechEndpointTemplate = "https://{0}.stt.speech.microsoft.com/speech/recognition/interactive/cognitiveservices/v1?language={1}&?cid={2}";
        private const string SpeechEndpointTemplate = "https://{0}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={1}";
        private const string LuisVersionPrefixConfigurationKey = "luisVersionPrefix";

        private const string BuildIdConfigurationKey = "BUILD_BUILDID";

        private static readonly string LuisAppIdConfigurationKey = CamelCase(nameof(LuisNLUTrainClient.LuisAppId));

        /// <inheritdoc/>
        public INLUTrainClient CreateTrainInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var userDefinedName = configuration[LuisAppNameConfigurationKey];
            var appName = userDefinedName ?? GetRandomName(configuration[LuisAppNamePrefixConfigurationKey]);

            var isStagingString = configuration[LuisIsStagingConfigurationKey];
            var isStaging = false;
            if (isStagingString != null && !bool.TryParse(isStagingString, out isStaging))
            {
                throw new ArgumentException("The configuration value 'luisIsStaging' must be a valid boolean.");
            }

            var luisSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LuisSettings>(File.ReadAllText(settingsPath))
                : new LuisSettings();

            var azureSubscriptionInfo = AzureSubscriptionInfo.Create(
                configuration[LuisSubscriptionIdConfigurationKey],
                configuration[LuisResourceGroupConfigurationKey],
                configuration[LuisAzureAppNameConfigurationKey],
                configuration[ArmTokenConfigurationKey]);

            var luisClient = new LuisTrainClient(
                    configuration[LuisAuthoringKeyConfigurationKey],
                    configuration[LuisAuthoringRegionConfigurationKey],
                    configuration[LuisEndpointRegionConfigurationKey],
                    azureSubscriptionInfo,
                    isStaging);

            return new LuisNLUTrainClient(
                appName,
                configuration[LuisAppIdConfigurationKey],
                GetVersionId(configuration),
                luisSettings,
                luisClient);
        }

        /// <inheritdoc/>
        public INLUTestClient CreateTestInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var isStagingString = configuration[LuisIsStagingConfigurationKey];
            var isStaging = isStagingString != null ? bool.Parse(isStagingString) : false;

            var luisSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LuisSettings>(File.ReadAllText(settingsPath))
                : new LuisSettings();

            var luisSpeechKey = configuration[SpeechKeyConfigurationKey];

            var speechKey = configuration[SpeechKeyConfigurationKey];
            var customSpeechAppId = configuration[CustomSpeechAppIdConfigurationKey];
            var speechRegion = configuration[SpeechRegionConfigurationKey] ?? configuration[LuisEndpointRegionConfigurationKey];
            var azureSubscriptionInfo = AzureSubscriptionInfo.Create(
                configuration[LuisSubscriptionIdConfigurationKey],
                configuration[LuisResourceGroupConfigurationKey],
                configuration[LuisAzureAppNameConfigurationKey],
                configuration[ArmTokenConfigurationKey]);
            var speechEndpoint = customSpeechAppId != null ?
                string.Format(CultureInfo.InvariantCulture, CustomSpeechEndpointTemplate, speechRegion, "en-US", customSpeechAppId) :
                string.Format(CultureInfo.InvariantCulture, SpeechEndpointTemplate, speechRegion, "en-US");

            var luisClient = speechKey != null
                ? new RestSpeechLuisTestClient(
                        configuration[LuisEndpointKeyConfigurationKey],
                        configuration[LuisEndpointRegionConfigurationKey],
                        speechKey,
                        speechEndpoint,
                        isStaging)
                : new LuisTestClient(
                    configuration[LuisEndpointKeyConfigurationKey],
                    configuration[LuisEndpointRegionConfigurationKey],
                    isStaging);

            return new LuisNLUTestClient(
                configuration[LuisAppIdConfigurationKey],
                luisSettings,
                luisClient);
        }

        private static string GetRandomName(string prefix)
        {
            var random = new Random();
            var randomString = new string(Enumerable.Repeat(0, 8)
                .Select(_ => (char)random.Next((int)'A', (int)'Z'))
                .ToArray());

            prefix = prefix != null ? $"{prefix}_" : prefix;
            return $"{prefix}{randomString}";
        }

        private static string GetVersionId(IConfiguration configuration)
        {
            var versionId = configuration[LuisVersionIdConfigurationKey];
            if (versionId != null)
            {
                return versionId;
            }

            var versionIdPrefix = configuration[LuisVersionPrefixConfigurationKey];
            if (versionIdPrefix == null)
            {
                return null;
            }

            var buildId = configuration[BuildIdConfigurationKey];
            var buildIdModifier = buildId != null ? $".{buildId}" : string.Empty;
            return $"{versionIdPrefix}{buildIdModifier}";
        }

        private static string CamelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || char.IsLower(s[0]))
            {
                return s;
            }

            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
