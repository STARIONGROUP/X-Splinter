// ------------------------------------------------------------------------------------------------
// <copyright file="PackageEntry.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    /// <summary>
    /// Associates an element identifier with the package that owns it and the
    /// output file that package is written to.
    /// </summary>
    /// <param name="PackageName">
    /// The name of the owning package.
    /// </param>
    /// <param name="OutputFile">
    /// The output filename for the owning package.
    /// </param>
    public record PackageEntry(string PackageName, string OutputFile);
}
