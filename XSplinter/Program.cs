// ------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text.Json;

    using Microsoft.Extensions.Logging;

    using XSplinter.Configuration;
    using XSplinter.Services;

    /// <summary>
    /// Entry point for the XSplinter console application.
    /// Splits a monolithic Enterprise Architect XMI export into separate
    /// XMI files per package with cross-file href references.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        /// <summary>
        /// The application entry point.
        /// </summary>
        /// <param name="args">
        /// Command-line arguments: input-xmi config-json [--output dir].
        /// </param>
        /// <returns>
        /// 0 on success; 1 on error.
        /// </returns>
        public static int Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            var logger = loggerFactory.CreateLogger<Program>();

            if (args.Length < 2)
            {
                PrintUsage(logger);
                return 1;
            }

            var inputPath = args[0];
            var configPath = args[1];
            var outputDirectory = ".";

            for (var argIndex = 2; argIndex < args.Length; argIndex++)
            {
                if (args[argIndex] == "--output" && argIndex + 1 < args.Length)
                {
                    outputDirectory = args[++argIndex];
                }
            }

            if (!File.Exists(inputPath))
            {
                logger.LogError("Input file not found: {InputPath}", inputPath);
                return 1;
            }

            if (!File.Exists(configPath))
            {
                logger.LogError("Config file not found: {ConfigPath}", configPath);
                return 1;
            }

            var config = JsonSerializer.Deserialize<SplitterConfig>(
                File.ReadAllText(configPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config == null)
            {
                logger.LogError("Failed to parse configuration file");
                return 1;
            }

            try
            {
                var splitterLogger = loggerFactory.CreateLogger<XmiSplitterService>();
                var splitter = new XmiSplitterService(splitterLogger);
                splitter.Split(inputPath, config, outputDirectory);
                return 0;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Splitting failed");
                return 1;
            }
        }

        /// <summary>
        /// Logs usage instructions.
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger"/> used to log the usage instructions.
        /// </param>
        private static void PrintUsage(ILogger logger)
        {
            logger.LogInformation(
                "Usage: XSplinter <input.xmi> <config.json> [--output <dir>]\n\n" +
                "Splits a monolithic Enterprise Architect XMI export into separate\n" +
                "XMI files per package with cross-file href references.\n\n" +
                "config.json format:\n" +
                "{{\n" +
                "  \"rootPackageName\": \"5. Data Structure\",\n" +
                "  \"packages\": [\n" +
                "    {{ \"name\": \"Primitives\", \"outputFile\": \"CSharp_Primitives.xmi\", \"convertToLibrary\": true }},\n" +
                "    {{ \"name\": \"Forge\", \"outputFile\": \"Forge.xmi\" }},\n" +
                "    {{ \"name\": \"FunctionalData\", \"outputFile\": \"FunctionalData.xmi\" }}\n" +
                "  ]\n" +
                "}}\n\n" +
                "Set convertToLibrary to true for reusable library packages that are\n" +
                "output as a simple uml:Package without EA metadata.");
        }
    }
}
