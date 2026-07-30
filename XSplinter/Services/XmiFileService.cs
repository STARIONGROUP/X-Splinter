// ------------------------------------------------------------------------------------------------
// <copyright file="XmiFileService.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.IO;
    using System.Text;
    using System.Xml.Linq;

    /// <summary>
    /// Default <see cref="IXmiFileService"/> implementation backed by the local file system.
    /// Registers the <see cref="CodePagesEncodingProvider"/> so that the <c>windows-1252</c>
    /// XML declaration used by the output documents can be honoured.
    /// </summary>
    public class XmiFileService : IXmiFileService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmiFileService"/> class.
        /// </summary>
        public XmiFileService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <inheritdoc />
        public XDocument Load(string path)
        {
            return XDocument.Load(path);
        }

        /// <inheritdoc />
        public void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <inheritdoc />
        public void Save(XDocument document, string path)
        {
            document.Save(path);
        }
    }
}
