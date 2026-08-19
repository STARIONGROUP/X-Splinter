// ------------------------------------------------------------------------------------------------
// <copyright file="NonDisposingLoggerFactory.cs" company="Starion Group S.A.">
//   Copyright (c) 2026 Starion Group S.A.
//
//   SPDX-License-Identifier: Apache-2.0
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace XSplinter.Services
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// An <see cref="ILoggerFactory"/> decorator that forwards all calls to the decorated
    /// factory but ignores <see cref="System.IDisposable.Dispose"/>.
    /// </summary>
    /// <remarks>
    /// The UML4NET scopes register the supplied <see cref="ILoggerFactory"/> in their container
    /// without marking it externally owned, so disposing a scope also disposes the caller's
    /// factory. The shared factory is therefore handed to UML4NET wrapped in this decorator.
    /// </remarks>
    public sealed class NonDisposingLoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// The decorated <see cref="ILoggerFactory"/>.
        /// </summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonDisposingLoggerFactory"/> class.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> to decorate.
        /// </param>
        public NonDisposingLoggerFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return this.loggerFactory.CreateLogger(categoryName);
        }

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider)
        {
            this.loggerFactory.AddProvider(provider);
        }

        /// <summary>
        /// Deliberately does nothing; the decorated factory is owned by the caller.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
