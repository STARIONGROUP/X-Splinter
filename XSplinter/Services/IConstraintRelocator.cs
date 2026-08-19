// ------------------------------------------------------------------------------------------------
// <copyright file="IConstraintRelocator.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using System.Collections.Generic;

    using uml4net.Packages;

    /// <summary>
    /// Moves the constraints owned by the root container package into the extracted package
    /// that owns the element they constrain.
    /// </summary>
    /// <remarks>
    /// The root container package is split around and never written, so a constraint owned by it
    /// would otherwise be lost. Since a constraint applies to specific elements, it is relocated
    /// to the package written to the same document as its constrained element.
    /// </remarks>
    public interface IConstraintRelocator
    {
        /// <summary>
        /// Relocates the constraints of <paramref name="rootPackage"/> whose constrained elements
        /// all live in one of the <paramref name="targetPackages"/>.
        /// </summary>
        /// <param name="rootPackage">
        /// The root container package holding the constraints.
        /// </param>
        /// <param name="targetPackages">
        /// The extracted packages, already stamped with their target document name.
        /// </param>
        /// <returns>
        /// The relocations that were performed. Constraints whose constrained elements cannot be
        /// resolved to exactly one target package are left untouched.
        /// </returns>
        IReadOnlyList<ConstraintRelocation> Relocate(IPackage rootPackage, IEnumerable<IPackage> targetPackages);
    }
}
