// ------------------------------------------------------------------------------------------------
// <copyright file="XmiSplitterServiceTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using NUnit.Framework;

    using uml4net;
    using uml4net.Packages;
    using uml4net.Profiling;
    using uml4net.xmi.Readers;
    using uml4net.xmi.Xmi;

    using XSplinter.Configuration;
    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="XmiSplitterService"/> orchestrator, using mocked
    /// collaborators so that the orchestration logic can be exercised without touching the disk.
    /// </summary>
    [TestFixture]
    public class XmiSplitterServiceTestFixture
    {
        private static readonly string[] PrimitivesApplicationIds = ["onPrimitives"];
        private static readonly string[] ForgeApplicationIds = ["onForge"];
        private static readonly string[] ForgeElementIds = ["pkgForge"];

        private Mock<IXmiModelLoader> modelLoader;
        private Mock<IPackageDocumentAssigner> documentAssigner;
        private Mock<IConstraintRelocator> constraintRelocator;
        private Mock<IExtensionBuilder> extensionBuilder;
        private Mock<IXmiDocumentWriter> documentWriter;
        private List<(IPackage Package, string Path, XmiRoot XmiRoot, string UmlNamespaceUri)> written;
        private SplitterConfig config;
        private XmiSplitterService xmiSplitterService;

        [SetUp]
        public void Setup()
        {
            this.modelLoader = new Mock<IXmiModelLoader>();
            this.documentAssigner = new Mock<IPackageDocumentAssigner>();
            this.constraintRelocator = new Mock<IConstraintRelocator>();

            this.constraintRelocator
                .Setup(x => x.Relocate(It.IsAny<IPackage>(), It.IsAny<IEnumerable<IPackage>>()))
                .Returns([]);
            this.extensionBuilder = new Mock<IExtensionBuilder>();
            this.documentWriter = new Mock<IXmiDocumentWriter>();

            this.modelLoader.Setup(x => x.Load(It.IsAny<string>())).Returns(CreateLoadedModel);

            this.documentAssigner
                .Setup(x => x.Assign(It.IsAny<IXmiElement>(), It.IsAny<string>()))
                .Returns(new PackageElementIds([], []));

            this.extensionBuilder.Setup(x => x.CanFilter(It.IsAny<XmiExtension>())).Returns(true);

            this.extensionBuilder
                .Setup(x => x.BuildConnectorPackageMap(
                    It.IsAny<XmiExtension>(),
                    It.IsAny<Dictionary<string, HashSet<string>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns([]);

            this.extensionBuilder
                .Setup(x => x.Build(
                    It.IsAny<XmiExtension>(),
                    It.IsAny<string>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, string>>()))
                .Returns(new XmiExtension { Extender = "Enterprise Architect", ExtenderId = "6.5" });

            this.written = [];

            this.documentWriter
                .Setup(x => x.Write(It.IsAny<IPackage>(), It.IsAny<string>(), It.IsAny<XmiRoot>(), It.IsAny<string>()))
                .Callback<IPackage, string, XmiRoot, string>(
                    (package, path, xmiRoot, umlNamespaceUri) =>
                        this.written.Add((package, path, xmiRoot, umlNamespaceUri)));

            this.config = new SplitterConfig
            {
                RootPackageName = "5. Data Structure",
                Packages =
                [
                    new PackageConfig { Name = "Primitives", OutputFile = "CSharp_Primitives.xmi", ConvertToLibrary = true },
                    new PackageConfig { Name = "Forge", OutputFile = "Forge.xmi" }
                ]
            };

            this.xmiSplitterService = new XmiSplitterService(
                NullLogger<XmiSplitterService>.Instance,
                this.modelLoader.Object,
                this.documentAssigner.Object,
                this.constraintRelocator.Object,
                this.extensionBuilder.Object,
                this.documentWriter.Object);
        }

        [Test]
        public void Verify_that_Split_ensures_the_output_directory_exists()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            this.documentWriter.Verify(x => x.EnsureDirectory("output"), Times.Once);
        }

        [Test]
        public void Verify_that_Split_writes_one_document_per_configured_package()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            Assert.Multiple(() =>
            {
                Assert.That(this.written, Has.Count.EqualTo(2));
                Assert.That(this.written.Select(document => document.Path), Is.EquivalentTo(new[]
                {
                    Path.Combine("output", "CSharp_Primitives.xmi"),
                    Path.Combine("output", "Forge.xmi")
                }));
            });
        }

        [Test]
        public void Verify_that_each_package_is_stamped_with_its_output_document()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            Assert.Multiple(() =>
            {
                this.documentAssigner.Verify(x => x.Assign(It.IsAny<IXmiElement>(), "CSharp_Primitives.xmi"), Times.Once);
                this.documentAssigner.Verify(x => x.Assign(It.IsAny<IXmiElement>(), "Forge.xmi"), Times.Once);
            });
        }

        [Test]
        public void Verify_that_a_convertToLibrary_package_is_written_without_a_model_wrapper_or_extension()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var library = this.written.Single(document => document.Path.EndsWith("CSharp_Primitives.xmi"));
            var full = this.written.Single(document => document.Path.EndsWith("Forge.xmi"));

            Assert.Multiple(() =>
            {
                Assert.That(library.Package, Is.Not.InstanceOf<IModel>());
                Assert.That(library.Package.Name, Is.EqualTo("Primitives"));
                Assert.That(library.XmiRoot.Extensions, Is.Empty);

                Assert.That(full.Package, Is.InstanceOf<IModel>());
                Assert.That(full.Package.Name, Is.EqualTo("EA_Model"));
                Assert.That(full.Package.PackagedElement.OfType<IPackage>().Single().Name, Is.EqualTo("Forge"));
                Assert.That(full.XmiRoot.Extensions, Is.Not.Empty);
            });
        }

        [Test]
        public void Verify_that_the_model_wrapper_mirrors_the_source_when_no_name_is_configured()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var full = this.written.Single(document => document.Path.EndsWith("Forge.xmi"));

            Assert.That(full.Package.Name, Is.EqualTo("EA_Model"));
        }

        [Test]
        public void Verify_that_a_configured_model_name_overrides_the_source_wrapper()
        {
            this.config.ModelName = "Mycelium";

            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var full = this.written.Single(document => document.Path.EndsWith("Forge.xmi"));
            var library = this.written.Single(document => document.Path.EndsWith("CSharp_Primitives.xmi"));

            Assert.Multiple(() =>
            {
                Assert.That(full.Package.Name, Is.EqualTo("Mycelium"));
                Assert.That(full.Package.PackagedElement.OfType<IPackage>().Single().Name, Is.EqualTo("Forge"));

                // a library package has no wrapper at all, so the setting does not apply
                Assert.That(library.Package.Name, Is.EqualTo("Primitives"));
            });
        }

        [Test]
        public void Verify_that_the_xmi_documentation_header_is_only_written_for_full_EA_documents()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var library = this.written.Single(document => document.Path.EndsWith("CSharp_Primitives.xmi"));
            var full = this.written.Single(document => document.Path.EndsWith("Forge.xmi"));

            Assert.Multiple(() =>
            {
                Assert.That(library.XmiRoot.Documentation, Is.Null);
                Assert.That(full.XmiRoot.Documentation, Is.Not.Null);
                Assert.That(full.XmiRoot.Documentation.Exporter, Is.EqualTo("Enterprise Architect"));
            });
        }

        [Test]
        public void Verify_that_a_stereotype_application_is_written_with_the_element_it_extends()
        {
            this.documentAssigner
                .Setup(x => x.Assign(It.IsAny<IXmiElement>(), It.IsAny<string>()))
                .Callback<IXmiElement, string>((element, documentName) => element.DocumentName = documentName)
                .Returns(new PackageElementIds([], []));

            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var library = this.written.Single(document => document.Path.EndsWith("CSharp_Primitives.xmi"));
            var full = this.written.Single(document => document.Path.EndsWith("Forge.xmi"));

            Assert.Multiple(() =>
            {
                Assert.That(library.XmiRoot.StereoTypeApplications.Select(application => application.XmiId), Is.EqualTo(PrimitivesApplicationIds));
                Assert.That(full.XmiRoot.StereoTypeApplications.Select(application => application.XmiId), Is.EqualTo(ForgeApplicationIds));
            });
        }

        [Test]
        public void Verify_that_the_extension_of_a_package_excludes_the_container_it_is_split_out_of()
        {
            this.documentAssigner
                .Setup(x => x.Assign(It.IsAny<IXmiElement>(), It.IsAny<string>()))
                .Returns(new PackageElementIds(["pkgForge"], ["root"]));

            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            this.extensionBuilder.Verify(x => x.Build(
                It.IsAny<XmiExtension>(),
                "Forge",
                It.Is<HashSet<string>>(ids => ids.SetEquals(ForgeElementIds)),
                It.IsAny<Dictionary<string, string>>()));
        }

        [Test]
        public void Verify_that_Split_throws_when_the_root_package_is_missing()
        {
            this.config.RootPackageName = "Does Not Exist";

            Assert.That(
                () => this.xmiSplitterService.Split("input.xmi", this.config, "output"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void Verify_that_Split_throws_when_a_configured_package_is_missing()
        {
            this.config.Packages.Add(new PackageConfig { Name = "Missing", OutputFile = "Missing.xmi" });

            Assert.That(
                () => this.xmiSplitterService.Split("input.xmi", this.config, "output"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        private static LoadedModel CreateLoadedModel()
        {
            var root = new Package { XmiId = "root", Name = "5. Data Structure" };
            var primitives = new Package { XmiId = "pkgPrim", Name = "Primitives" };
            var forge = new Package { XmiId = "pkgForge", Name = "Forge" };
            root.PackagedElement.Add(primitives);
            root.PackagedElement.Add(forge);

            var model = new Model { XmiId = "model", Name = "EA_Model" };
            model.PackagedElement.Add(root);

            var readerResult = new XmiReaderResult
            {
                Packages = [model],
                XmiRoot = new XmiRoot
                {
                    Documentation = new Documentation { Exporter = "Enterprise Architect", ExporterVersion = "6.5" },
                    Extensions = [new XmiExtension { Extender = "Enterprise Architect", ExtenderId = "6.5" }],
                    StereoTypeApplications =
                    [
                        new StereoTypeApplication { XmiId = "onRoot", ExtendedElement = root },
                        new StereoTypeApplication { XmiId = "onPrimitives", ExtendedElement = primitives },
                        new StereoTypeApplication { XmiId = "onForge", ExtendedElement = forge }
                    ]
                }
            };

            return new LoadedModel(readerResult, "http://www.omg.org/spec/UML/20161101");
        }
    }
}
