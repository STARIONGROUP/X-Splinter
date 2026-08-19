// ------------------------------------------------------------------------------------------------
// <copyright file="ConstraintRelocator.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Linq;

    using uml4net.Packages;

    /// <summary>
    /// Default <see cref="IConstraintRelocator"/> implementation that decides the target package
    /// from the document name stamped on the constrained elements.
    /// </summary>
    public class ConstraintRelocator : IConstraintRelocator
    {
        /// <inheritdoc />
        public IReadOnlyList<ConstraintRelocation> Relocate(IPackage rootPackage, IEnumerable<IPackage> targetPackages)
        {
            var relocations = new List<ConstraintRelocation>();

            if (rootPackage?.OwnedRule == null)
            {
                return relocations;
            }

            var packages = targetPackages.ToList();

            foreach (var constraint in rootPackage.OwnedRule.ToList())
            {
                var documentNames = constraint.ConstrainedElement
                    .Where(element => !string.IsNullOrEmpty(element?.DocumentName))
                    .Select(element => element.DocumentName)
                    .Distinct()
                    .ToList();

                if (documentNames.Count != 1)
                {
                    continue;
                }

                var target = packages.FirstOrDefault(package => package.DocumentName == documentNames[0]);

                if (target == null)
                {
                    continue;
                }

                rootPackage.OwnedRule.Remove(constraint);
                target.OwnedRule.Add(constraint);

                relocations.Add(new ConstraintRelocation(constraint, target));
            }

            return relocations;
        }
    }
}
