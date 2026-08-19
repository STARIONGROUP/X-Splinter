// ------------------------------------------------------------------------------------------------
// <copyright file="IXmiDocumentWriter.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;

    using uml4net;
    using uml4net.Packages;
    using uml4net.xmi.Xmi;

    /// <summary>
    /// Writes a UML package to an XMI document using UML4NET.
    /// </summary>
    public interface IXmiDocumentWriter
    {
        /// <summary>
        /// Ensures the given output directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">
        /// The directory path to create.
        /// </param>
        void EnsureDirectory(string path);

        /// <summary>
        /// Writes the given package to the specified path, emitting cross-document references
        /// as <c>href</c> attributes.
        /// </summary>
        /// <param name="package">
        /// The <see cref="IPackage"/> to write. This may be an <see cref="IModel"/> when the
        /// Enterprise Architect model wrapper is required.
        /// </param>
        /// <param name="path">
        /// The destination file path.
        /// </param>
        /// <param name="documentation">
        /// The <see cref="Documentation"/> header to emit, or <c>null</c> when the document is
        /// written without an <c>xmi:Documentation</c> element.
        /// </param>
        /// <param name="extensions">
        /// The <see cref="XmiExtension"/> instances to emit, or <c>null</c> when the document
        /// is written without an extension section.
        /// </param>
        /// <param name="umlNamespaceUri">
        /// The UML namespace URI to declare, matching the version of UML used by the source document.
        /// </param>
        void Write(IPackage package, string path, Documentation documentation, IEnumerable<XmiExtension> extensions, string umlNamespaceUri);
    }
}
