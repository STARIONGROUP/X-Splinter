// ------------------------------------------------------------------------------------------------
// <copyright file="PackageDocumentAssigner.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    using uml4net;
    using uml4net.Classification;
    using uml4net.Decorators;

    /// <summary>
    /// Default <see cref="IPackageDocumentAssigner"/> implementation that walks the containment
    /// tree of a package and stamps every contained element with the target document name.
    /// </summary>
    /// <remarks>
    /// Containment comes from the UML4NET <see cref="PropertyAttribute"/> metadata: only
    /// <see cref="AggregationKind.Composite"/> properties are traversed. Elements reached through
    /// any other property are merely referenced - their ids are recorded but they are neither
    /// stamped nor traversed, so elements in another package keep pointing at their own document.
    /// </remarks>
    public class PackageDocumentAssigner : IPackageDocumentAssigner
    {
        /// <inheritdoc />
        public PackageElementIds Assign(IXmiElement element, string documentName)
        {
            var result = new PackageElementIds([], []);

            Walk(element, documentName, result, []);

            return result;
        }

        /// <summary>
        /// Recursively stamps the document name on the element and the elements it contains.
        /// </summary>
        /// <param name="element">
        /// The element to stamp.
        /// </param>
        /// <param name="documentName">
        /// The name of the document the element is written to.
        /// </param>
        /// <param name="ids">
        /// The <see cref="PackageElementIds"/> collecting the encountered identifiers.
        /// </param>
        /// <param name="visited">
        /// The set of already visited elements, guarding against cycles.
        /// </param>
        private static void Walk(IXmiElement element, string documentName, PackageElementIds ids, HashSet<IXmiElement> visited)
        {
            if (element == null || !visited.Add(element))
            {
                return;
            }

            element.DocumentName = documentName;

            if (!string.IsNullOrEmpty(element.XmiId))
            {
                ids.ContainedIds.Add(element.XmiId);
            }

            foreach (var property in element.GetType().GetProperties())
            {
                var metadata = property.GetCustomAttribute<PropertyAttribute>();

                if (metadata == null || metadata.IsDerived || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                var isContainment = metadata.Aggregation == AggregationKind.Composite;

                foreach (var child in QueryElements(property, element))
                {
                    if (isContainment)
                    {
                        Walk(child, documentName, ids, visited);
                    }
                    else if (!string.IsNullOrEmpty(child.XmiId))
                    {
                        ids.ReferencedIds.Add(child.XmiId);
                    }
                }
            }
        }

        /// <summary>
        /// Queries the <see cref="IXmiElement"/> values held by the given property, whether the
        /// property is single valued or a collection.
        /// </summary>
        /// <param name="property">
        /// The property to read.
        /// </param>
        /// <param name="element">
        /// The element the property is read from.
        /// </param>
        /// <returns>
        /// The <see cref="IXmiElement"/> values held by the property.
        /// </returns>
        private static IEnumerable<IXmiElement> QueryElements(PropertyInfo property, IXmiElement element)
        {
            object value;

            try
            {
                value = property.GetValue(element);
            }
            catch (System.Exception)
            {
                // a handful of UML4NET properties throw when the elements they resolve are not
                // present in the document that is being read; such properties are skipped
                yield break;
            }

            switch (value)
            {
                case IXmiElement single:
                    yield return single;
                    break;
                case IEnumerable values:
                    foreach (var item in values)
                    {
                        if (item is IXmiElement child)
                        {
                            yield return child;
                        }
                    }

                    break;
            }
        }
    }
}
