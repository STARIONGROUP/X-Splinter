// ------------------------------------------------------------------------------------------------
// <copyright file="XmiModelLoader.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Text;
    using System.Xml;

    using Microsoft.Extensions.Logging;

    using uml4net.xmi;
    using uml4net.xmi.Extensions.EnterpriseArchitect.Extender;
    using uml4net.xmi.Extensions.EnterpriseArchitect.Structure.Readers;

    /// <summary>
    /// Default <see cref="IXmiModelLoader"/> implementation that reads an XMI export using UML4NET.
    /// </summary>
    /// <remarks>
    /// The Enterprise Architect extender and extension content readers are always registered. They
    /// are inert for documents that carry no Enterprise Architect extension, so the same loader
    /// serves both EA exports and plain UML XMI produced by other tools.
    /// </remarks>
    public class XmiModelLoader : IXmiModelLoader
    {
        /// <summary>
        /// The UML namespace URI assumed when the source document declares no <c>uml</c> prefix.
        /// </summary>
        public const string DefaultUmlNamespaceUri = "http://www.omg.org/spec/UML/20161101";

        /// <summary>
        /// The <see cref="ILoggerFactory"/> used to set up logging for the UML4NET reader.
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmiModelLoader"/> class.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to set up logging for the UML4NET reader.
        /// </param>
        public XmiModelLoader(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = new NonDisposingLoggerFactory(loggerFactory);
        }

        /// <inheritdoc />
        public LoadedModel Load(string path)
        {
            var umlNamespaceUri = QueryUmlNamespaceUri(path);

            using var scope = XmiReaderBuilder.Create()
                .WithLogger(this.loggerFactory)
                .WithExtender<EnterpriseArchitectExtenderReader>()
                .WithExtensionContentReaderFacade<ExtensionContentReaderFacade>();

            return new LoadedModel(scope.Build().Read(path), umlNamespaceUri);
        }

        /// <summary>
        /// Reads the UML namespace URI declared on the root element of the document, so that the
        /// split documents are written against the same version of UML as the input.
        /// </summary>
        /// <param name="path">
        /// The path to the XMI file.
        /// </param>
        /// <returns>
        /// The declared UML namespace URI, or <see cref="DefaultUmlNamespaceUri"/> when none is found.
        /// </returns>
        private static string QueryUmlNamespaceUri(string path)
        {
            // Enterprise Architect exports declare windows-1252, which is not available on .NET
            // without registering the code pages provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                var declared = reader.GetAttribute("xmlns:uml");

                return string.IsNullOrEmpty(declared) ? DefaultUmlNamespaceUri : declared;
            }

            return DefaultUmlNamespaceUri;
        }
    }
}
