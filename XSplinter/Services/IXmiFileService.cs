// ------------------------------------------------------------------------------------------------
// <copyright file="IXmiFileService.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Xml.Linq;

    /// <summary>
    /// Abstracts the file-system interactions used by the splitter (loading the input
    /// XMI, ensuring the output directory exists, and saving output documents), so that
    /// the orchestration logic can be unit tested without touching the disk.
    /// </summary>
    public interface IXmiFileService
    {
        /// <summary>
        /// Loads the XMI document located at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the XMI file to load.
        /// </param>
        /// <returns>
        /// The loaded <see cref="XDocument"/>.
        /// </returns>
        XDocument Load(string path);

        /// <summary>
        /// Ensures the given output directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">
        /// The directory path to create.
        /// </param>
        void EnsureDirectory(string path);

        /// <summary>
        /// Saves the given document to the specified path.
        /// </summary>
        /// <param name="document">
        /// The <see cref="XDocument"/> to save.
        /// </param>
        /// <param name="path">
        /// The destination file path.
        /// </param>
        void Save(XDocument document, string path);
    }
}
