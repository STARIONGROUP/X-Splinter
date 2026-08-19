// ------------------------------------------------------------------------------------------------
// <copyright file="ExtensionBuilder.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using uml4net;
    using uml4net.xmi.Extensions.EnterpriseArchitect.Structure;

    /// <summary>
    /// Builds the <c>xmi:Extension</c> content for each split XMI file, filtering the
    /// EA-proprietary elements and connectors to include only those belonging to the
    /// target package.
    /// </summary>
    /// <remarks>
    /// Membership is decided on the parsed Enterprise Architect structures
    /// (<see cref="Element"/>, <see cref="Connector"/> and their resolved <c>ExtendedElement</c>).
    /// The selected entries are sliced out of <see cref="XmiExtension.ContentRawXmi"/>, since the
    /// UML4NET writer emits that raw markup verbatim and ignores <see cref="XmiExtension.Content"/>.
    /// </remarks>
    public class ExtensionBuilder : IExtensionBuilder
    {
        /// <summary>
        /// The XMI namespace URI.
        /// </summary>
        private static readonly XNamespace Xmi = "http://www.omg.org/spec/XMI/20131001";

        /// <summary>
        /// The local name of the <c>xmi:idref</c> attribute.
        /// </summary>
        private const string IdRefAttribute = "idref";

        /// <inheritdoc />
        public bool CanFilter(XmiExtension extension)
        {
            return extension?.Content != null
                   && extension.Content.Exists(item => item is Element or Connector);
        }

        /// <inheritdoc />
        public Dictionary<string, string> BuildConnectorPackageMap(
            XmiExtension extension,
            Dictionary<string, HashSet<string>> packageElementIds,
            IEnumerable<string> packageNames)
        {
            var connectorPackageMap = new Dictionary<string, string>();

            if (extension?.Content == null)
            {
                return connectorPackageMap;
            }

            var names = packageNames.ToList();

            foreach (var connector in extension.Content.OfType<Connector>())
            {
                var connectorId = connector.ExtendedElement?.XmiId;

                if (string.IsNullOrEmpty(connectorId))
                {
                    continue;
                }

                var sourceId = connector.Source?.ExtendedElement?.XmiId ?? "";
                var targetId = connector.Target?.ExtendedElement?.XmiId ?? "";

                foreach (var packageName in names)
                {
                    var ids = packageElementIds[packageName];

                    if (ids.Contains(sourceId) || ids.Contains(targetId))
                    {
                        connectorPackageMap[connectorId] = packageName;

                        if (ids.Contains(sourceId) && ids.Contains(targetId))
                        {
                            break;
                        }
                    }
                }
            }

            return connectorPackageMap;
        }

        /// <inheritdoc />
        public XmiExtension Build(
            XmiExtension extension,
            string packageName,
            HashSet<string> packageIds,
            Dictionary<string, string> connectorPackageMap)
        {
            if (!this.CanFilter(extension))
            {
                // the extension belongs to another tool and its content cannot be attributed to a
                // package; it is emitted unchanged so that no information is lost
                return extension;
            }

            var elementIds = extension?.Content?
                .OfType<Element>()
                .Select(element => element.ExtendedElement?.XmiId)
                .Where(id => !string.IsNullOrEmpty(id) && packageIds.Contains(id))
                .ToHashSet() ?? [];

            var connectorIds = connectorPackageMap
                .Where(entry => entry.Value == packageName)
                .Select(entry => entry.Key)
                .ToHashSet();

            return new XmiExtension
            {
                Extender = extension?.Extender,
                ExtenderId = extension?.ExtenderId,
                ContentRawXmi = this.SliceContent(extension, elementIds, connectorIds)
            };
        }

        /// <summary>
        /// Slices the raw extension markup down to the selected elements and connectors.
        /// </summary>
        /// <param name="extension">
        /// The <see cref="XmiExtension"/> holding the raw content of the source document.
        /// </param>
        /// <param name="elementIds">
        /// The identifiers of the Enterprise Architect elements to retain.
        /// </param>
        /// <param name="connectorIds">
        /// The identifiers of the Enterprise Architect connectors to retain.
        /// </param>
        /// <returns>
        /// The filtered raw markup that is emitted inside the <c>xmi:Extension</c> element.
        /// </returns>
        private string SliceContent(XmiExtension extension, HashSet<string> elementIds, HashSet<string> connectorIds)
        {
            var content = ParseContent(extension);

            var elements = content?
                .Elements("elements")
                .Elements("element")
                .Where(element => elementIds.Contains((string)element.Attribute(Xmi + IdRefAttribute) ?? ""))
                .ToList() ?? [];

            var connectors = content?
                .Elements("connectors")
                .Elements("connector")
                .Where(connector => connectorIds.Contains((string)connector.Attribute(Xmi + IdRefAttribute) ?? ""))
                .ToList() ?? [];

            var filtered = new XElement("content",
                new XElement("elements", elements),
                new XElement("connectors", connectors),
                new XElement("primitivetypes",
                    new XElement("packagedElement",
                        new XAttribute(XNamespace.Xmlns + "xmi", Xmi),
                        new XAttribute(Xmi + "type", "uml:Package"),
                        new XAttribute(Xmi + "id", "EAPrimitiveTypesPackage"),
                        new XAttribute("name", "EA_PrimitiveTypes_Package"))),
                new XElement("profiles"));

            return string.Concat(filtered.Elements().Select(element => element.ToString()));
        }

        /// <summary>
        /// Parses the raw extension content into an <see cref="XElement"/> wrapper so that the
        /// <c>elements</c> and <c>connectors</c> sections can be queried.
        /// </summary>
        /// <param name="extension">
        /// The <see cref="XmiExtension"/> whose content is parsed.
        /// </param>
        /// <returns>
        /// The wrapper element, or <c>null</c> when the extension holds no raw content.
        /// </returns>
        private static XElement ParseContent(XmiExtension extension)
        {
            if (string.IsNullOrEmpty(extension?.ContentRawXmi))
            {
                return null;
            }

            return XElement.Parse(
                $"<content xmlns:xmi=\"{Xmi}\">{extension.ContentRawXmi}</content>",
                LoadOptions.PreserveWhitespace);
        }
    }
}
