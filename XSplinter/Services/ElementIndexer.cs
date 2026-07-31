// ------------------------------------------------------------------------------------------------
// <copyright file="ElementIndexer.cs" company="Starion Group S.A.">
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
    /// Builds an index that maps every <c>xmi:id</c> found within a package subtree
    /// to its owning package, enabling cross-package reference detection.
    /// </summary>
    public class ElementIndexer : IElementIndexer
    {
        /// <summary>
        /// The XMI namespace URI.
        /// </summary>
        private readonly XNamespace xmi;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementIndexer"/> class.
        /// </summary>
        /// <param name="xmiNamespace">
        /// The XMI namespace used in the source document.
        /// </param>
        public ElementIndexer(XNamespace xmiNamespace)
        {
            this.xmi = xmiNamespace;
        }

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
        public void IndexElementIds(XElement element, string packageName, string outputFile, Dictionary<string, PackageEntry> index)
        {
            var id = (string?)element.Attribute(this.xmi + "id");

            if (id != null)
            {
                index[id] = new PackageEntry(packageName, outputFile);
            }

            foreach (var child in element.Elements())
            {
                this.IndexElementIds(child, packageName, outputFile, index);
            }
        }

        /// <summary>
        /// Collects all <c>xmi:id</c> and <c>xmi:idref</c> attribute values
        /// from the given element and its descendants into a set.
        /// This is used to determine which EA Extension entries belong to a package.
        /// </summary>
        /// <param name="element">
        /// The root element of the package subtree to scan.
        /// </param>
        /// <param name="ids">
        /// The set to populate with all discovered identifiers.
        /// </param>
        public void CollectAllIds(XElement element, HashSet<string> ids)
        {
            var id = (string?)element.Attribute(this.xmi + "id");

            if (id != null)
            {
                ids.Add(id);
            }

            var idref = (string?)element.Attribute(this.xmi + "idref");

            if (idref != null)
            {
                ids.Add(idref);
            }

            foreach (var child in element.Elements())
            {
                this.CollectAllIds(child, ids);
            }
        }
    }
}
