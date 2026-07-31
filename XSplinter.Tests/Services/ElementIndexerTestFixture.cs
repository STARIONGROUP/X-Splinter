// ------------------------------------------------------------------------------------------------
// <copyright file="ElementIndexerTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    using NUnit.Framework;

    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="ElementIndexer"/> class.
    /// </summary>
    [TestFixture]
    public class ElementIndexerTestFixture
    {
        private const string XmiNamespace = "http://www.omg.org/spec/XMI/20131001";

        private ElementIndexer elementIndexer;
        private static readonly string[] Expected = ["p1", "r1", "c1", "r2"];
        private static readonly string[] ExpectedArray = ["p1", "c1", "c2"];

        [SetUp]
        public void Setup()
        {
            this.elementIndexer = new ElementIndexer(XmiNamespace);
        }

        [Test]
        public void Verify_that_IndexElementIds_maps_every_xmi_id_to_the_owning_package()
        {
            var element = XElement.Parse(
                $"<packagedElement xmlns:xmi=\"{XmiNamespace}\" xmi:id=\"p1\">" +
                "  <packagedElement xmi:id=\"c1\" />" +
                "  <ownedAttribute xmi:id=\"c2\"><type xmi:idref=\"external\" /></ownedAttribute>" +
                "</packagedElement>");

            var index = new Dictionary<string, PackageEntry>();

            this.elementIndexer.IndexElementIds(element, "Forge", "Forge.xmi", index);

            Assert.Multiple(() =>
            {
                Assert.That(index.Keys, Is.EquivalentTo(ExpectedArray));
                Assert.That(index["p1"], Is.EqualTo(new PackageEntry("Forge", "Forge.xmi")));
                Assert.That(index["c1"].OutputFile, Is.EqualTo("Forge.xmi"));
            });
        }

        [Test]
        public void Verify_that_CollectAllIds_gathers_both_ids_and_idrefs()
        {
            var element = XElement.Parse(
                $"<packagedElement xmlns:xmi=\"{XmiNamespace}\" xmi:id=\"p1\">" +
                "  <type xmi:idref=\"r1\" />" +
                "  <packagedElement xmi:id=\"c1\"><type xmi:idref=\"r2\" /></packagedElement>" +
                "</packagedElement>");

            var ids = new HashSet<string>();

            this.elementIndexer.CollectAllIds(element, ids);

            Assert.That(ids, Is.EquivalentTo(Expected));
        }
    }
}
