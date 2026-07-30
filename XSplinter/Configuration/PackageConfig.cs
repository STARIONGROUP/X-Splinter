// ------------------------------------------------------------------------------------------------
// <copyright file="PackageConfig.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Configuration
{
    /// <summary>
    /// Represents the configuration for a single UML package to be extracted
    /// from the monolithic XMI file into its own output file.
    /// </summary>
    public class PackageConfig
    {
        /// <summary>
        /// Gets or sets the name of the UML package as it appears in the XMI
        /// (e.g. "Primitives", "Forge", "FunctionalData").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the filename for the output XMI file
        /// (e.g. "CSharp_Primitives.xmi").
        /// </summary>
        public string OutputFile { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether this package should be converted into a
        /// simple reusable library. When <c>false</c> (the default), the package is written
        /// as a full Enterprise Architect model (<c>uml:Model name="EA_Model"</c> wrapper and
        /// <c>xmi:Extension</c> section). When set to <c>true</c>, the package is output as a
        /// standard <c>uml:Package</c> element without the model wrapper or EA metadata,
        /// suitable for use as a reusable library.
        /// </summary>
        public bool ConvertToLibrary { get; set; } = false;
    }
}
