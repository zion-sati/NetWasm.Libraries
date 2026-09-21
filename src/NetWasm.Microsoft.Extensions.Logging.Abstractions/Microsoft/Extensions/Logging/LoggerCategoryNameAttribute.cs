// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Marks the generated-activation parameter that receives the closed logger
    /// category name. NetWasm's DI generator replaces it with a compile-time
    /// string instead of requiring runtime type metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class LoggerCategoryNameAttribute : Attribute
    {
    }
}
