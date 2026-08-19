// ------------------------------------------------------------------------------------------------
// <copyright file="XmiSplitterService.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using uml4net;
    using uml4net.Packages;

    using XSplinter.Configuration;

    /// <summary>
    /// Orchestrates the splitting of a monolithic Enterprise Architect XMI export into separate
    /// XMI files per package. The document is read into the UML4NET object model; each extracted
    /// package is stamped with the document it is written to, so that UML4NET emits cross-package
    /// references as <c>href</c> attributes pointing at the corresponding split file.
    /// </summary>
    public class XmiSplitterService : IXmiSplitterService
    {
        /// <summary>
        /// The <see cref="ILogger{T}"/> used to log diagnostic messages.
        /// </summary>
        private readonly ILogger<XmiSplitterService> logger;

        /// <summary>
        /// The loader used to read the monolithic document into the UML4NET object model.
        /// </summary>
        private readonly IXmiModelLoader modelLoader;

        /// <summary>
        /// The assigner used to stamp each package subtree with its target document name.
        /// </summary>
        private readonly IPackageDocumentAssigner documentAssigner;

        /// <summary>
        /// The relocator used to move root-owned constraints into the package they constrain.
        /// </summary>
        private readonly IConstraintRelocator constraintRelocator;

        /// <summary>
        /// The extension builder used to create filtered EA extension sections.
        /// </summary>
        private readonly IExtensionBuilder extensionBuilder;

        /// <summary>
        /// The writer used to persist the split documents.
        /// </summary>
        private readonly IXmiDocumentWriter documentWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiSplitterService"/> class,
        /// wiring up the default concrete collaborators.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to set up logging.
        /// </param>
        public XmiSplitterService(ILoggerFactory loggerFactory)
            : this(
                loggerFactory.CreateLogger<XmiSplitterService>(),
                new XmiModelLoader(loggerFactory),
                new PackageDocumentAssigner(),
                new ConstraintRelocator(),
                new ExtensionBuilder(),
                new XmiDocumentWriter(loggerFactory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiSplitterService"/> class
        /// with the supplied collaborators. This overload enables unit testing with mocks.
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{T}"/> used to log diagnostic messages.
        /// </param>
        /// <param name="modelLoader">
        /// The <see cref="IXmiModelLoader"/> used to read the monolithic document.
        /// </param>
        /// <param name="documentAssigner">
        /// The <see cref="IPackageDocumentAssigner"/> used to stamp the target document names.
        /// </param>
        /// <param name="constraintRelocator">
        /// The <see cref="IConstraintRelocator"/> used to move root-owned constraints.
        /// </param>
        /// <param name="extensionBuilder">
        /// The <see cref="IExtensionBuilder"/> used to create filtered EA extension sections.
        /// </param>
        /// <param name="documentWriter">
        /// The <see cref="IXmiDocumentWriter"/> used to persist the split documents.
        /// </param>
        public XmiSplitterService(
            ILogger<XmiSplitterService> logger,
            IXmiModelLoader modelLoader,
            IPackageDocumentAssigner documentAssigner,
            IConstraintRelocator constraintRelocator,
            IExtensionBuilder extensionBuilder,
            IXmiDocumentWriter documentWriter)
        {
            this.logger = logger;
            this.modelLoader = modelLoader;
            this.documentAssigner = documentAssigner;
            this.constraintRelocator = constraintRelocator;
            this.extensionBuilder = extensionBuilder;
            this.documentWriter = documentWriter;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when the root package or a configured child package is not found in the XMI.
        /// </exception>
        public void Split(string inputPath, SplitterConfig config, string outputDirectory)
        {
            this.documentWriter.EnsureDirectory(outputDirectory);

            var loaded = this.modelLoader.Load(inputPath);
            var result = loaded.ReaderResult;

            var candidates = CollectPackages(result.Packages, config.RootPackageName);

            var rootPackage = string.IsNullOrEmpty(config.RootPackageName)
                ? null
                : candidates.First(candidate => candidate.Package.Name == config.RootPackageName).Package;

            var packageNodes = FindPackageNodes(candidates, config);
            var enclosingModels = candidates
                .GroupBy(candidate => candidate.Package)
                .ToDictionary(group => group.Key, group => group.First().Model);

            var packageElementIds = new Dictionary<string, HashSet<string>>();

            foreach (var packageConfig in config.Packages)
            {
                packageElementIds[packageConfig.Name] =
                    this.documentAssigner.Assign(packageNodes[packageConfig.Name], packageConfig.OutputFile).All();
            }

            // a constraint owned by the root container package would be lost, since that package
            // is split around and never written; it is moved to the package holding the element
            // it constrains, and stamped so that it is written to that document
            foreach (var relocation in this.constraintRelocator.Relocate(rootPackage, packageNodes.Values))
            {
                var packageName = packageNodes
                    .First(entry => ReferenceEquals(entry.Value, relocation.TargetPackage))
                    .Key;

                var relocatedIds = this.documentAssigner.Assign(relocation.Constraint, relocation.TargetPackage.DocumentName);

                packageElementIds[packageName].UnionWith(relocatedIds.All());

                this.logger.LogInformation(
                    "Relocated constraint {ConstraintName} to {PackageName}",
                    relocation.Constraint.Name,
                    packageName);
            }

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                this.logger.LogInformation(
                    "Indexed {ElementCount} elements across {PackageCount} packages",
                    packageElementIds.Values.Sum(ids => ids.Count),
                    config.Packages.Count);
            }

            var extension = result.XmiRoot?.Extensions?.FirstOrDefault();

            if (extension != null && !this.extensionBuilder.CanFilter(extension))
            {
                this.logger.LogWarning(
                    "The {Extender} extension is not understood and is copied unchanged into every output document",
                    extension.Extender);
            }

            var connectorPackageMap = extension == null
                ? []
                : this.extensionBuilder.BuildConnectorPackageMap(
                    extension,
                    packageElementIds,
                    config.Packages.Select(packageConfig => packageConfig.Name));

            foreach (var packageConfig in config.Packages)
            {
                var package = packageNodes[packageConfig.Name];
                var outputPath = Path.Combine(outputDirectory, packageConfig.OutputFile);

                var enclosingModel = enclosingModels[package];

                if (packageConfig.ConvertToLibrary || enclosingModel == null)
                {
                    // no model wrapper: either explicitly requested, or the source did not have one.
                    // a name configured for this package therefore renames the package itself, which
                    // is the top level element of the document
                    if (!string.IsNullOrEmpty(packageConfig.ModelName))
                    {
                        package.Name = packageConfig.ModelName;
                    }

                    this.documentWriter.Write(package, outputPath, null, null, loaded.UmlNamespaceUri);
                }
                else
                {
                    // use the configured wrapper name, otherwise mirror the source document
                    var model = new Model
                    {
                        Name = QueryModelName(packageConfig, config, enclosingModel),
                        DocumentName = packageConfig.OutputFile
                    };

                    model.PackagedElement.Add(package);

                    var extensions = extension == null
                        ? null
                        : new List<XmiExtension>
                        {
                            this.extensionBuilder.Build(
                                extension,
                                packageConfig.Name,
                                packageElementIds[packageConfig.Name],
                                connectorPackageMap)
                        };

                    this.documentWriter.Write(model, outputPath, result.XmiRoot?.Documentation, extensions, loaded.UmlNamespaceUri);
                }

                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation("Written: {OutputPath}", outputPath);
                }
            }

            this.logger.LogInformation("Splitting complete");
        }

        /// <summary>
        /// Determines the name of the <c>uml:Model</c> wrapper: the name configured for the package,
        /// otherwise the name configured for the whole run, otherwise the name of the model that
        /// encloses the package in the source document.
        /// </summary>
        /// <param name="packageConfig">
        /// The configuration of the package being written.
        /// </param>
        /// <param name="config">
        /// The splitter configuration.
        /// </param>
        /// <param name="enclosingModel">
        /// The model enclosing the package in the source document.
        /// </param>
        /// <returns>
        /// The name to write on the model wrapper.
        /// </returns>
        private static string QueryModelName(PackageConfig packageConfig, SplitterConfig config, IModel enclosingModel)
        {
            if (!string.IsNullOrEmpty(packageConfig.ModelName))
            {
                return packageConfig.ModelName;
            }

            return string.IsNullOrEmpty(config.ModelName) ? enclosingModel.Name : config.ModelName;
        }

        /// <summary>
        /// Walks the package hierarchy of the document and records every package together with the
        /// <see cref="IModel"/> that encloses it, so that the split documents can mirror the
        /// wrapper of the source. When a root container package is configured, only that package
        /// and its descendants are returned.
        /// </summary>
        /// <param name="rootPackages">
        /// The root packages read from the document.
        /// </param>
        /// <param name="rootPackageName">
        /// The name of the root container package, or <c>null</c>/empty to search the whole document.
        /// </param>
        /// <returns>
        /// The discovered packages and their enclosing model.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a root container package is configured but not found.
        /// </exception>
        private static List<PackageCandidate> CollectPackages(IEnumerable<IPackage> rootPackages, string rootPackageName)
        {
            var candidates = new List<PackageCandidate>();
            Walk(rootPackages, null, candidates, []);

            if (string.IsNullOrEmpty(rootPackageName))
            {
                return candidates;
            }

            var container = candidates.FirstOrDefault(candidate => candidate.Package.Name == rootPackageName)
                ?? throw new InvalidOperationException($"Root package '{rootPackageName}' not found in the XMI document.");

            var scoped = new List<PackageCandidate> { container };
            Walk(container.Package.PackagedElement.OfType<IPackage>(), container.Model, scoped, []);

            return scoped;
        }

        /// <summary>
        /// Recursively records the packages and the model enclosing them.
        /// </summary>
        /// <param name="packages">
        /// The packages to walk.
        /// </param>
        /// <param name="enclosingModel">
        /// The <see cref="IModel"/> enclosing the packages, if any.
        /// </param>
        /// <param name="candidates">
        /// The list collecting the discovered packages.
        /// </param>
        /// <param name="visited">
        /// The already visited packages, guarding against cycles.
        /// </param>
        private static void Walk(IEnumerable<IPackage> packages, IModel enclosingModel, List<PackageCandidate> candidates, HashSet<IPackage> visited)
        {
            foreach (var package in packages)
            {
                if (!visited.Add(package))
                {
                    continue;
                }

                var model = package as IModel ?? enclosingModel;

                candidates.Add(new PackageCandidate(package, model));

                Walk(package.PackagedElement.OfType<IPackage>(), model, candidates, visited);
            }
        }

        /// <summary>
        /// Locates the <see cref="IPackage"/> for each configured package.
        /// </summary>
        /// <param name="candidates">
        /// The packages discovered in the document.
        /// </param>
        /// <param name="config">
        /// The splitter configuration.
        /// </param>
        /// <returns>
        /// A dictionary mapping each package name to its <see cref="IPackage"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a configured package is not found.
        /// </exception>
        private static Dictionary<string, IPackage> FindPackageNodes(List<PackageCandidate> candidates, SplitterConfig config)
        {
            var scope = string.IsNullOrEmpty(config.RootPackageName)
                ? "the XMI document"
                : $"'{config.RootPackageName}'";

            return config.Packages.ToDictionary(
                packageConfig => packageConfig.Name,
                packageConfig => candidates
                    .FirstOrDefault(candidate => candidate.Package.Name == packageConfig.Name)?.Package
                    ?? throw new InvalidOperationException(
                        $"Package '{packageConfig.Name}' not found under {scope}."));
        }

        /// <summary>
        /// Associates a discovered package with the model that encloses it.
        /// </summary>
        /// <param name="Package">
        /// The discovered package.
        /// </param>
        /// <param name="Model">
        /// The enclosing <see cref="IModel"/>, or <c>null</c> when the package is not inside a model.
        /// </param>
        private sealed record PackageCandidate(IPackage Package, IModel Model);
    }
}
