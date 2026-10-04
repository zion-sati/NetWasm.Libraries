// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// NetWasm adaptation: use the pinned English resources as constants so the
// closed-world runtime does not need ResourceManager-based lookup.
namespace System.Formats.Cbor
{
    internal static class SR
    {
        internal const string ArgumentOutOfRange_Generic_MustBeNonNegative = "{0} ('{1}') must be a non-negative value.";
        internal const string Argument_EncodeDestinationTooSmall = "The destination is too small to hold the encoded value.";
        internal const string CborContentException_DefaultMessage = "The CBOR encoding is invalid.";
        internal const string Cbor_ConformanceMode_ContainsDuplicateKeys = "CBOR Conformance mode '{0}' does not support duplicate keys.";
        internal const string Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported = "CBOR Conformance mode '{0}' does not support indefinite-length data items.";
        internal const string Cbor_ConformanceMode_InvalidSimpleValueEncoding = "CBOR Conformance mode '{0}' does not support simple values in the range 24-31 and must be encoded as small as possible.";
        internal const string Cbor_ConformanceMode_KeysNotInSortedOrder = "CBOR keys not sorted in accordance with conformance mode '{0}'.";
        internal const string Cbor_ConformanceMode_NonCanonicalIntegerRepresentation = "CBOR Conformance mode '{0}' does not support non-canonical integer representations.";
        internal const string Cbor_ConformanceMode_RequiresDefiniteLengthItems = "CBOR Conformance mode '{0}' does not support indefinite-length items.";
        internal const string Cbor_ConformanceMode_TagsNotSupported = "CBOR Conformance mode '{0}' does not support tagged values.";
        internal const string Cbor_NotAtEndOfDefiniteLengthDataItem = "Not at end of the definite-length data item.";
        internal const string Cbor_NotAtEndOfIndefiniteLengthDataItem = "Not at end of the indefinite-length data item.";
        internal const string Cbor_PopMajorTypeMismatch = "Cannot perform the requested operation, the current major type context is '{0}'.";
        internal const string Cbor_Reader_DefiniteLengthExceedsBufferSize = "Declared definite length of CBOR data item exceeds available buffer size.";
        internal const string Cbor_Reader_InvalidBigNumEncoding = "Not a valid tagged bignum encoding.";
        internal const string Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem = "Indefinite-length CBOR string nests an invalid data item of major type {0}.";
        internal const string Cbor_Reader_InvalidCbor_InvalidIntegerEncoding = "CBOR initial byte contains invalid integer encoding";
        internal const string Cbor_Reader_InvalidCbor_InvalidUtf8StringEncoding = "CBOR text string payload is not valid a UTF-8 encoding.";
        internal const string Cbor_Reader_InvalidCbor_KeyMissingValue = "The current CBOR map contains an incomplete key/value pair.";
        internal const string Cbor_Reader_InvalidCbor_TagNotFollowedByValue = "A CBOR tag should always be followed by a data item.";
        internal const string Cbor_Reader_InvalidCbor_UnexpectedBreakByte = "CBOR definite-length data items contains unexpected break byte.";
        internal const string Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer = "Unexpected end of CBOR encoding data.";
        internal const string Cbor_Reader_InvalidDateTimeEncoding = "Not a valid tagged RFC3339 DateTime encoding.";
        internal const string Cbor_Reader_InvalidDecimalEncoding = "Not a valid tagged decimal encoding.";
        internal const string Cbor_Reader_InvalidUnixTimeEncoding = "Not a valid tagged unix time encoding.";
        internal const string Cbor_Reader_IsAtRootContext = "CBOR reader is already at the root data item context.";
        internal const string Cbor_Reader_MajorTypeMismatch = "Cannot perform the requested operation, the next CBOR data item is of major type '{0}'.";
        internal const string Cbor_Reader_MaximumDepthExceeded = "The depth of the CBOR data exceeded the maximum allowed depth of {0}.";
        internal const string Cbor_Reader_NoMoreDataItemsToRead = "No more CBOR data items to read in the current context.";
        internal const string Cbor_Reader_NotABooleanEncoding = "CBOR simple value does not encode a boolean value.";
        internal const string Cbor_Reader_NotAFloatEncoding = "Data item does not encode a floating point number.";
        internal const string Cbor_Reader_NotANullEncoding = "CBOR simple value does not encode null.";
        internal const string Cbor_Reader_NotASimpleValueEncoding = "CBOR data item does not encode a simple value.";
        internal const string Cbor_Reader_NotIndefiniteLengthString = "CBOR string is not of indefinite length.";
        internal const string Cbor_Reader_ReadingAsLowerPrecision = "Attempting to read floating point encoding as a lower-precision value.";
        internal const string Cbor_Reader_Skip_InvalidState = "Reader state '{0}' is not at the start of a data item.";
        internal const string Cbor_Reader_TagMismatch = "CBOR tag does not match expected value.";
        internal const string Cbor_Writer_CannotNestDataItemsInIndefiniteLengthStrings = "Cannot nest data items in indefinite-length CBOR string contexts.";
        internal const string Cbor_Writer_DecimalOverflow = "Value was either too large or too small for a Decimal.";
        internal const string Cbor_Writer_DefiniteLengthExceeded = "Adding a CBOR data item to the current context exceeds its definite length.";
        internal const string Cbor_Writer_IncompleteCborDocument = "Writer contains an incomplete CBOR document.";
        internal const string Cbor_Writer_InvalidUtf8String = "Not a valid UTF-8 encoding.";
        internal const string Cbor_Writer_MapIncompleteKeyValuePair = "CBOR map incomplete; each key must be followed by a corresponding value.";
        internal const string Cbor_Writer_MaximumDepthExceeded = "Writing the CBOR data item would exceed the maximum allowed depth of {0}.";
        internal const string Cbor_Writer_PayloadIsNotValidCbor = "Not a valid CBOR value encoding.";
        internal const string Cbor_Writer_ValueCannotBeInfiniteOrNaN = "Value cannot be infinite or NaN.";

        internal static string Format(string format, params object?[] args)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }
}
