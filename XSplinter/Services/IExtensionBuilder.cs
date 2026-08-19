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

    using uml4net;

    /// <summary>
    /// Builds the filtered Enterprise Architect <c>xmi:Extension</c> for a single output document.
    /// </summary>
    public interface IExtensionBuilder
    {
        /// <summary>
        /// Queries whether the extension can be filtered per package. Only extensions whose content
        /// has been parsed into Enterprise Architect structures are understood; any other extension
        /// is opaque and cannot be split.
        /// </summary>
        /// <param name="extension">
        /// The <see cref="XmiExtension"/> to inspect.
        /// </param>
        /// <returns>
        /// true when the extension content can be filtered, false otherwise.
        /// </returns>
        bool CanFilter(XmiExtension extension);

        /// <summary>
        /// Maps each Enterprise Architect connector to the package that owns it. A connector is
        /// assigned to the package that contains both its source and target, or otherwise to the
        /// first package that contains at least one of its endpoints.
        /// </summary>
        /// <param name="extension">
        /// The <see cref="XmiExtension"/> read from the monolithic document.
        /// </param>
        /// <param name="packageElementIds">
        /// A dictionary mapping each package name to the set of element ids it contains.
        /// </param>
        /// <param name="packageNames">
        /// The ordered list of package names to iterate over when assigning ownership.
        /// </param>
        /// <returns>
        /// A dictionary mapping each connector id to its owning package name.
        /// </returns>
        Dictionary<string, string> BuildConnectorPackageMap(
            XmiExtension extension,
            Dictionary<string, HashSet<string>> packageElementIds,
            IEnumerable<string> packageNames);

        /// <summary>
        /// Builds an <see cref="XmiExtension"/> that contains only the elements and connectors
        /// belonging to the specified package.
        /// </summary>
        /// <param name="extension">
        /// The <see cref="XmiExtension"/> read from the monolithic document.
        /// </param>
        /// <param name="packageName">
        /// The name of the package being written.
        /// </param>
        /// <param name="packageIds">
        /// The set of element ids belonging to the package.
        /// </param>
        /// <param name="connectorPackageMap">
        /// The mapping of connector ids to owning package names.
        /// </param>
        /// <returns>
        /// A new <see cref="XmiExtension"/> holding the filtered content.
        /// </returns>
        XmiExtension Build(
            XmiExtension extension,
            string packageName,
            HashSet<string> packageIds,
            Dictionary<string, string> connectorPackageMap);
    }
}
