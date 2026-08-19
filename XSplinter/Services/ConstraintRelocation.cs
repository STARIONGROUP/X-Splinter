// ------------------------------------------------------------------------------------------------
// <copyright file="ConstraintRelocation.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using uml4net.CommonStructure;
    using uml4net.Packages;

    /// <summary>
    /// Records that a constraint has been moved from the root container package into the
    /// package that is written to the same document as the element it constrains.
    /// </summary>
    /// <param name="Constraint">
    /// The relocated <see cref="IConstraint"/>.
    /// </param>
    /// <param name="TargetPackage">
    /// The <see cref="IPackage"/> the constraint has been moved into.
    /// </param>
    public record ConstraintRelocation(IConstraint Constraint, IPackage TargetPackage);
}
