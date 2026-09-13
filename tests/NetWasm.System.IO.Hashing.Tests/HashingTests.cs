using System;
using System.IO.Hashing;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.System.IO.Hashing.Tests;

public sealed class HashingTests
{
    [Test]
    public async Task ChecksumsMatchPinnedAnswers()
    {
        await HashingAdaptationAssertions.AssertCrcScalarParameterPathsAsync();

        var data = Convert.FromHexString("48656c6c6f2c204e65745761736d21");

        await Assert.That(Convert.ToHexString(Adler32.Hash(data)).ToLowerInvariant()).IsEqualTo("296c0521");
        await Assert.That(Adler32.HashToUInt32(data)).IsEqualTo(694945057u);
        await Assert.That(Convert.ToHexString(Crc32.Hash(data)).ToLowerInvariant()).IsEqualTo("3cb87cc1");
        await Assert.That(Crc32.HashToUInt32(data)).IsEqualTo(3246176316u);
        await Assert.That(Convert.ToHexString(Crc32.Hash(Crc32ParameterSet.Crc32C, data)).ToLowerInvariant()).IsEqualTo("5411f351");
        await Assert.That(Crc32.HashToUInt32(Crc32ParameterSet.Crc32C, data)).IsEqualTo(1374884180u);
        await Assert.That(Convert.ToHexString(Crc64.Hash(data)).ToLowerInvariant()).IsEqualTo("72ad88c30c37f1df");
        await Assert.That(Crc64.HashToUInt64(data)).IsEqualTo(8263411262599721439ul);
        await Assert.That(Convert.ToHexString(Crc64.Hash(Crc64ParameterSet.Nvme, data)).ToLowerInvariant()).IsEqualTo("f35ca9b629c02d78");
        await Assert.That(Crc64.HashToUInt64(Crc64ParameterSet.Nvme, data)).IsEqualTo(8659788943894076659ul);
    }

    [Test]
    public async Task XxHashFamilyMatchesPinnedAnswers()
    {
        await HashingAdaptationAssertions.AssertLongSeededXxHashScalarPathAsync();

        var data = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");

        await Assert.That(Convert.ToHexString(XxHash32.Hash(data)).ToLowerInvariant()).IsEqualTo("830741c1");
        await Assert.That(XxHash32.HashToUInt32(data)).IsEqualTo(0x830741c1u);
        await Assert.That(Convert.ToHexString(XxHash64.Hash(data)).ToLowerInvariant()).IsEqualTo("cbf59c5116ff32b4");
        await Assert.That(XxHash64.HashToUInt64(data)).IsEqualTo(0xcbf59c5116ff32b4ul);
        await Assert.That(Convert.ToHexString(XxHash3.Hash(data)).ToLowerInvariant()).IsEqualTo("3523581fe96e4c05");
        await Assert.That(XxHash3.HashToUInt64(data)).IsEqualTo(0x3523581fe96e4c05ul);
        await Assert.That(Convert.ToHexString(XxHash128.Hash(data)).ToLowerInvariant()).IsEqualTo("25e7c9b3424ceed2457d9566b6fcd697");
        await Assert.That(XxHash128.HashToUInt128(data).ToString("x32")).IsEqualTo("25e7c9b3424ceed2457d9566b6fcd697");
    }

    [Test]
    public async Task SeededXxHashFormsMatchPinnedAnswers()
    {
        var data = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");

        var xxHash32 = Convert.ToHexString(XxHash32.Hash(data, -1)).ToLowerInvariant();
        var xxHash64 = Convert.ToHexString(XxHash64.Hash(data, -1)).ToLowerInvariant();
        var xxHash3 = Convert.ToHexString(XxHash3.Hash(data, -1)).ToLowerInvariant();
        var xxHash128 = Convert.ToHexString(XxHash128.Hash(data, -1)).ToLowerInvariant();

        await Assert.That(xxHash32).IsEqualTo("f64b0c63");
        await Assert.That(xxHash64).IsEqualTo("35220dfdb7d4d7c9");
        await Assert.That(xxHash3).IsEqualTo("4e9a5972aede556d");
        await Assert.That(xxHash128).IsEqualTo("5d506526ab64b23fc64e94d368bece3b");
    }

    [Test]
    public async Task IncrementalAndResetFormsPreserveState()
    {
        var data = BoundaryData();
        var algorithms = new (NonCryptographicHashAlgorithm Hash, Func<byte[], byte[]> OneShot)[]
        {
            (new Adler32(), Adler32.Hash),
            (new Crc32(), Crc32.Hash),
            (new Crc64(), Crc64.Hash),
            (new XxHash32(), XxHash32.Hash),
            (new XxHash64(), XxHash64.Hash),
            (new XxHash3(), XxHash3.Hash),
            (new XxHash128(), XxHash128.Hash),
        };

        foreach (var algorithm in algorithms)
        {
            for (var offset = 0; offset < data.Length; offset += 13)
            {
                algorithm.Hash.Append(data.AsSpan(offset, Math.Min(13, data.Length - offset)));
            }

            var expected = algorithm.OneShot(data);
            await Assert.That(SameBytes(algorithm.Hash.GetCurrentHash(), expected)).IsTrue();
            await Assert.That(SameBytes(algorithm.Hash.GetHashAndReset(), expected)).IsTrue();
            await Assert.That(SameBytes(algorithm.Hash.GetCurrentHash(), algorithm.OneShot(Array.Empty<byte>()))).IsTrue();
        }
    }

    [Test]
    public async Task CloneDestinationAndTryFormsPreserveContracts()
    {
        await HashingAdaptationAssertions.AssertDestinationValidationAsync();

        var data = BoundaryData();
        var hash = new XxHash64(17);
        hash.Append(data.AsSpan(0, 31));
        var clone = hash.Clone();
        hash.Append(data.AsSpan(31));
        clone.Append(data.AsSpan(31));
        await Assert.That(SameBytes(hash.GetCurrentHash(), clone.GetCurrentHash())).IsTrue();
        hash.Reset();
        await Assert.That(SameBytes(hash.GetCurrentHash(), XxHash64.Hash(Array.Empty<byte>(), 17))).IsTrue();

        await AssertDestinationForms(data, XxHash32.Hash(data, -1), 4, Write32, Try32);
        await AssertDestinationForms(data, XxHash64.Hash(data, -1), 8, Write64, Try64);
        await AssertDestinationForms(data, XxHash3.Hash(data, -1), 8, Write3, Try3);
        await AssertDestinationForms(data, XxHash128.Hash(data, -1), 16, Write128, Try128);
    }

    [Test]
    public async Task CustomCrcParametersPreserveStandardResults()
    {
        var data = Convert.FromHexString("313233343536373839");
        var crc32c = Crc32ParameterSet.Create(0x1edc6f41, 0xffffffff, 0xffffffff, reflectValues: true);
        var crc64 = Crc64ParameterSet.Create(0x42f0e1eba9ea3693, 0, 0, reflectValues: false);

        await Assert.That(SameBytes(Crc32.Hash(crc32c, data), Crc32.Hash(Crc32ParameterSet.Crc32C, data))).IsTrue();
        await Assert.That(SameBytes(Crc64.Hash(crc64, data), Crc64.Hash(Crc64ParameterSet.Crc64, data))).IsTrue();

        var shortDestination = new byte[3];
        await Assert.That(Crc32.TryHash(data, shortDestination, out var bytesWritten)).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    private static async Task AssertDestinationForms(
        byte[] data,
        byte[] expected,
        int hashLength,
        WriteHash write,
        TryHash tryHash)
    {
        var destination = new byte[hashLength + 3];
        await Assert.That(write(data, destination)).IsEqualTo(hashLength);
        await Assert.That(SameBytes(destination, expected, hashLength)).IsTrue();
        await Assert.That(tryHash(data, destination, out var bytesWritten)).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(hashLength);

        var shortDestination = new byte[hashLength - 1];
        await Assert.That(tryHash(data, shortDestination, out bytesWritten)).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    private static int Write32(ReadOnlySpan<byte> source, Span<byte> destination) => XxHash32.Hash(source, destination, -1);

    private static bool Try32(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => XxHash32.TryHash(source, destination, out bytesWritten, -1);

    private static int Write64(ReadOnlySpan<byte> source, Span<byte> destination) => XxHash64.Hash(source, destination, -1);

    private static bool Try64(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => XxHash64.TryHash(source, destination, out bytesWritten, -1);

    private static int Write3(ReadOnlySpan<byte> source, Span<byte> destination) => XxHash3.Hash(source, destination, -1);

    private static bool Try3(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => XxHash3.TryHash(source, destination, out bytesWritten, -1);

    private static int Write128(ReadOnlySpan<byte> source, Span<byte> destination) => XxHash128.Hash(source, destination, -1);

    private static bool Try128(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) => XxHash128.TryHash(source, destination, out bytesWritten, -1);

    private static byte[] BoundaryData()
    {
        var data = new byte[257];
        data[0] = 0;
        data[1] = 0xff;
        data[2] = 0x7f;
        data[3] = 0x80;
        data[4] = 0xaa;
        data[5] = 0x55;
        data[6] = 0xde;
        data[7] = 0xad;
        data[8] = 0xbe;
        data[9] = 0xef;
        for (var index = 10; index < data.Length; index++)
        {
            data[index] = (byte)(index - 9);
        }

        return data;
    }

    private static bool SameBytes(byte[] actual, byte[] expected, int length = -1)
    {
        var count = length < 0 ? expected.Length : length;
        if (actual.Length < count || expected.Length < count)
        {
            return false;
        }

        for (var index = 0; index < count; index++)
        {
            if (actual[index] != expected[index])
            {
                return false;
            }
        }

        return true;
    }

    private delegate int WriteHash(ReadOnlySpan<byte> source, Span<byte> destination);

    private delegate bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten);
}
