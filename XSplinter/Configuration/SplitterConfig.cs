// ------------------------------------------------------------------------------------------------
// <copyright file="SplitterConfig.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the top-level configuration for the XMI splitter,
    /// defining the root package and the list of child packages to extract.
    /// </summary>
    public class SplitterConfig
    {
        /// <summary>
        /// Gets or sets the name of the root container package in the XMI
        /// (e.g. "5. Data Structure").
        /// </summary>
        /// <remarks>
        /// Optional. When set, only that package and its descendants are searched for the
        /// configured packages. When left empty, the whole document is searched, which suits
        /// exports whose packages sit directly under the model.
        /// </remarks>
        public string RootPackageName { get; set; } = "";

        /// <summary>
        /// Gets or sets the name of the <c>uml:Model</c> wrapper written around each extracted
        /// package (e.g. "EA_Model").
        /// </summary>
        /// <remarks>
        /// Optional. When left empty the name of the model that encloses the package in the source
        /// document is reused, so the output mirrors the input. The wrapper is not written at all
        /// for packages marked <see cref="PackageConfig.ConvertToLibrary"/>, nor when the source
        /// has no model.
        /// </remarks>
        public string ModelName { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of packages to extract into separate XMI files.
        /// </summary>
        public List<PackageConfig> Packages { get; set; } = [];
    }
}
