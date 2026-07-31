// ------------------------------------------------------------------------------------------------
// <copyright file="IReferenceRewriter.cs" company="Starion Group S.A.">
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
    /// Defines the contract for rewriting cross-package <c>xmi:idref</c> references
    /// into cross-file <c>href</c> references.
    /// </summary>
    public interface IReferenceRewriter
    {
        /// <summary>
        /// Recursively walks the given element tree and rewrites any <c>xmi:idref</c>
        /// on <c>type</c> or <c>constrainedElement</c> nodes that reference an element
        /// in a different package into an <c>href="filename.xmi#id"</c> attribute.
        /// Intra-package references are left unchanged.
        /// </summary>
        /// <param name="element">
        /// The root element of the cloned package subtree to rewrite.
        /// </param>
        /// <param name="currentPackageName">
        /// The name of the package being written, used to detect cross-package references.
        /// </param>
        /// <param name="elementIndex">
        /// The element index mapping each <c>xmi:id</c> to its owning <see cref="PackageEntry"/>.
        /// </param>
        void Rewrite(XElement element, string currentPackageName, IReadOnlyDictionary<string, PackageEntry> elementIndex);
    }
}
