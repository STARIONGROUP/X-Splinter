// ------------------------------------------------------------------------------------------------
// <copyright file="ConstraintRelocatorTestFixture.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Tests.Services
{
    using NUnit.Framework;

    using uml4net.CommonStructure;
    using uml4net.Packages;
    using uml4net.StructuredClassifiers;

    using XSplinter.Services;

    /// <summary>
    /// Suite of tests for the <see cref="ConstraintRelocator"/> class.
    /// </summary>
    [TestFixture]
    public class ConstraintRelocatorTestFixture
    {
        private ConstraintRelocator constraintRelocator;
        private Package rootPackage;
        private Package forge;
        private Package primitives;
        private Class asset;

        [SetUp]
        public void Setup()
        {
            this.constraintRelocator = new ConstraintRelocator();

            this.forge = new Package { XmiId = "pkgForge", Name = "Forge", DocumentName = "Forge.xmi" };
            this.primitives = new Package { XmiId = "pkgPrim", Name = "Primitives", DocumentName = "CSharp_Primitives.xmi" };

            this.asset = new Class { XmiId = "asset", Name = "Asset", DocumentName = "Forge.xmi" };
            this.forge.PackagedElement.Add(this.asset);

            this.rootPackage = new Package { XmiId = "root", Name = "5. Data Structure", DocumentName = "Monolith.xmi" };
            this.rootPackage.PackagedElement.Add(this.forge);
            this.rootPackage.PackagedElement.Add(this.primitives);
        }

        [Test]
        public void Verify_that_a_constraint_is_moved_to_the_package_of_its_constrained_element()
        {
            var constraint = new Constraint { XmiId = "rule", Name = "assetRule" };
            constraint.ConstrainedElement.Add(this.asset);
            this.rootPackage.OwnedRule.Add(constraint);

            var relocations = this.constraintRelocator.Relocate(this.rootPackage, [this.forge, this.primitives]);

            Assert.Multiple(() =>
            {
                Assert.That(relocations, Has.Count.EqualTo(1));
                Assert.That(relocations[0].TargetPackage, Is.SameAs(this.forge));
                Assert.That(this.forge.OwnedRule, Does.Contain(constraint));
                Assert.That(this.rootPackage.OwnedRule, Is.Empty);
            });
        }

        [Test]
        public void Verify_that_a_constraint_spanning_two_documents_is_left_untouched()
        {
            var external = new Class { XmiId = "ext", Name = "Uri", DocumentName = "CSharp_Primitives.xmi" };

            var constraint = new Constraint { XmiId = "rule", Name = "spanning" };
            constraint.ConstrainedElement.Add(this.asset);
            constraint.ConstrainedElement.Add(external);
            this.rootPackage.OwnedRule.Add(constraint);

            var relocations = this.constraintRelocator.Relocate(this.rootPackage, [this.forge, this.primitives]);

            Assert.Multiple(() =>
            {
                Assert.That(relocations, Is.Empty);
                Assert.That(this.rootPackage.OwnedRule, Does.Contain(constraint));
            });
        }

        [Test]
        public void Verify_that_a_constraint_targeting_no_extracted_package_is_left_untouched()
        {
            var outside = new Class { XmiId = "outside", Name = "Elsewhere", DocumentName = "Other.xmi" };

            var constraint = new Constraint { XmiId = "rule", Name = "outside" };
            constraint.ConstrainedElement.Add(outside);
            this.rootPackage.OwnedRule.Add(constraint);

            var relocations = this.constraintRelocator.Relocate(this.rootPackage, [this.forge, this.primitives]);

            Assert.Multiple(() =>
            {
                Assert.That(relocations, Is.Empty);
                Assert.That(this.rootPackage.OwnedRule, Does.Contain(constraint));
            });
        }

        [Test]
        public void Verify_that_a_package_without_constraints_is_handled()
        {
            Assert.That(this.constraintRelocator.Relocate(this.rootPackage, [this.forge]), Is.Empty);
        }
    }
}
