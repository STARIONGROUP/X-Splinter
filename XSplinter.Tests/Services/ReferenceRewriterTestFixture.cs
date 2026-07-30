// ------------------------------------------------------------------------------------------------
// <copyright file="ReferenceRewriterTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    using NUnit.Framework;

    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="ReferenceRewriter"/> class.
    /// </summary>
    [TestFixture]
    public class ReferenceRewriterTestFixture
    {
        private const string XmiNamespace = "http://www.omg.org/spec/XMI/20131001";

        private XNamespace xmi;
        private ReferenceRewriter referenceRewriter;

        [SetUp]
        public void Setup()
        {
            this.xmi = XmiNamespace;
            this.referenceRewriter = new ReferenceRewriter(XmiNamespace);
        }

        [Test]
        public void Verify_that_a_cross_package_type_reference_is_rewritten_to_an_href()
        {
            var element = XElement.Parse(
                $"<packagedElement xmlns:xmi=\"{XmiNamespace}\" xmi:id=\"local\"><type xmi:idref=\"externalId\" /></packagedElement>");

            var index = new Dictionary<string, PackageEntry>
            {
                ["externalId"] = new PackageEntry("Primitives", "CSharp_Primitives.xmi")
            };

            this.referenceRewriter.Rewrite(element, "Forge", index);

            var type = element.Element("type")!;

            Assert.Multiple(() =>
            {
                Assert.That((string)type.Attribute(this.xmi + "idref"), Is.Null);
                Assert.That((string)type.Attribute("href"), Is.EqualTo("CSharp_Primitives.xmi#externalId"));
            });
        }

        [Test]
        public void Verify_that_an_intra_package_reference_is_left_untouched()
        {
            var element = XElement.Parse(
                $"<packagedElement xmlns:xmi=\"{XmiNamespace}\" xmi:id=\"local\"><type xmi:idref=\"forgeId\" /></packagedElement>");

            var index = new Dictionary<string, PackageEntry>
            {
                ["forgeId"] = new ("Forge", "Forge.xmi")
            };

            this.referenceRewriter.Rewrite(element, "Forge", index);

            var type = element.Element("type")!;

            Assert.Multiple(() =>
            {
                Assert.That((string)type.Attribute(this.xmi + "idref"), Is.EqualTo("forgeId"));
                Assert.That((string)type.Attribute("href"), Is.Null);
            });
        }

        [Test]
        public void Verify_that_a_cross_package_constrainedElement_reference_is_rewritten()
        {
            var element = XElement.Parse(
                $"<ownedRule xmlns:xmi=\"{XmiNamespace}\"><constrainedElement xmi:idref=\"externalId\" /></ownedRule>");

            var index = new Dictionary<string, PackageEntry>
            {
                ["externalId"] = new ("Primitives", "CSharp_Primitives.xmi")
            };

            this.referenceRewriter.Rewrite(element, "Forge", index);

            var constrained = element.Element("constrainedElement")!;

            Assert.That((string)constrained.Attribute("href"), Is.EqualTo("CSharp_Primitives.xmi#externalId"));
        }

        [Test]
        public void Verify_that_a_reference_to_an_unknown_id_is_left_untouched()
        {
            var element = XElement.Parse(
                $"<packagedElement xmlns:xmi=\"{XmiNamespace}\"><type xmi:idref=\"unknown\" /></packagedElement>");

            this.referenceRewriter.Rewrite(element, "Forge", new Dictionary<string, PackageEntry>());

            var type = element.Element("type")!;

            Assert.Multiple(() =>
            {
                Assert.That((string)type.Attribute(this.xmi + "idref"), Is.EqualTo("unknown"));
                Assert.That((string)type.Attribute("href"), Is.Null);
            });
        }
    }
}
