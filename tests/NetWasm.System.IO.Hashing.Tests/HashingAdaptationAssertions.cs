using System;
using System.IO.Hashing;
using System.Threading.Tasks;
using TUnit.Assertions;

namespace NetWasm.System.IO.Hashing.Tests;

internal static class HashingAdaptationAssertions
{
    public static async Task AssertCrcScalarParameterPathsAsync()
    {
        byte[] source = "123456789"u8.ToArray();
        var customCrc32 = Crc32ParameterSet.Create(0x04c11db7u, uint.MaxValue, uint.MaxValue, true);
        var standardCrc32 = Crc32.HashToUInt32(Crc32ParameterSet.Crc32, source);
        var customCrc32Value = Crc32.HashToUInt32(customCrc32, source);
        var crc32C = Crc32.HashToUInt32(Crc32ParameterSet.Crc32C, source);
        var customCrc64 = Crc64ParameterSet.Create(0x42f0e1eba9ea3693ul, 0ul, 0ul, false);
        var standardCrc64 = Crc64.HashToUInt64(Crc64ParameterSet.Crc64, source);
        var customCrc64Value = Crc64.HashToUInt64(customCrc64, source);
        var nvme = Crc64.HashToUInt64(Crc64ParameterSet.Nvme, source);

        await Assert.That(standardCrc32).IsEqualTo(0xcbf43926u);
        await Assert.That(customCrc32Value).IsEqualTo(standardCrc32);
        await Assert.That(crc32C).IsEqualTo(0xe3069283u);
        await Assert.That(standardCrc64).IsEqualTo(0x6c40df5f0b497347ul);
        await Assert.That(customCrc64Value).IsEqualTo(standardCrc64);
        await Assert.That(nvme).IsEqualTo(0xae8b14860a799888ul);
    }

    public static async Task AssertLongSeededXxHashScalarPathAsync()
    {
        var source = new byte[2048];
        const long seed = 0x1020304050607080;

        for (var index = 0; index < source.Length; index++)
        {
            source[index] = unchecked((byte)((index * 37) + 11));
        }

        var staticXxHash3 = XxHash3.HashToUInt64(source, seed);
        var staticXxHash128 = XxHash128.HashToUInt128(source, seed);
        var incrementalXxHash3 = new XxHash3(seed);
        var incrementalXxHash128 = new XxHash128(seed);

        incrementalXxHash3.Append(source.AsSpan(0, 513));
        incrementalXxHash3.Append(source.AsSpan(513));
        incrementalXxHash128.Append(source.AsSpan(0, 1027));
        incrementalXxHash128.Append(source.AsSpan(1027));

        await Assert.That(incrementalXxHash3.GetCurrentHashAsUInt64()).IsEqualTo(staticXxHash3);
        await Assert.That(incrementalXxHash128.GetCurrentHashAsUInt128()).IsEqualTo(staticXxHash128);
    }

    public static async Task AssertDestinationValidationAsync()
    {
        byte[] source = "hashing"u8.ToArray();
        var destination = new byte[15];
        var threw = false;

        try
        {
            XxHash128.Hash(source, destination, 0);
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }
}
