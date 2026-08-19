// ------------------------------------------------------------------------------------------------
// <copyright file="IXmiModelLoader.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    /// <summary>
    /// Loads a monolithic XMI export into the UML4NET object model.
    /// </summary>
    public interface IXmiModelLoader
    {
        /// <summary>
        /// Reads the XMI document located at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the XMI file to read.
        /// </param>
        /// <returns>
        /// The <see cref="LoadedModel"/> describing the read document.
        /// </returns>
        LoadedModel Load(string path);
    }
}
