// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Common.Tests;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public partial class BinaryMessageFormatTests
    {
        [Fact]
        public async Task WriteMultipleMessages()
        {
            var expectedEncoding = new byte[]
            {
                /* length: */ 0x00,
                    /* body: <empty> */
                /* length: */ 0x0E,
                    /* body: */ 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x0D, 0x0A, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,
            };

            var messages = new[]
            {
                new byte[0],
                Encoding.UTF8.GetBytes("Hello,\r\nWorld!")
            };

            var pipe = new Pipe();
            foreach (var message in messages)
            {
                BinaryMessageFormat.WriteLengthPrefix(message.Length, pipe.Writer);
                pipe.Writer.Write(message);
            }
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            Assert.Equal(expectedEncoding, await pipe.Reader.ReadAllAsync());
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, new byte[0])]
        [InlineData(new byte[] { 0x04, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(new byte[]
            {
                0x80, 0x01, // Size - 128
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f,
                0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f
            },
            new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f,
                0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f
            })]
        public async Task WriteBinaryMessage(byte[] encoded, byte[] payload)
        {
            var pipe = new Pipe();
            BinaryMessageFormat.WriteLengthPrefix(payload.Length, pipe.Writer);
            pipe.Writer.Write(payload);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            Assert.Equal(encoded, await pipe.Reader.ReadAllAsync());
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, "")]
        [InlineData(new byte[] { 0x03, 0x41, 0x42, 0x43 }, "ABC")]
        [InlineData(new byte[] { 0x0B, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, "A\nR\rC\r\n;DEF")]
        public async Task WriteTextMessage(byte[] encoded, string payload)
        {
            var message = Encoding.UTF8.GetBytes(payload);
            var pipe = new Pipe();
            BinaryMessageFormat.WriteLengthPrefix(message.Length, pipe.Writer);
            pipe.Writer.Write(message);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            Assert.Equal(encoded, await pipe.Reader.ReadAllAsync());
        }

        [Theory]
        [MemberData(nameof(RandomPayloads))]
        public async Task RoundTrippingTest(byte[] payload)
        {
            // Make sure we can all the data in a single segment, to make the test faster
            var pipe = new Pipe();
            BinaryMessageFormat.WriteLengthPrefix(payload.Length, pipe.Writer);
            pipe.Writer.Write(payload);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            var result = await pipe.Reader.ReadAsync();
            Assert.True(result.IsCompleted);
            var buffer = result.Buffer;
            Assert.True(BinaryMessageFormat.TryParseMessage(ref buffer, out var roundtripped));
            Assert.Equal(payload, roundtripped.ToArray());
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, "")]
        [InlineData(new byte[] { 0x03, 0x41, 0x42, 0x43 }, "ABC")]
        [InlineData(new byte[] { 0x0B, 0x41, 0x0A, 0x52, 0x0D, 0x43, 0x0D, 0x0A, 0x3B, 0x44, 0x45, 0x46 }, "A\nR\rC\r\n;DEF")]
        public void ReadMessage(byte[] encoded, string payload)
        {
            var buffer = new ReadOnlyBuffer<byte>(encoded);
            Assert.True(BinaryMessageFormat.TryParseMessage(ref buffer, out var message));
            Assert.Equal(0, buffer.Length);

            Assert.Equal(Encoding.UTF8.GetBytes(payload), message.ToArray());
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, new byte[0])]
        [InlineData(new byte[] { 0x04, 0xAB, 0xCD, 0xEF, 0x12 }, new byte[] { 0xAB, 0xCD, 0xEF, 0x12 })]
        [InlineData(new byte[]
            {
                0x80, 0x01, // Size - 128
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f,
                0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f
            },
            new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
                0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f,
                0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f
            })]
        public void ReadBinaryMessage(byte[] encoded, byte[] payload)
        {
            var buffer = new ReadOnlyBuffer<byte>(encoded);
            Assert.True(BinaryMessageFormat.TryParseMessage(ref buffer, out var message));
            Assert.Equal(0, buffer.Length);
            Assert.Equal(payload, message.ToArray());
        }

        [Theory]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
        [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x08 })] // 2GB + 1
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })] // We shouldn't read the last byte!
        public void BinaryMessageFormatStopsReadingLengthAt2GB(byte[] payload)
        {
            var buffer = new ReadOnlyBuffer<byte>(payload);
            var ex = Assert.Throws<FormatException>(() =>
            {
                BinaryMessageFormat.TryParseMessage(ref buffer, out var message);
            });
            Assert.Equal("Messages over 2GB in size are not supported.", ex.Message);
        }

        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x04, 0xAB, 0xCD, 0xEF })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x07 })] // 2GB
        [InlineData(new byte[] { 0x80 })] // size is cut
        public void BinaryMessageFormatReturnsFalseForPartialPayloads(byte[] payload)
        {
            var buffer = new ReadOnlyBuffer<byte>(payload);
            var oldStart = buffer.Start;
            Assert.False(BinaryMessageFormat.TryParseMessage(ref buffer, out var message));
            Assert.Equal(oldStart, buffer.Start);
        }

        [Fact]
        public void ReadMultipleMessages()
        {
            var encoded = new byte[]
            {
                /* length: */ 0x00,
                    /* body: <empty> */
                /* length: */ 0x0E,
                    /* body: */ 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x0D, 0x0A, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x21,
            };

            var buffer = new ReadOnlyBuffer<byte>(encoded);
            var messages = new List<byte[]>();
            while (BinaryMessageFormat.TryParseMessage(ref buffer, out var message))
            {
                messages.Add(message.ToArray());
            }

            Assert.Equal(0, buffer.Length);

            Assert.Equal(2, messages.Count);
            Assert.Equal(new byte[0], messages[0]);
            Assert.Equal(Encoding.UTF8.GetBytes("Hello,\r\nWorld!"), messages[1]);
        }

        [Theory]
        [InlineData(new byte[0])] // Empty
        [InlineData(new byte[] { 0x09, 0x00, 0x00 })] // Not enough data for payload
        public void ReadIncompleteMessages(byte[] encoded)
        {
            var buffer = new ReadOnlyBuffer<byte>(encoded);
            Assert.False(BinaryMessageFormat.TryParseMessage(ref buffer, out var message));
            Assert.Equal(encoded.Length, buffer.Length);
        }

        public static IEnumerable<object[]> RandomPayloads()
        {
            // boundaries
            yield return new[] { CreatePayload(0) };
            yield return new[] { CreatePayload(1) };
            yield return new[] { CreatePayload(0x7f) };
            yield return new[] { CreatePayload(0x80) };
            yield return new[] { CreatePayload(0x3fff) };
            yield return new[] { CreatePayload(0x4000) };

            // random
            yield return new[] { CreatePayload(0xc0de) };
        }


        private static byte[] CreatePayload(int size) =>
            Enumerable.Range(0, size).Select(n => (byte)(n & 0xff)).ToArray();
    }
}
