// ------------------------------------------------------------------------------------------------
// <copyright file="IExtensionBuilder.cs" company="Starion Group S.A.">
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
    /// Defines the contract for building the filtered EA <c>xmi:Extension</c> section
    /// for each split XMI file.
    /// </summary>
    public interface IExtensionBuilder
    {
        /// <summary>
        /// Maps each connector (by its <c>xmi:idref</c>) to the package that owns it.
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
        Dictionary<string, string> BuildConnectorPackageMap(
            List<XElement> connectors,
            Dictionary<string, HashSet<string>> packageElementIds,
            IEnumerable<string> packageNames);

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
        XElement Build(
            string packageName,
            HashSet<string> packageIds,
            List<XElement> allElements,
            List<XElement> allConnectors,
            Dictionary<string, string> connectorPackageMap);
    }
}
