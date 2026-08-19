// ------------------------------------------------------------------------------------------------
// <copyright file="PackageElementIds.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Holds the identifiers discovered while walking a package subtree.
    /// </summary>
    /// <param name="ContainedIds">
    /// The <c>xmi:id</c> values of the elements that are contained by the package.
    /// </param>
    /// <param name="ReferencedIds">
    /// The <c>xmi:id</c> values of the elements that are referenced, but not contained, by the package.
    /// </param>
    public record PackageElementIds(HashSet<string> ContainedIds, HashSet<string> ReferencedIds)
    {
        /// <summary>
        /// Gets the union of the contained and referenced identifiers, which determines the
        /// Enterprise Architect extension entries that belong to the package.
        /// </summary>
        /// <returns>
        /// The set of all identifiers associated with the package.
        /// </returns>
        public HashSet<string> All()
        {
            return this.ContainedIds.Concat(this.ReferencedIds).ToHashSet();
        }
    }
}
