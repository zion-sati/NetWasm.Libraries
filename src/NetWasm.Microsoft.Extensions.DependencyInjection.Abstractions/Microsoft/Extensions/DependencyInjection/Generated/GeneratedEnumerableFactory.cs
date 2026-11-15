// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Generated;

/// <summary>Materializes one generated typed sequence from already-resolved values.</summary>
public delegate object? GeneratedEnumerableFactory(IReadOnlyList<object?> values);
