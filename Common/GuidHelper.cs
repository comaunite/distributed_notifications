using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Common;

public static class GuidHelper
{
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "SHA1 is required for UUIDv5 generation. Weak algorithm isn't a flaw here.")]
    public static Guid CreateDeterministic(Guid notificationId, Guid userId, int channelType)
    {
        // 16 bytes for notificationId + 16 bytes for userId + 4 bytes for channel int = 36 bytes
        Span<byte> input = stackalloc byte[36];

        // Write IDs directly to the stack buffer as bytes (avoiding strings entirely)
        MemoryMarshal.TryWrite(input[..16], in notificationId);
        MemoryMarshal.TryWrite(input[16..32], in userId);
        BinaryPrimitives.WriteInt32LittleEndian(input[32..], channelType);

        // Perform SHA1 hash directly from the span
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);

        // Take the first 16 bytes of the hash to form the GUID
        Span<byte> newGuidBytes = hash[..16];

        // Set version (5) and variant (RFC 4122) bits
        newGuidBytes[6] = (byte)((newGuidBytes[6] & 0x0F) | (5 << 4));
        newGuidBytes[8] = (byte)((newGuidBytes[8] & 0x3F) | 0x80);

        return new Guid(newGuidBytes);
    }
}