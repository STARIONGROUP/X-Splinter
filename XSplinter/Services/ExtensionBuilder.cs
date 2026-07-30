// ------------------------------------------------------------------------------------------------
// <copyright file="ExtensionBuilder.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Builds the <c>xmi:Extension</c> section for each split XMI file,
    /// filtering the EA-proprietary elements and connectors to include only
    /// those belonging to the target package.
    /// </summary>
    public class ExtensionBuilder : IExtensionBuilder
    {
        /// <summary>
        /// The XMI namespace URI.
        /// </summary>
        private readonly XNamespace xmi;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionBuilder"/> class.
        /// </summary>
        /// <param name="xmiNamespace">
        /// The XMI namespace used in the source document.
        /// </param>
        public ExtensionBuilder(XNamespace xmiNamespace)
        {
            this.xmi = xmiNamespace;
        }

        /// <summary>
        /// Maps each connector (by its <c>xmi:idref</c>) to the package that owns it.
        /// A connector is assigned to the package that contains both its source and target,
        /// or to the first package that contains at least one endpoint.
        /// </summary>
        /// <param name="connectors">
        /// The list of connector elements from the EA Extension section.
        /// </param>
        /// <param name="packageElementIds">
        /// A dictionary mapping each package name to the set of element IDs it contains.
        /// </param>
        /// <param name="packageNames">
        /// The ordered list of package names to iterate over when assigning ownership.
        /// </param>
        /// <returns>
        /// A dictionary mapping each connector's <c>xmi:idref</c> to its owning package name.
        /// </returns>
        public Dictionary<string, string> BuildConnectorPackageMap(
            List<XElement> connectors,
            Dictionary<string, HashSet<string>> packageElementIds,
            IEnumerable<string> packageNames)
        {
            var connectorPackageMap = new Dictionary<string, string>();

            foreach (var connector in connectors)
            {
                var connectorId = (string?)connector.Attribute(this.xmi + "idref") ?? "";
                var sourceId = (string?)connector.Element("source")?.Attribute(this.xmi + "idref") ?? "";
                var targetId = (string?)connector.Element("target")?.Attribute(this.xmi + "idref") ?? "";

                foreach (var packageName in packageNames)
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

        /// <summary>
        /// Builds the <c>xmi:Extension</c> element for a single output XMI file,
        /// filtering elements and connectors to include only those belonging to
        /// the specified package.
        /// </summary>
        /// <param name="packageName">
        /// The name of the package being written.
        /// </param>
        /// <param name="packageIds">
        /// The set of element IDs belonging to this package.
        /// </param>
        /// <param name="allElements">
        /// All element entries from the source EA Extension section.
        /// </param>
        /// <param name="allConnectors">
        /// All connector entries from the source EA Extension section.
        /// </param>
        /// <param name="connectorPackageMap">
        /// The mapping of connector IDs to owning package names.
        /// </param>
        /// <returns>
        /// A new <c>xmi:Extension</c> element containing only the filtered entries.
        /// </returns>
        public XElement Build(
            string packageName,
            HashSet<string> packageIds,
            List<XElement> allElements,
            List<XElement> allConnectors,
            Dictionary<string, string> connectorPackageMap)
        {
            var filteredElements = allElements
                .Where(element =>
                {
                    var idref = (string?)element.Attribute(this.xmi + "idref") ?? "";
                    return packageIds.Contains(idref);
                })
                .Select(element => new XElement(element))
                .ToList();

            var filteredConnectors = allConnectors
                .Where(connector =>
                {
                    var idref = (string?)connector.Attribute(this.xmi + "idref") ?? "";
                    return connectorPackageMap.TryGetValue(idref, out var owner) && owner == packageName;
                })
                .Select(connector => new XElement(connector))
                .ToList();

            return new XElement(this.xmi + "Extension",
                new XAttribute("extender", "Enterprise Architect"),
                new XAttribute("extenderID", "6.5"),
                new XElement("elements", filteredElements),
                new XElement("connectors", filteredConnectors),
                new XElement("primitivetypes",
                    new XElement("packagedElement",
                        new XAttribute(this.xmi + "type", "uml:Package"),
                        new XAttribute(this.xmi + "id", "EAPrimitiveTypesPackage"),
                        new XAttribute("name", "EA_PrimitiveTypes_Package"))),
                new XElement("profiles"));
        }
    }
}
