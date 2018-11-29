﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine
{
    using System;
    using System.IO;
    using System.Text;
    using Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal abstract class BaseCommand<TOptions> : ICommand
        where TOptions : BaseOptions
    {
        public BaseCommand(TOptions options)
        {
            this.Options = options;
            this.LazyConfiguration = new Lazy<IConfiguration>(this.CreateConfiguration);
            this.LazyNLUService = new Lazy<INLUService>(this.CreateNLUService);
            this.LazyLogger = new Lazy<ILogger>(this.CreateLogger);
        }

        protected TOptions Options { get; }

        protected IConfiguration Configuration => this.LazyConfiguration.Value;

        protected INLUService NLUService => this.LazyNLUService.Value;

        private Lazy<IConfiguration> LazyConfiguration { get; }

        private Lazy<INLUService> LazyNLUService { get; }

        private Lazy<ILogger> LazyLogger { get; }

        public abstract int Main();

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static T Read<T>(string path)
        {
            var serializer = JsonSerializer.CreateDefault();
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        protected static void Write(Stream stream, object value)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.Formatting = Formatting.Indented;
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
            {
                serializer.Serialize(textWriter, value);
            }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing && this.LazyNLUService.IsValueCreated)
            {
                this.NLUService.Dispose();
            }
        }

        protected void Log(string message)
        {
            this.LazyLogger.Value.LogInformation(message);
        }

        private IConfiguration CreateConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json");

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{this.Options.Service}.json")))
            {
                configurationBuilder.AddJsonFile($"appsettings.{this.Options.Service}.json");
            }

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.local.json")))
            {
                configurationBuilder.AddJsonFile("appsettings.local.json");
            }

            return configurationBuilder
                .AddEnvironmentVariables()
                .Build();
        }

        private INLUService CreateNLUService()
        {
            return NLUServiceFactory.Create(this.Options.Service, this.Configuration);
        }

        private ILogger CreateLogger()
        {
            var logLevel = this.Options.Verbose ? LogLevel.Trace : LogLevel.Information;

            if (this.Options.Quiet)
            {
                logLevel = LogLevel.Warning;
            }

            return ApplicationLogger.LoggerFactory.AddConsole(logLevel).CreateLogger(this.GetType());
        }
    }
}
