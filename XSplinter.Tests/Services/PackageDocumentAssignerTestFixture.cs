// ------------------------------------------------------------------------------------------------
// <copyright file="PackageDocumentAssignerTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using NUnit.Framework;

    using uml4net.Classification;
    using uml4net.Packages;
    using uml4net.SimpleClassifiers;
    using uml4net.StructuredClassifiers;

    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="PackageDocumentAssigner"/> class.
    /// </summary>
    [TestFixture]
    public class PackageDocumentAssignerTestFixture
    {
        private PackageDocumentAssigner packageDocumentAssigner;

        [SetUp]
        public void Setup()
        {
            this.packageDocumentAssigner = new PackageDocumentAssigner();
        }

        [Test]
        public void Verify_that_contained_elements_are_stamped_with_the_document_name()
        {
            var package = new Package { XmiId = "pkg", Name = "Forge", DocumentName = "Monolith.xmi" };
            var @class = new Class { XmiId = "c1", Name = "Asset", DocumentName = "Monolith.xmi" };

            package.PackagedElement.Add(@class);

            var ids = this.packageDocumentAssigner.Assign(package, "Forge.xmi");

            Assert.Multiple(() =>
            {
                Assert.That(package.DocumentName, Is.EqualTo("Forge.xmi"));
                Assert.That(@class.DocumentName, Is.EqualTo("Forge.xmi"));
                Assert.That(ids.ContainedIds, Is.EquivalentTo(new[] { "pkg", "c1" }));
            });
        }

        [Test]
        public void Verify_that_a_referenced_element_is_recorded_but_not_stamped()
        {
            var external = new PrimitiveType { XmiId = "ext", Name = "DateTime", DocumentName = "Primitives.xmi" };

            var property = new Property { XmiId = "p1", Name = "created", DocumentName = "Monolith.xmi", Type = external };
            var @class = new Class { XmiId = "c1", Name = "Asset", DocumentName = "Monolith.xmi" };
            @class.OwnedAttribute.Add(property);

            var package = new Package { XmiId = "pkg", Name = "Forge", DocumentName = "Monolith.xmi" };
            package.PackagedElement.Add(@class);

            var ids = this.packageDocumentAssigner.Assign(package, "Forge.xmi");

            Assert.Multiple(() =>
            {
                // the referenced type lives in another document and must keep pointing at it,
                // otherwise the emitted href would target the wrong file
                Assert.That(external.DocumentName, Is.EqualTo("Primitives.xmi"));
                Assert.That(ids.ContainedIds, Does.Contain("p1"));
                Assert.That(ids.ContainedIds, Does.Not.Contain("ext"));
                Assert.That(ids.ReferencedIds, Does.Contain("ext"));
                Assert.That(ids.All(), Does.Contain("ext"));
            });
        }
    }
}
