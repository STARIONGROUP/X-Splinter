// ------------------------------------------------------------------------------------------------
// <copyright file="LoadedModel.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using uml4net.xmi.Readers;

    /// <summary>
    /// The result of reading a monolithic XMI document.
    /// </summary>
    /// <param name="ReaderResult">
    /// The <see cref="XmiReaderResult"/> holding the root packages and the <c>xmi:XMI</c> level content.
    /// </param>
    /// <param name="UmlNamespaceUri">
    /// The UML namespace URI declared by the source document, so that the split documents are
    /// written against the same version of UML as the input.
    /// </param>
    public record LoadedModel(XmiReaderResult ReaderResult, string UmlNamespaceUri);
}
