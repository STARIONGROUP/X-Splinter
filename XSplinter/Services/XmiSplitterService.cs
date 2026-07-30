// ------------------------------------------------------------------------------------------------
// <copyright file="XmiSplitterService.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.Extensions.Logging;

    using XSplinter.Configuration;

    /// <summary>
    /// Orchestrates the splitting of a monolithic Enterprise Architect XMI export
    /// into separate XMI files per package, rewriting cross-package references
    /// into <c>href</c> attributes that can be resolved by UML4NET.
    /// </summary>
    public class XmiSplitterService : IXmiSplitterService
    {
        /// <summary>
        /// The XMI namespace URI.
        /// </summary>
        private static readonly XNamespace Xmi = "http://www.omg.org/spec/XMI/20131001";

        /// <summary>
        /// The UML namespace URI.
        /// </summary>
        private static readonly XNamespace Uml = "http://www.omg.org/spec/UML/20161101";

        /// <summary>
        /// The UML DI namespace URI.
        /// </summary>
        private static readonly XNamespace Umldi = "http://www.omg.org/spec/UML/20161101/UMLDI";

        /// <summary>
        /// The UML DC namespace URI.
        /// </summary>
        private static readonly XNamespace Dc = "http://www.omg.org/spec/UML/20161101/UMLDC";

        /// <summary>
        /// The <see cref="ILogger{T}"/> used to log diagnostic messages.
        /// </summary>
        private readonly ILogger<XmiSplitterService> logger;

        /// <summary>
        /// The element indexer used to build the element-to-package mapping.
        /// </summary>
        private readonly IElementIndexer elementIndexer;

        /// <summary>
        /// The reference rewriter used to convert cross-package references into cross-file hrefs.
        /// </summary>
        private readonly IReferenceRewriter referenceRewriter;

        /// <summary>
        /// The extension builder used to create filtered EA Extension sections.
        /// </summary>
        private readonly IExtensionBuilder extensionBuilder;

        /// <summary>
        /// The file service used to load the input document and persist the outputs.
        /// </summary>
        private readonly IXmiFileService fileService;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiSplitterService"/> class,
        /// wiring up the default concrete collaborators.
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{T}"/> used to log diagnostic messages.
        /// </param>
        public XmiSplitterService(ILogger<XmiSplitterService> logger)
            : this(logger, new ElementIndexer(Xmi), new ReferenceRewriter(Xmi), new ExtensionBuilder(Xmi), new XmiFileService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiSplitterService"/> class
        /// with the supplied collaborators. This overload enables unit testing with mocks.
        /// </summary>
        /// <param name="logger">
        /// The <see cref="ILogger{T}"/> used to log diagnostic messages.
        /// </param>
        /// <param name="elementIndexer">
        /// The <see cref="IElementIndexer"/> used to build the element-to-package mapping.
        /// </param>
        /// <param name="referenceRewriter">
        /// The <see cref="IReferenceRewriter"/> used to rewrite cross-package references.
        /// </param>
        /// <param name="extensionBuilder">
        /// The <see cref="IExtensionBuilder"/> used to create filtered EA Extension sections.
        /// </param>
        /// <param name="fileService">
        /// The <see cref="IXmiFileService"/> used to load and save documents.
        /// </param>
        public XmiSplitterService(
            ILogger<XmiSplitterService> logger,
            IElementIndexer elementIndexer,
            IReferenceRewriter referenceRewriter,
            IExtensionBuilder extensionBuilder,
            IXmiFileService fileService)
        {
            this.logger = logger;
            this.elementIndexer = elementIndexer;
            this.referenceRewriter = referenceRewriter;
            this.extensionBuilder = extensionBuilder;
            this.fileService = fileService;
        }

        /// <summary>
        /// Splits the monolithic XMI file at <paramref name="inputPath"/> into
        /// separate files according to the provided <paramref name="config"/>,
        /// writing the results to the <paramref name="outputDirectory"/>.
        /// </summary>
        /// <param name="inputPath">
        /// The path to the monolithic XMI file exported from Enterprise Architect.
        /// </param>
        /// <param name="config">
        /// The splitter configuration defining the root package and child packages.
        /// </param>
        /// <param name="outputDirectory">
        /// The directory in which to write the split XMI files.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the root package or a configured child package is not found in the XMI.
        /// </exception>
        public void Split(string inputPath, SplitterConfig config, string outputDirectory)
        {
            this.fileService.EnsureDirectory(outputDirectory);

            var document = this.fileService.Load(inputPath);
            var root = document.Root!;

            var rootPackage = this.FindRootPackage(root, config.RootPackageName);
            var packageNodes = this.FindPackageNodes(rootPackage, config);

            var elementIndex = this.BuildElementIndex(config, packageNodes);

            this.logger.LogInformation("Indexed {ElementCount} elements across {PackageCount} packages", elementIndex.Count, config.Packages.Count);

            var packageElementIds = this.BuildPackageElementIds(config, packageNodes);

            var extension = root.Element(Xmi + "Extension");
            var extensionElements = extension?.Element("elements")?.Elements("element").ToList() ?? [];
            var extensionConnectors = extension?.Element("connectors")?.Elements("connector").ToList() ?? [];

            var connectorPackageMap = this.extensionBuilder.BuildConnectorPackageMap(
                extensionConnectors,
                packageElementIds,
                config.Packages.Select(packageConfig => packageConfig.Name));

            foreach (var packageConfig in config.Packages)
            {
                var clonedPackage = new XElement(packageNodes[packageConfig.Name]);

                this.referenceRewriter.Rewrite(clonedPackage, packageConfig.Name, elementIndex);

                var outputDocument = packageConfig.ConvertToLibrary
                    ? BuildLibraryOutputDocument(clonedPackage)
                    : this.BuildOutputDocument(
                        clonedPackage,
                        packageConfig,
                        packageElementIds[packageConfig.Name],
                        extensionElements,
                        extensionConnectors,
                        connectorPackageMap);

                var outputPath = Path.Combine(outputDirectory, packageConfig.OutputFile);
                this.fileService.Save(outputDocument, outputPath);

                this.logger.LogInformation("Written: {OutputPath}", outputPath);
            }

            this.logger.LogInformation("Splitting complete");
        }

        /// <summary>
        /// Locates the root container package (e.g. "5. Data Structure") inside
        /// the <c>uml:Model</c> element.
        /// </summary>
        /// <param name="root">
        /// The root <c>xmi:XMI</c> element of the source document.
        /// </param>
        /// <param name="rootPackageName">
        /// The expected name of the root container package.
        /// </param>
        /// <returns>
        /// The <see cref="XElement"/> representing the root container package.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no package with the given name is found.
        /// </exception>
        private XElement FindRootPackage(XElement root, string rootPackageName)
        {
            var model = root.Element(Uml + "Model")
                ?? throw new InvalidOperationException("No uml:Model element found in the XMI document.");

            return model
                .Elements("packagedElement")
                .FirstOrDefault(element => (string?)element.Attribute("name") == rootPackageName)
                ?? throw new InvalidOperationException($"Root package '{rootPackageName}' not found in the XMI document.");
        }

        /// <summary>
        /// Locates the <see cref="XElement"/> for each configured child package
        /// within the root container package.
        /// </summary>
        /// <param name="rootPackage">
        /// The root container package element.
        /// </param>
        /// <param name="config">
        /// The splitter configuration.
        /// </param>
        /// <returns>
        /// A dictionary mapping each package name to its <see cref="XElement"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a configured package is not found.
        /// </exception>
        private Dictionary<string, XElement> FindPackageNodes(XElement rootPackage, SplitterConfig config)
        {
            var packageNodes = new Dictionary<string, XElement>();

            foreach (var packageConfig in config.Packages)
            {
                var packageElement = rootPackage
                    .Elements("packagedElement")
                    .FirstOrDefault(element =>
                        (string?)element.Attribute(Xmi + "type") == "uml:Package"
                        && (string?)element.Attribute("name") == packageConfig.Name)
                    ?? throw new InvalidOperationException(
                        $"Package '{packageConfig.Name}' not found under '{config.RootPackageName}'.");

                packageNodes[packageConfig.Name] = packageElement;
            }

            return packageNodes;
        }

        /// <summary>
        /// Builds the global element index mapping every <c>xmi:id</c> across
        /// all configured packages to its owning <see cref="PackageEntry"/>.
        /// </summary>
        /// <param name="config">
        /// The splitter configuration.
        /// </param>
        /// <param name="packageNodes">
        /// The dictionary of package name to package <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// The populated element index.
        /// </returns>
        private Dictionary<string, PackageEntry> BuildElementIndex(
            SplitterConfig config,
            Dictionary<string, XElement> packageNodes)
        {
            var elementIndex = new Dictionary<string, PackageEntry>();

            foreach (var packageConfig in config.Packages)
            {
                var packageElement = packageNodes[packageConfig.Name];
                this.elementIndexer.IndexElementIds(packageElement, packageConfig.Name, packageConfig.OutputFile, elementIndex);
            }

            return elementIndex;
        }

        /// <summary>
        /// Builds a dictionary mapping each package name to the set of all
        /// element identifiers (both <c>xmi:id</c> and <c>xmi:idref</c>) found
        /// within that package. Used for filtering EA Extension entries.
        /// </summary>
        /// <param name="config">
        /// The splitter configuration.
        /// </param>
        /// <param name="packageNodes">
        /// The dictionary of package name to package <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// A dictionary mapping each package name to its set of element identifiers.
        /// </returns>
        private Dictionary<string, HashSet<string>> BuildPackageElementIds(
            SplitterConfig config,
            Dictionary<string, XElement> packageNodes)
        {
            var packageElementIds = new Dictionary<string, HashSet<string>>();

            foreach (var packageConfig in config.Packages)
            {
                var ids = new HashSet<string>();
                this.elementIndexer.CollectAllIds(packageNodes[packageConfig.Name], ids);
                packageElementIds[packageConfig.Name] = ids;
            }

            return packageElementIds;
        }

        /// <summary>
        /// Constructs a standard library output <see cref="XDocument"/> for a package
        /// that does not retain the Enterprise Architect model structure. The output
        /// uses a <c>uml:Package</c> root element without the <c>uml:Model</c> wrapper
        /// or <c>xmi:Extension</c> section.
        /// </summary>
        /// <param name="packageElement">
        /// The cloned and rewritten package element.
        /// </param>
        /// <returns>
        /// The library output <see cref="XDocument"/>.
        /// </returns>
        private static XDocument BuildLibraryOutputDocument(XElement packageElement)
        {
            return new XDocument(
                new XDeclaration("1.0", "windows-1252", null),
                new XElement(Xmi + "XMI",
                    new XAttribute(XNamespace.Xmlns + "xmi", Xmi),
                    new XAttribute(XNamespace.Xmlns + "uml", Uml),
                    new XElement(Uml + "Package",
                        new XAttribute(Xmi + "type", "uml:Package"),
                        packageElement.Attribute(Xmi + "id") is { } idAttr ? new XAttribute(Xmi + "id", idAttr.Value) : null!,
                        new XAttribute("name", (string?)packageElement.Attribute("name") ?? ""),
                        packageElement.Elements())));
        }

        /// <summary>
        /// Constructs the complete output <see cref="XDocument"/> for a single
        /// package, including the XMI envelope, UML model wrapper, and filtered
        /// EA Extension section.
        /// </summary>
        /// <param name="packageElement">
        /// The cloned and rewritten package element.
        /// </param>
        /// <param name="packageConfig">
        /// The configuration for this package.
        /// </param>
        /// <param name="packageIds">
        /// The set of element identifiers belonging to this package.
        /// </param>
        /// <param name="extensionElements">
        /// All element entries from the source EA Extension section.
        /// </param>
        /// <param name="extensionConnectors">
        /// All connector entries from the source EA Extension section.
        /// </param>
        /// <param name="connectorPackageMap">
        /// The mapping of connector IDs to owning package names.
        /// </param>
        /// <returns>
        /// The complete output <see cref="XDocument"/>.
        /// </returns>
        private XDocument BuildOutputDocument(
            XElement packageElement,
            PackageConfig packageConfig,
            HashSet<string> packageIds,
            List<XElement> extensionElements,
            List<XElement> extensionConnectors,
            Dictionary<string, string> connectorPackageMap)
        {
            return new XDocument(
                new XDeclaration("1.0", "windows-1252", null),
                new XElement(Xmi + "XMI",
                    new XAttribute(XNamespace.Xmlns + "xmi", Xmi),
                    new XAttribute(XNamespace.Xmlns + "uml", Uml),
                    new XAttribute(XNamespace.Xmlns + "umldi", Umldi),
                    new XAttribute(XNamespace.Xmlns + "dc", Dc),
                    new XElement(Xmi + "Documentation",
                        new XAttribute("exporter", "Enterprise Architect"),
                        new XAttribute("exporterVersion", "6.5"),
                        new XAttribute("exporterID", "1704")),
                    new XElement(Uml + "Model",
                        new XAttribute(Xmi + "type", "uml:Model"),
                        new XAttribute("name", "EA_Model"),
                        packageElement),
                    this.extensionBuilder.Build(
                        packageConfig.Name,
                        packageIds,
                        extensionElements,
                        extensionConnectors,
                        connectorPackageMap)));
        }
    }
}
