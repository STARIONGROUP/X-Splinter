// ------------------------------------------------------------------------------------------------
// <copyright file="IXmiSplitterService.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using XSplinter.Configuration;

    /// <summary>
    /// Defines the contract for the orchestrator that splits a monolithic Enterprise
    /// Architect XMI export into separate XMI files per package.
    /// </summary>
    public interface IXmiSplitterService
    {
        /// <summary>
        /// Splits the monolithic XMI file at <paramref name="inputPath"/> into
        /// separate files according to the provided <paramref name="config"/>,
        /// writing the results to the <paramref name="outputDirectory"/>.
        /// </summary>
        /// <param name="inputPath">
        /// The path to the monolithic XMI file exported from Enterprise Architect.
        /// </param>
        /// <param name="config">
        /// The splitter configuration defining the root package and child packages.
        /// </param>
        /// <param name="outputDirectory">
        /// The directory in which to write the split XMI files.
        /// </param>
        void Split(string inputPath, SplitterConfig config, string outputDirectory);
    }
}
