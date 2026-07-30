namespace XSplinter.Tests.Services
{
    using System.Xml.Linq;

    using Microsoft.Extensions.Logging.Abstractions;

    using Moq;

    using NUnit.Framework;

    using XSplinter.Configuration;
    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="XmiSplitterService" /> orchestrator, using mocked
    /// collaborators so that the orchestration logic can be exercised without touching the disk.
    /// </summary>
    [TestFixture]
    public class XmiSplitterServiceTestFixture
    {
        private const string XmiNamespace = "http://www.omg.org/spec/XMI/20131001";
        private const string UmlNamespace = "http://www.omg.org/spec/UML/20161101";

        private Mock<IElementIndexer> elementIndexer;
        private Mock<IReferenceRewriter> referenceRewriter;
        private Mock<IExtensionBuilder> extensionBuilder;
        private Mock<IXmiFileService> fileService;
        private List<(XDocument Document, string OutputPath)> savedDocuments;
        private SplitterConfig config;
        private XmiSplitterService xmiSplitterService;

        [SetUp]
        public void Setup()
        {
            this.elementIndexer = new Mock<IElementIndexer>();
            this.referenceRewriter = new Mock<IReferenceRewriter>();
            this.extensionBuilder = new Mock<IExtensionBuilder>();
            this.fileService = new Mock<IXmiFileService>();

            this.extensionBuilder
                .Setup(x => x.BuildConnectorPackageMap(
                    It.IsAny<List<XElement>>(),
                    It.IsAny<Dictionary<string, HashSet<string>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .Returns(new Dictionary<string, string>());

            this.extensionBuilder
                .Setup(x => x.Build(
                    It.IsAny<string>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<List<XElement>>(),
                    It.IsAny<List<XElement>>(),
                    It.IsAny<Dictionary<string, string>>()))
                .Returns(new XElement(XName.Get("Extension", XmiNamespace)));

            this.fileService.Setup(x => x.Load(It.IsAny<string>())).Returns(CreateMonolith);

            this.savedDocuments = [];

            this.fileService
                .Setup(x => x.Save(It.IsAny<XDocument>(), It.IsAny<string>()))
                .Callback<XDocument, string>((document, path) => this.savedDocuments.Add((document, path)));

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
                this.elementIndexer.Object,
                this.referenceRewriter.Object,
                this.extensionBuilder.Object,
                this.fileService.Object);
        }

        [Test]
        public void Verify_that_a_convertToLibrary_package_is_written_as_a_plain_uml_Package()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            var libraryDocument = this.savedDocuments.Single(document => document.OutputPath.EndsWith("CSharp_Primitives.xmi")).Document;
            var fullDocument = this.savedDocuments.Single(document => document.OutputPath.EndsWith("Forge.xmi")).Document;

            XNamespace uml = UmlNamespace;
            XNamespace xmi = XmiNamespace;

            Assert.Multiple(() =>
            {
                Assert.That(libraryDocument.Root!.Element(uml + "Package"), Is.Not.Null);
                Assert.That(libraryDocument.Root!.Element(uml + "Model"), Is.Null);
                Assert.That(libraryDocument.Descendants(xmi + "Extension"), Is.Empty);

                Assert.That(fullDocument.Root!.Element(uml + "Model"), Is.Not.Null);
                Assert.That(fullDocument.Descendants(xmi + "Extension"), Is.Not.Empty);
            });
        }

        [Test]
        public void Verify_that_Split_ensures_the_output_directory_exists()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            this.fileService.Verify(x => x.EnsureDirectory("output"), Times.Once);
        }

        [Test]
        public void Verify_that_Split_rewrites_references_for_each_package()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            this.referenceRewriter.Verify(
                x => x.Rewrite(It.IsAny<XElement>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, PackageEntry>>()),
                Times.Exactly(2));
        }

        [Test]
        public void Verify_that_Split_throws_when_the_root_package_is_missing()
        {
            this.fileService.Setup(x => x.Load(It.IsAny<string>())).Returns(CreateMonolithWithoutRoot);

            Assert.That(
                () => this.xmiSplitterService.Split("input.xmi", this.config, "output"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void Verify_that_Split_writes_one_document_per_configured_package()
        {
            this.xmiSplitterService.Split("input.xmi", this.config, "output");

            Assert.Multiple(() =>
            {
                Assert.That(this.savedDocuments, Has.Count.EqualTo(2));

                Assert.That(this.savedDocuments.Select(document => document.OutputPath), Is.EquivalentTo(new[]
                {
                    Path.Combine("output", "CSharp_Primitives.xmi"),
                    Path.Combine("output", "Forge.xmi")
                }));
            });
        }

        private static XDocument CreateMonolith()
        {
            return XDocument.Parse(
                $"<xmi:XMI xmlns:xmi=\"{XmiNamespace}\" xmlns:uml=\"{UmlNamespace}\">" +
                "  <uml:Model xmi:type=\"uml:Model\" name=\"EA_Model\">" +
                "    <packagedElement name=\"5. Data Structure\">" +
                "      <packagedElement xmi:type=\"uml:Package\" xmi:id=\"pkgPrim\" name=\"Primitives\">" +
                "        <packagedElement xmi:type=\"uml:PrimitiveType\" xmi:id=\"e1\" name=\"Text\" />" +
                "      </packagedElement>" +
                "      <packagedElement xmi:type=\"uml:Package\" xmi:id=\"pkgForge\" name=\"Forge\">" +
                "        <packagedElement xmi:type=\"uml:Class\" xmi:id=\"e2\" name=\"Widget\" />" +
                "      </packagedElement>" +
                "    </packagedElement>" +
                "  </uml:Model>" +
                "  <xmi:Extension extender=\"Enterprise Architect\" extenderID=\"6.5\">" +
                "    <elements><element xmi:idref=\"e1\" /><element xmi:idref=\"e2\" /></elements>" +
                "    <connectors />" +
                "  </xmi:Extension>" +
                "</xmi:XMI>");
        }

        private static XDocument CreateMonolithWithoutRoot()
        {
            return XDocument.Parse(
                $"<xmi:XMI xmlns:xmi=\"{XmiNamespace}\" xmlns:uml=\"{UmlNamespace}\">" +
                "  <uml:Model xmi:type=\"uml:Model\" name=\"EA_Model\">" +
                "    <packagedElement name=\"Some Other Root\" />" +
                "  </uml:Model>" +
                "</xmi:XMI>");
        }
    }
}
