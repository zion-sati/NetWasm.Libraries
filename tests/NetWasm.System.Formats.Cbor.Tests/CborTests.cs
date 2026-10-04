using System;
using System.Formats.Cbor;
using System.Numerics;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace NetWasm.System.Formats.Cbor.Tests;

public sealed class CborTests
{
    [Test]
    public async Task IntegersStringsBytesArraysAndMapsMatchExpectedEncoding()
    {
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteInt32(42);
        await AssertBytes(writer.Encode(), 0x18, 0x2a);

        writer.Reset();
        writer.WriteInt32(-42);
        await AssertBytes(writer.Encode(), 0x38, 0x29);

        writer.Reset();
        writer.WriteTextString("hi");
        await AssertBytes(writer.Encode(), 0x62, 0x68, 0x69);

        writer.Reset();
        writer.WriteByteString(new byte[] { 1, 2, 3 });
        await AssertBytes(writer.Encode(), 0x43, 0x01, 0x02, 0x03);

        writer.Reset();
        writer.WriteStartArray(2);
        writer.WriteInt32(42);
        writer.WriteTextString("hi");
        writer.WriteEndArray();
        var arrayEncoding = writer.Encode();
        await AssertBytes(arrayEncoding, 0x82, 0x18, 0x2a, 0x62, 0x68, 0x69);

        var arrayReader = new CborReader(arrayEncoding, CborConformanceMode.Lax);
        await Assert.That(arrayReader.ReadStartArray()).IsEqualTo((int?)2);
        await Assert.That(arrayReader.ReadInt32()).IsEqualTo(42);
        await Assert.That(arrayReader.ReadTextString()).IsEqualTo("hi");
        arrayReader.ReadEndArray();
        await Assert.That(arrayReader.PeekState()).IsEqualTo(CborReaderState.Finished);

        writer.Reset();
        writer.WriteStartMap(1);
        writer.WriteTextString("x");
        writer.WriteInt32(42);
        writer.WriteEndMap();
        var mapEncoding = writer.Encode();
        await AssertBytes(mapEncoding, 0xa1, 0x61, 0x78, 0x18, 0x2a);

        var mapReader = new CborReader(mapEncoding, CborConformanceMode.Lax);
        await Assert.That(mapReader.ReadStartMap()).IsEqualTo((int?)1);
        await Assert.That(mapReader.ReadTextString()).IsEqualTo("x");
        await Assert.That(mapReader.ReadInt32()).IsEqualTo(42);
        mapReader.ReadEndMap();
    }

    [Test]
    public async Task IntegerBoundariesAndFloatingPointInfinitiesRoundTrip()
    {
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteUInt64(ulong.MaxValue);
        await AssertBytes(writer.Encode(),
            0x1b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff);

        writer.Reset();
        writer.WriteInt64(long.MinValue);
        await AssertBytes(writer.Encode(),
            0x3b, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff);

        writer.Reset();
        writer.WriteStartArray(3);
        writer.WriteHalf(Half.PositiveInfinity);
        writer.WriteSingle(float.PositiveInfinity);
        writer.WriteDouble(double.NegativeInfinity);
        writer.WriteEndArray();

        var reader = new CborReader(writer.Encode(), CborConformanceMode.Lax);
        await Assert.That(reader.ReadStartArray()).IsEqualTo((int?)3);
        await Assert.That(Half.IsPositiveInfinity(reader.ReadHalf())).IsTrue();
        await Assert.That(float.IsPositiveInfinity(reader.ReadSingle())).IsTrue();
        await Assert.That(double.IsNegativeInfinity(reader.ReadDouble())).IsTrue();
        reader.ReadEndArray();
    }

    [Test]
    public async Task IndefiniteCollectionsAndSimpleValuesRoundTrip()
    {
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartArray(3);
        writer.WriteStartArray(null);
        writer.WriteInt32(1);
        writer.WriteTextString("x");
        writer.WriteEndArray();
        writer.WriteStartMap(null);
        writer.WriteTextString("a");
        writer.WriteInt32(2);
        writer.WriteEndMap();
        writer.WriteSimpleValue(CborSimpleValue.Undefined);
        writer.WriteEndArray();

        var encoded = writer.Encode();
        await AssertBytes(encoded,
            0x83, 0x9f, 0x01, 0x61, 0x78, 0xff, 0xbf, 0x61, 0x61, 0x02, 0xff, 0xf7);

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        await Assert.That(reader.ReadStartArray()).IsEqualTo((int?)3);
        await Assert.That(reader.ReadStartArray()).IsNull();
        await Assert.That(reader.ReadInt32()).IsEqualTo(1);
        await Assert.That(reader.ReadTextString()).IsEqualTo("x");
        reader.ReadEndArray();
        await Assert.That(reader.ReadStartMap()).IsNull();
        await Assert.That(reader.ReadTextString()).IsEqualTo("a");
        await Assert.That(reader.ReadInt32()).IsEqualTo(2);
        reader.ReadEndMap();
        await Assert.That(reader.ReadSimpleValue()).IsEqualTo(CborSimpleValue.Undefined);
        reader.ReadEndArray();
        await Assert.That(reader.PeekState()).IsEqualTo(CborReaderState.Finished);
    }

    [Test]
    public async Task FloatingPointBigIntegerDecimalAndUnixTagsRoundTrip()
    {
        var positiveBigInteger = BigInteger.One << 80;
        var negativeBigInteger = -(BigInteger.One << 90);
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartArray(8);
        writer.WriteHalf((Half)1.5f);
        writer.WriteSingle(2.25f);
        writer.WriteDouble(-4.5d);
        writer.WriteHalf(Half.NaN);
        writer.WriteBigInteger(positiveBigInteger);
        writer.WriteBigInteger(negativeBigInteger);
        writer.WriteDecimal(12345.6789m);
        writer.WriteUnixTimeSeconds(42L);
        writer.WriteEndArray();

        var reader = new CborReader(writer.Encode(), CborConformanceMode.Lax);
        await Assert.That(reader.ReadStartArray()).IsEqualTo((int?)8);
        await Assert.That((float)reader.ReadHalf()).IsEqualTo(1.5f);
        await Assert.That(reader.ReadSingle()).IsEqualTo(2.25f);
        await Assert.That(reader.ReadDouble()).IsEqualTo(-4.5d);
        await Assert.That(float.IsNaN((float)reader.ReadHalf())).IsTrue();
        await Assert.That(reader.ReadBigInteger()).IsEqualTo(positiveBigInteger);
        await Assert.That(reader.ReadBigInteger()).IsEqualTo(negativeBigInteger);
        await Assert.That(reader.ReadDecimal()).IsEqualTo(12345.6789m);
        await Assert.That(reader.ReadUnixTimeSeconds().ToUnixTimeSeconds()).IsEqualTo(42L);
        reader.ReadEndArray();
    }

    [Test]
    public async Task IndefiniteStringsAndTryReadsPreserveDestinationAndReaderState()
    {
        var writer = new CborWriter(CborConformanceMode.Lax, allowMultipleRootLevelValues: true);
        writer.WriteStartIndefiniteLengthTextString();
        writer.WriteTextString("Net");
        writer.WriteTextString("Wasm");
        writer.WriteEndIndefiniteLengthTextString();
        writer.WriteStartIndefiniteLengthByteString();
        writer.WriteByteString(new byte[] { 1, 2 });
        writer.WriteByteString(new byte[] { 3 });
        writer.WriteEndIndefiniteLengthByteString();

        var reader = new CborReader(writer.Encode(), CborConformanceMode.Lax, allowMultipleRootLevelValues: true);
        reader.ReadStartIndefiniteLengthTextString();
        await Assert.That(reader.ReadTextString()).IsEqualTo("Net");
        await Assert.That(reader.ReadTextString()).IsEqualTo("Wasm");
        reader.ReadEndIndefiniteLengthTextString();
        reader.ReadStartIndefiniteLengthByteString();
        await AssertBytes(reader.ReadByteString(), 0x01, 0x02);
        await AssertBytes(reader.ReadByteString(), 0x03);
        reader.ReadEndIndefiniteLengthByteString();

        var byteReader = new CborReader(new byte[] { 0x43, 0x01, 0x02, 0x03 }, CborConformanceMode.Lax);
        var shortByteResult = TryReadByteString(byteReader, 2);
        await Assert.That(shortByteResult.Success).IsFalse();
        await Assert.That(shortByteResult.BytesWritten).IsEqualTo(0);
        await Assert.That(shortByteResult.BytesRemaining).IsEqualTo(4);
        await Assert.That(shortByteResult.Hex).IsEqualTo("0000");
        var byteResult = TryReadByteString(byteReader, 3);
        await Assert.That(byteResult.Success).IsTrue();
        await Assert.That(byteResult.BytesWritten).IsEqualTo(3);
        await Assert.That(byteResult.BytesRemaining).IsEqualTo(0);
        await Assert.That(byteResult.Hex).IsEqualTo("010203");

        var textReader = new CborReader(new byte[] { 0x62, 0x68, 0x69 }, CborConformanceMode.Lax);
        var shortTextResult = TryReadTextString(textReader, 1);
        await Assert.That(shortTextResult.Success).IsFalse();
        await Assert.That(shortTextResult.CharsWritten).IsEqualTo(0);
        await Assert.That(shortTextResult.BytesRemaining).IsEqualTo(3);
        var textResult = TryReadTextString(textReader, 2);
        await Assert.That(textResult.Success).IsTrue();
        await Assert.That(textResult.CharsWritten).IsEqualTo(2);
        await Assert.That(textResult.Text).IsEqualTo("hi");
    }

    [Test]
    public async Task ConformanceModesEnforceCanonicalAndStrictRules()
    {
        var nonCanonicalInteger = new CborReader(new byte[] { 0x18, 0x01 }, CborConformanceMode.Canonical);
        await Assert.That(ThrowsCborContentException(() => nonCanonicalInteger.ReadUInt32())).IsTrue();

        var invalidUtf8 = new CborReader(new byte[] { 0x61, 0xff }, CborConformanceMode.Strict);
        await Assert.That(ThrowsCborContentException(() => invalidUtf8.ReadTextString())).IsTrue();

        var duplicateKeys = new CborReader(
            new byte[] { 0xa2, 0x61, 0x61, 0x01, 0x61, 0x61, 0x02 },
            CborConformanceMode.Strict);
        duplicateKeys.ReadStartMap();
        await Assert.That(duplicateKeys.ReadTextString()).IsEqualTo("a");
        await Assert.That(duplicateKeys.ReadInt32()).IsEqualTo(1);
        await Assert.That(ThrowsCborContentException(() => duplicateKeys.ReadTextString())).IsTrue();

        var unorderedWriter = new CborWriter(CborConformanceMode.Canonical);
        unorderedWriter.WriteStartMap(2);
        unorderedWriter.WriteTextString("b");
        unorderedWriter.WriteInt32(1);
        unorderedWriter.WriteTextString("a");
        unorderedWriter.WriteInt32(2);
        unorderedWriter.WriteEndMap();
        await AssertBytes(unorderedWriter.Encode(), 0xa2, 0x61, 0x61, 0x02, 0x61, 0x62, 0x01);

        var unorderedReader = new CborReader(
            new byte[] { 0xa2, 0x61, 0x62, 0x01, 0x61, 0x61, 0x02 },
            CborConformanceMode.Canonical);
        unorderedReader.ReadStartMap();
        await Assert.That(unorderedReader.ReadTextString()).IsEqualTo("b");
        await Assert.That(unorderedReader.ReadInt32()).IsEqualTo(1);
        await Assert.That(ThrowsCborContentException(() => unorderedReader.ReadTextString())).IsTrue();

        var ctapWriter = new CborWriter(CborConformanceMode.Ctap2Canonical);
        await Assert.That(ThrowsInvalidOperationException(() => ctapWriter.WriteTag(CborTag.Uri))).IsTrue();
    }

    [Test]
    public async Task ReaderStateResetAndMultipleRootsWork()
    {
        var reader = new CborReader(
            new byte[] { 0x83, 0x01, 0xa1, 0x61, 0x78, 0x02, 0x62, 0x68, 0x69 },
            CborConformanceMode.Lax);
        await Assert.That(reader.PeekState()).IsEqualTo(CborReaderState.StartArray);
        reader.ReadStartArray();
        await Assert.That(reader.ReadUInt32()).IsEqualTo(1u);
        reader.ReadStartMap();
        await Assert.That(reader.ReadTextString()).IsEqualTo("x");
        await Assert.That(reader.ReadInt32()).IsEqualTo(2);
        reader.ReadEndMap();
        await Assert.That(reader.ReadTextString()).IsEqualTo("hi");
        reader.ReadEndArray();
        await Assert.That(reader.PeekState()).IsEqualTo(CborReaderState.Finished);

        reader.Reset(new byte[] { 0x18, 0x2a });
        await Assert.That(reader.ReadInt32()).IsEqualTo(42);

        var options = new CborReaderOptions { AllowMultipleRootLevelValues = true };
        var multipleRoots = new CborReader(new byte[] { 0x01, 0x02 }, options);
        await Assert.That(multipleRoots.ReadUInt32()).IsEqualTo(1u);
        await Assert.That(multipleRoots.ReadUInt32()).IsEqualTo(2u);
        await Assert.That(multipleRoots.PeekState()).IsEqualTo(CborReaderState.Finished);
    }

    [Test]
    public async Task DateTimeTagsSkipValueAndEncodedValuesRoundTrip()
    {
        var utc = new DateTimeOffset(2026, 10, 4, 12, 34, 56, 123, TimeSpan.Zero).AddTicks(4567);
        var offset = new DateTimeOffset(2026, 10, 4, 22, 34, 56, TimeSpan.FromHours(10));
        var writer = new CborWriter(CborConformanceMode.Lax);
        writer.WriteStartArray(2);
        writer.WriteDateTimeOffset(utc);
        writer.WriteDateTimeOffset(offset);
        writer.WriteEndArray();

        var dateReader = new CborReader(writer.Encode(), CborConformanceMode.Lax);
        await Assert.That(dateReader.ReadStartArray()).IsEqualTo((int?)2);
        await Assert.That(dateReader.ReadDateTimeOffset()).IsEqualTo(utc);
        var decodedOffset = dateReader.ReadDateTimeOffset();
        await Assert.That(decodedOffset).IsEqualTo(offset);
        await Assert.That(decodedOffset.Offset).IsEqualTo(offset.Offset);
        dateReader.ReadEndArray();

        var nestedValue = new byte[] { 0xa1, 0x64, 0x73, 0x6b, 0x69, 0x70, 0x82, 0x02, 0x03 };
        var encoded = new byte[]
        {
            0x83, 0x01,
            0xa1, 0x64, 0x73, 0x6b, 0x69, 0x70, 0x82, 0x02, 0x03,
            0x64, 0x74, 0x61, 0x69, 0x6c,
        };
        var skipReader = new CborReader(encoded, CborConformanceMode.Lax);
        await Assert.That(skipReader.ReadStartArray()).IsEqualTo((int?)3);
        await Assert.That(skipReader.ReadInt32()).IsEqualTo(1);
        skipReader.SkipValue();
        await Assert.That(skipReader.ReadTextString()).IsEqualTo("tail");
        skipReader.ReadEndArray();
        await Assert.That(skipReader.PeekState()).IsEqualTo(CborReaderState.Finished);

        var encodedValueReader = new CborReader(nestedValue, CborConformanceMode.Lax);
        await AssertBytes(encodedValueReader.ReadEncodedValue().ToArray(), nestedValue);
        await Assert.That(encodedValueReader.PeekState()).IsEqualTo(CborReaderState.Finished);
    }

    [Test]
    public async Task MalformedInputAndDepthLimitsUseCborContentErrors()
    {
        var truncatedInteger = new CborReader(new byte[] { 0x18 }, CborConformanceMode.Lax);
        await Assert.That(ThrowsCborContentException(() => truncatedInteger.ReadUInt32())).IsTrue();

        var unexpectedBreak = new CborReader(new byte[] { 0xff }, CborConformanceMode.Lax);
        await Assert.That(ThrowsCborContentException(() => unexpectedBreak.PeekState())).IsTrue();

        var options = new CborReaderOptions { MaxDepth = 1 };
        var nested = new CborReader(new byte[] { 0x81, 0x81, 0x00 }, options);
        nested.ReadStartArray();
        await Assert.That(ThrowsCborContentException(() => nested.ReadStartArray())).IsTrue();
    }

    private static async Task AssertBytes(byte[] actual, params byte[] expected)
        => await Assert.That(BytesEqual(actual, expected)).IsTrue();

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool ThrowsCborContentException(Action action)
    {
        try
        {
            action();
        }
        catch (CborContentException)
        {
            return true;
        }

        return false;
    }

    private static bool ThrowsInvalidOperationException(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return false;
    }

    private static (bool Success, int BytesWritten, int BytesRemaining, string Hex) TryReadByteString(
        CborReader reader,
        int destinationLength)
    {
        var destination = new byte[destinationLength];
        var success = reader.TryReadByteString(destination.AsSpan(), out var bytesWritten);
        return (success, bytesWritten, reader.BytesRemaining, Convert.ToHexString(destination));
    }

    private static (bool Success, int CharsWritten, int BytesRemaining, string Text) TryReadTextString(
        CborReader reader,
        int destinationLength)
    {
        var destination = new char[destinationLength];
        var success = reader.TryReadTextString(destination.AsSpan(), out var charsWritten);
        return (success, charsWritten, reader.BytesRemaining, new string(destination));
    }
}
