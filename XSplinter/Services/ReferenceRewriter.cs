// ------------------------------------------------------------------------------------------------
// <copyright file="ReferenceRewriter.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Rewrites cross-package <c>xmi:idref</c> references into <c>href</c> references
    /// that point to the external XMI file containing the target element.
    /// This enables UML4NET to resolve types across separate XMI files.
    /// </summary>
    public class ReferenceRewriter : IReferenceRewriter
    {
        /// <summary>
        /// The XMI namespace URI.
        /// </summary>
        private readonly XNamespace xmi;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceRewriter"/> class.
        /// </summary>
        /// <param name="xmiNamespace">
        /// The XMI namespace used in the source document.
        /// </param>
        public ReferenceRewriter(XNamespace xmiNamespace)
        {
            this.xmi = xmiNamespace;
        }

        /// <inheritdoc />
        public void Rewrite(XElement element, string currentPackageName, IReadOnlyDictionary<string, PackageEntry> elementIndex)
        {
            switch (element.Name.LocalName)
            {
                case "type":
                case "constrainedElement":
                    this.RewriteIdref(element, currentPackageName, elementIndex);
                    break;
            }

            foreach (var child in element.Elements().ToList())
            {
                this.Rewrite(child, currentPackageName, elementIndex);
            }
        }

        /// <summary>
        /// Replaces an <c>xmi:idref</c> attribute with an <c>href</c> attribute
        /// when the referenced element belongs to a different package.
        /// </summary>
        /// <param name="element">
        /// The element whose <c>xmi:idref</c> attribute may be rewritten.
        /// </param>
        /// <param name="currentPackageName">
        /// The name of the current package.
        /// </param>
        /// <param name="elementIndex">
        /// The element index mapping each <c>xmi:id</c> to its owning <see cref="PackageEntry"/>.
        /// </param>
        private void RewriteIdref(XElement element, string currentPackageName, IReadOnlyDictionary<string, PackageEntry> elementIndex)
        {
            var idref = (string?)element.Attribute(this.xmi + "idref");

            if (idref != null
                && elementIndex.TryGetValue(idref, out var entry)
                && entry.PackageName != currentPackageName)
            {
                element.Attribute(this.xmi + "idref")!.Remove();
                element.SetAttributeValue("href", $"{entry.OutputFile}#{idref}");
            }
        }
    }
}
