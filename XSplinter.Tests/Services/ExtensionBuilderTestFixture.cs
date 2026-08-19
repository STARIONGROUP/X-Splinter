// ------------------------------------------------------------------------------------------------
// <copyright file="ExtensionBuilderTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using NUnit.Framework;

    using uml4net;
    using uml4net.StructuredClassifiers;
    

    using XSplinter.Services;

    using EaElement = uml4net.xmi.Extensions.EnterpriseArchitect.Structure.Element;
    using EaConnector = uml4net.xmi.Extensions.EnterpriseArchitect.Structure.Connector;

    /// <summary>
    /// Suite of tests for the <see cref="ExtensionBuilder"/> class.
    /// </summary>
    [TestFixture]
    public class ExtensionBuilderTestFixture
    {
        private const string XmiNamespace = "http://www.omg.org/spec/XMI/20131001";

        private static readonly string[] PackageNames = ["Forge", "Primitives"];

        private ExtensionBuilder extensionBuilder;
        private XmiExtension extension;

        [SetUp]
        public void Setup()
        {
            this.extensionBuilder = new ExtensionBuilder();

            this.extension = new XmiExtension
            {
                Extender = "Enterprise Architect",
                ExtenderId = "6.5",
                Content =
                [
                    new EaElement { Name = "Asset", ExtendedElement = new Class { XmiId = "e1" } },
                    new EaElement { Name = "Widget", ExtendedElement = new Class { XmiId = "e2" } },
                    CreateConnector("c1", "a", "b"),
                    CreateConnector("c2", "x", "x")
                ],
                // the UML4NET reader declares the xmi prefix on each entry of the raw content,
                // which is what keeps the prefix intact when the entries are sliced out
                ContentRawXmi =
                    "<elements>" +
                    $"  <element xmi:idref=\"e1\" xmlns:xmi=\"{XmiNamespace}\" />" +
                    $"  <element xmi:idref=\"e2\" xmlns:xmi=\"{XmiNamespace}\" />" +
                    "</elements>" +
                    "<connectors>" +
                    $"  <connector xmi:idref=\"c1\" xmlns:xmi=\"{XmiNamespace}\" />" +
                    $"  <connector xmi:idref=\"c2\" xmlns:xmi=\"{XmiNamespace}\" />" +
                    "</connectors>"
            };
        }

        [Test]
        public void Verify_that_a_connector_is_assigned_to_the_package_owning_its_endpoints()
        {
            var packageElementIds = new Dictionary<string, HashSet<string>>
            {
                ["Forge"] = ["a", "b"],
                ["Primitives"] = ["x"]
            };

            var map = this.extensionBuilder.BuildConnectorPackageMap(this.extension, packageElementIds, PackageNames);

            Assert.Multiple(() =>
            {
                Assert.That(map["c1"], Is.EqualTo("Forge"));
                Assert.That(map["c2"], Is.EqualTo("Primitives"));
            });
        }

        [Test]
        public void Verify_that_Build_filters_elements_and_connectors_to_the_target_package()
        {
            var connectorPackageMap = new Dictionary<string, string>
            {
                ["c1"] = "Forge",
                ["c2"] = "Primitives"
            };

            var built = this.extensionBuilder.Build(this.extension, "Forge", ["e1"], connectorPackageMap);

            Assert.Multiple(() =>
            {
                Assert.That(built.Extender, Is.EqualTo("Enterprise Architect"));
                Assert.That(built.ExtenderId, Is.EqualTo("6.5"));
                Assert.That(built.ContentRawXmi, Does.Contain("xmi:idref=\"e1\""));
                Assert.That(built.ContentRawXmi, Does.Not.Contain("xmi:idref=\"e2\""));
                Assert.That(built.ContentRawXmi, Does.Contain("xmi:idref=\"c1\""));
                Assert.That(built.ContentRawXmi, Does.Not.Contain("xmi:idref=\"c2\""));
            });
        }

        [Test]
        public void Verify_that_the_EA_primitive_types_stub_keeps_the_xmi_prefix()
        {
            var built = this.extensionBuilder.Build(this.extension, "Forge", [], []);

            Assert.Multiple(() =>
            {
                Assert.That(built.ContentRawXmi, Does.Contain("xmi:id=\"EAPrimitiveTypesPackage\""));
                Assert.That(built.ContentRawXmi, Does.Contain("<profiles />"));
            });
        }

        [Test]
        public void Verify_that_an_enterprise_architect_extension_can_be_filtered()
        {
            Assert.That(this.extensionBuilder.CanFilter(this.extension), Is.True);
        }

        [Test]
        public void Verify_that_an_extension_of_another_tool_is_passed_through_unchanged()
        {
            // the content of a foreign extension cannot be attributed to a package, so it is
            // emitted as-is rather than silently dropped
            var foreign = new XmiExtension
            {
                Extender = "Some Other Tool",
                ExtenderId = "1.0",
                ContentRawXmi = "<somethingElse />"
            };

            var map = this.extensionBuilder.BuildConnectorPackageMap(foreign, [], PackageNames);
            var built = this.extensionBuilder.Build(foreign, "Forge", [], []);

            Assert.Multiple(() =>
            {
                Assert.That(this.extensionBuilder.CanFilter(foreign), Is.False);
                Assert.That(map, Is.Empty);
                Assert.That(built, Is.SameAs(foreign));
                Assert.That(built.ContentRawXmi, Is.EqualTo("<somethingElse />"));
            });
        }

        [Test]
        public void Verify_that_a_connector_without_a_resolved_element_is_ignored()
        {
            var extensionWithUnresolvedConnector = new XmiExtension
            {
                Extender = "Enterprise Architect",
                Content = [new EaConnector { Name = "dangling" }]
            };

            var map = this.extensionBuilder.BuildConnectorPackageMap(
                extensionWithUnresolvedConnector,
                new Dictionary<string, HashSet<string>> { ["Forge"] = ["a"] },
                PackageNames);

            Assert.That(map, Is.Empty);
        }

        private static EaConnector CreateConnector(string id, string sourceId, string targetId)
        {
            return new EaConnector
            {
                ExtendedElement = new Class { XmiId = id },
                Source = new uml4net.xmi.Extensions.EnterpriseArchitect.Structure.ConnectorEnd { ExtendedElement = new Class { XmiId = sourceId } },
                Target = new uml4net.xmi.Extensions.EnterpriseArchitect.Structure.ConnectorEnd { ExtendedElement = new Class { XmiId = targetId } }
            };
        }
    }
}
