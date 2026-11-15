// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).
// SPDX-License-Identifier: MIT

namespace NetWasm.Microsoft.Extensions.DependencyInjection.Generator;

internal interface IActivationDescriptorLocator
{
    ActivationModel? Locate(IReadOnlyList<ActivationModel> activations, string identity);
}
