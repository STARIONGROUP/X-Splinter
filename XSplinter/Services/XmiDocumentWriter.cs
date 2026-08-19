// ------------------------------------------------------------------------------------------------
// <copyright file="XmiDocumentWriter.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.Extensions.Logging;

    using uml4net;
    using uml4net.Packages;
    using uml4net.xmi;
    using uml4net.xmi.Settings;
    using uml4net.xmi.Xmi;

    /// <summary>
    /// Default <see cref="IXmiDocumentWriter"/> implementation backed by the UML4NET
    /// <c>XmiWriter</c>, configured to resolve external references as <c>href</c> attributes.
    /// </summary>
    public class XmiDocumentWriter : IXmiDocumentWriter
    {
        /// <summary>
        /// The <see cref="ILoggerFactory"/> used to set up logging for the UML4NET writer.
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiDocumentWriter"/> class.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to set up logging for the UML4NET writer.
        /// </param>
        public XmiDocumentWriter(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = new NonDisposingLoggerFactory(loggerFactory);
        }

        /// <inheritdoc />
        public void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <inheritdoc />
        public void Write(IPackage package, string path, Documentation documentation, IEnumerable<XmiExtension> extensions, string umlNamespaceUri)
        {
            using var scope = XmiWriterBuilder.Create()
                .WithLogger(this.loggerFactory)
                .UsingSettings(settings =>
                {
                    settings.ExternalReferenceResolution = ExternalReferenceResolutionKind.Href;
                    settings.UmlNamespaceUri = umlNamespaceUri;
                    settings.Indent = true;
                });

            var writer = scope.Build();

            if (extensions == null)
            {
                writer.Write(package, path);
            }
            else if (documentation == null)
            {
                writer.Write(package, path, extensions);
            }
            else
            {
                writer.Write(package, path, documentation, extensions);
            }
        }
    }
}
