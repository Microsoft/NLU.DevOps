﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.Lex
{
    using System;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Amazon;
    using Amazon.Runtime;
    using Microsoft.Extensions.Configuration;
    using Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Lex NLU service factory.
    /// </summary>
    [Export("lex", typeof(INLUServiceFactory))]
    public class LexNLUServiceFactory : INLUServiceFactory
    {
        private const string LexBotNamePrefixConfigurationKey = "lexBotNamePrefix";
        private const string LexAccessKeyConfigurationKey = "awsAccessKey";
        private const string LexSecretKeyConfigurationKey = "awsSecretKey";
        private const string LexSecretKeyBase64ConfigurationKey = "awsSecretKeyBase64";
        private const string LexRegionConfigurationKey = "awsRegion";

        /// <summary>
        /// The Lex bot name configuration key.
        /// </summary>
        private static readonly string LexBotNameConfigurationKey = CamelCase(nameof(LexNLUService.LexBotName));

        /// <summary>
        /// The Lex bot alias configuration key.
        /// </summary>
        private static readonly string LexBotAliasConfigurationKey = CamelCase(nameof(LexNLUService.LexBotAlias));

        /// <inheritdoc />
        public INLUService CreateInstance(IConfiguration configuration, string settingsPath)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var userDefinedName = configuration[LexBotNameConfigurationKey];
            var botName = userDefinedName ?? GetRandomName(configuration[LexBotNamePrefixConfigurationKey]);
            var botAlias = configuration[LexBotAliasConfigurationKey] ?? botName;
            var lexSettings = settingsPath != null
                ? JsonConvert.DeserializeObject<LexSettings>(File.ReadAllText(settingsPath))
                : new LexSettings();
            var credentials = new BasicAWSCredentials(configuration[LexAccessKeyConfigurationKey], GetSecretKey(configuration));
            var regionEndpoint = GetRegionEndpoint(configuration[LexRegionConfigurationKey]);
            return new LexNLUService(botName, botAlias, lexSettings, credentials, regionEndpoint);
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

        private static RegionEndpoint GetRegionEndpoint(string region)
        {
            return RegionEndpoint.EnumerableAllRegions.FirstOrDefault(r => r.SystemName == region);
        }

        private static string GetSecretKey(IConfiguration configuration)
        {
            var secretKey = configuration[LexSecretKeyConfigurationKey];
            if (secretKey != null)
            {
                return secretKey;
            }

            var secretKeyBase64 = configuration[LexSecretKeyBase64ConfigurationKey];
            if (secretKeyBase64 == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(secretKeyBase64));
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
