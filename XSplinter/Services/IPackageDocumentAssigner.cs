// ------------------------------------------------------------------------------------------------
// <copyright file="IPackageDocumentAssigner.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using uml4net;

    /// <summary>
    /// Assigns the target document name to every element contained by a package subtree.
    /// </summary>
    /// <remarks>
    /// UML4NET resolves a cross-document reference as <c>{DocumentName}#{XmiId}</c>, so stamping
    /// each package subtree with its output file is what makes references from other packages
    /// resolve to the correct split file rather than to the original monolithic document.
    /// </remarks>
    public interface IPackageDocumentAssigner
    {
        /// <summary>
        /// Recursively sets the <see cref="IXmiElement.DocumentName"/> of the given element and
        /// all of the elements it contains, and collects the encountered identifiers.
        /// </summary>
        /// <param name="element">
        /// The root element of the package subtree.
        /// </param>
        /// <param name="documentName">
        /// The name of the document the subtree is written to.
        /// </param>
        /// <returns>
        /// The <see cref="PackageElementIds"/> describing the contained and referenced identifiers.
        /// </returns>
        PackageElementIds Assign(IXmiElement element, string documentName);
    }
}
