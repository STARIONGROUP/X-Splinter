// ------------------------------------------------------------------------------------------------
// <copyright file="IElementIndexer.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    /// <summary>
    /// Defines the contract for indexing the <c>xmi:id</c> and <c>xmi:idref</c>
    /// values found within a package subtree.
    /// </summary>
    public interface IElementIndexer
    {
        /// <summary>
        /// Scans the given element and all its descendants, recording every
        /// <c>xmi:id</c> attribute value into the provided index.
        /// </summary>
        /// <param name="element">
        /// The root element of the package subtree to index.
        /// </param>
        /// <param name="packageName">
        /// The name of the owning package.
        /// </param>
        /// <param name="outputFile">
        /// The output filename for the owning package.
        /// </param>
        /// <param name="index">
        /// The dictionary to populate with element-id to <see cref="PackageEntry"/> mappings.
        /// </param>
        void IndexElementIds(XElement element, string packageName, string outputFile, Dictionary<string, PackageEntry> index);

        /// <summary>
        /// Collects all <c>xmi:id</c> and <c>xmi:idref</c> attribute values
        /// from the given element and its descendants into a set.
        /// </summary>
        /// <param name="element">
        /// The root element of the package subtree to scan.
        /// </param>
        /// <param name="ids">
        /// The set to populate with all discovered identifiers.
        /// </param>
        void CollectAllIds(XElement element, HashSet<string> ids);
    }
}
