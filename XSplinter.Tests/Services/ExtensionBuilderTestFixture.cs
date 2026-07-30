// ------------------------------------------------------------------------------------------------
// <copyright file="ExtensionBuilderTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using NUnit.Framework;

    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="ExtensionBuilder"/> class.
    /// </summary>
    [TestFixture]
    public class ExtensionBuilderTestFixture
    {
        private const string XmiNamespace = "http://www.omg.org/spec/XMI/20131001";

        private XNamespace xmi;
        private ExtensionBuilder extensionBuilder;

        [SetUp]
        public void Setup()
        {
            this.xmi = XmiNamespace;
            this.extensionBuilder = new ExtensionBuilder(XmiNamespace);
        }

        [Test]
        public void Verify_that_a_connector_is_assigned_to_the_package_owning_its_endpoints()
        {
            var connectors = new List<XElement>
            {
                XElement.Parse($"<connector xmlns:xmi=\"{XmiNamespace}\" xmi:idref=\"conn1\"><source xmi:idref=\"a\" /><target xmi:idref=\"b\" /></connector>")
            };

            var packageElementIds = new Dictionary<string, HashSet<string>>
            {
                ["Forge"] = ["a", "b"],
                ["Primitives"] = ["x"]
            };

            var map = this.extensionBuilder.BuildConnectorPackageMap(connectors, packageElementIds, new[] { "Forge", "Primitives" });

            Assert.That(map["conn1"], Is.EqualTo("Forge"));
        }

        [Test]
        public void Verify_that_Build_filters_elements_and_connectors_to_the_target_package()
        {
            var allElements = new List<XElement>
            {
                XElement.Parse($"<element xmlns:xmi=\"{XmiNamespace}\" xmi:idref=\"e1\" />"),
                XElement.Parse($"<element xmlns:xmi=\"{XmiNamespace}\" xmi:idref=\"e2\" />")
            };

            var allConnectors = new List<XElement>
            {
                XElement.Parse($"<connector xmlns:xmi=\"{XmiNamespace}\" xmi:idref=\"c1\" />"),
                XElement.Parse($"<connector xmlns:xmi=\"{XmiNamespace}\" xmi:idref=\"c2\" />")
            };

            var connectorPackageMap = new Dictionary<string, string>
            {
                ["c1"] = "Forge",
                ["c2"] = "Primitives"
            };

            var packageIds = new HashSet<string> { "e1" };

            var extension = this.extensionBuilder.Build("Forge", packageIds, allElements, allConnectors, connectorPackageMap);

            var elements = extension.Element("elements")!.Elements("element").ToList();
            var connectors = extension.Element("connectors")!.Elements("connector").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(extension.Name, Is.EqualTo(this.xmi + "Extension"));
                Assert.That(elements, Has.Count.EqualTo(1));
                Assert.That((string)elements[0].Attribute(this.xmi + "idref"), Is.EqualTo("e1"));
                Assert.That(connectors, Has.Count.EqualTo(1));
                Assert.That((string)connectors[0].Attribute(this.xmi + "idref"), Is.EqualTo("c1"));
                Assert.That(extension.Element("primitivetypes"), Is.Not.Null);
                Assert.That(extension.Element("profiles"), Is.Not.Null);
            });
        }
    }
}
