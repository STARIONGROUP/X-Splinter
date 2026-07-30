// ------------------------------------------------------------------------------------------------
// <copyright file="SplitterConfig.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
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
        public string RootPackageName { get; set; } = "";

        /// <summary>
        /// Gets or sets the list of packages to extract into separate XMI files.
        /// </summary>
        public List<PackageConfig> Packages { get; set; } = [];
    }
}
