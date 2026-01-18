using System;
using System.Security.Cryptography;

namespace Dpz.Core.WebMore.Helper;

// Source - https://stackoverflow.com/a/76734976
// Posted by LoneSpawn, modified by community. See post 'Timeline' for change history
// Retrieved 2026-01-19, License - CC BY-SA 4.0
// Modified to inherit HashAlgorithm for consistency

public sealed class MD5Hash : HashAlgorithm
{
    private uint _a = 0x67452301;
    private uint _b = 0xEFCDAB89;
    private uint _c = 0x98BADCFE;
    private uint _d = 0x10325476;
    private readonly byte[] _buffer = new byte[64];
    private int _bufferLength;
    private long _totalBytesProcessed;

    public MD5Hash()
    {
        HashSizeValue = 128;
        Initialize();
    }

    public override void Initialize()
    {
        _a = 0x67452301;
        _b = 0xEFCDAB89;
        _c = 0x98BADCFE;
        _d = 0x10325476;
        _bufferLength = 0;
        _totalBytesProcessed = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        _totalBytesProcessed += cbSize;
        var i = ibStart;
        var remaining = cbSize;

        // Process buffered data first
        if (_bufferLength > 0 && _bufferLength + remaining >= 64)
        {
            var toCopy = 64 - _bufferLength;
            Buffer.BlockCopy(array, i, _buffer, _bufferLength, toCopy);
            ProcessBlock(_buffer, 0);
            _bufferLength = 0;
            i += toCopy;
            remaining -= toCopy;
        }

        // Process complete blocks
        while (remaining >= 64)
        {
            ProcessBlock(array, i);
            i += 64;
            remaining -= 64;
        }

        // Buffer remaining data
        if (remaining > 0)
        {
            Buffer.BlockCopy(array, i, _buffer, _bufferLength, remaining);
            _bufferLength += remaining;
        }
    }

    protected override byte[] HashFinal()
    {
        // Add padding
        var totalBits = _totalBytesProcessed * 8;
        var paddingLength = (56 - (_bufferLength % 64)) % 64;
        if (paddingLength == 0)
            paddingLength = 56;

        var padding = new byte[paddingLength + 8];
        padding[0] = 0x80;
        BitConverter.GetBytes(totalBits).CopyTo(padding, paddingLength);

        HashCore(padding, 0, padding.Length);

        // Generate hash
        var hash = new byte[16];
        BitConverter.GetBytes(_a).CopyTo(hash, 0);
        BitConverter.GetBytes(_b).CopyTo(hash, 4);
        BitConverter.GetBytes(_c).CopyTo(hash, 8);
        BitConverter.GetBytes(_d).CopyTo(hash, 12);

        return hash;
    }

    private void ProcessBlock(byte[] block, int offset)
    {
        var x = new uint[16];
        for (int i = 0; i < 16; i++)
        {
            x[i] = BitConverter.ToUInt32(block, offset + i * 4);
        }

        uint aa = _a,
            bb = _b,
            cc = _c,
            dd = _d;

        // Round 1
        for (var i = 0; i < 16; i++)
        {
            var f = (bb & cc) | (~bb & dd);
            var g = (uint)i;
            var temp = dd;
            dd = cc;
            cc = bb;
            bb += LeftRotate(aa + f + K[i] + x[g], S[i]);
            aa = temp;
        }

        // Round 2
        for (var i = 16; i < 32; i++)
        {
            var f = (dd & bb) | (~dd & cc);
            uint g = (5 * (uint)i + 1) % 16;
            uint temp = dd;
            dd = cc;
            cc = bb;
            bb += LeftRotate(aa + f + K[i] + x[g], S[i]);
            aa = temp;
        }

        // Round 3
        for (var i = 32; i < 48; i++)
        {
            var f = bb ^ cc ^ dd;
            var g = (3 * (uint)i + 5) % 16;
            var temp = dd;
            dd = cc;
            cc = bb;
            bb = bb + LeftRotate(aa + f + K[i] + x[g], S[i]);
            aa = temp;
        }

        // Round 4
        for (var i = 48; i < 64; i++)
        {
            uint f = cc ^ (bb | ~dd);
            uint g = (7 * (uint)i) % 16;
            uint temp = dd;
            dd = cc;
            cc = bb;
            bb = bb + LeftRotate(aa + f + K[i] + x[g], S[i]);
            aa = temp;
        }

        _a += aa;
        _b += bb;
        _c += cc;
        _d += dd;
    }

    // private static uint LeftRotate(uint x, int c)
    // {
    //     return (x << c) | (x >> (32 - c));
    // }

    public new static MD5Hash Create()
    {
        return new MD5Hash();
    }

    private static readonly int[] S =
    [
        7,
        12,
        17,
        22,
        7,
        12,
        17,
        22,
        7,
        12,
        17,
        22,
        7,
        12,
        17,
        22,
        5,
        9,
        14,
        20,
        5,
        9,
        14,
        20,
        5,
        9,
        14,
        20,
        5,
        9,
        14,
        20,
        4,
        11,
        16,
        23,
        4,
        11,
        16,
        23,
        4,
        11,
        16,
        23,
        4,
        11,
        16,
        23,
        6,
        10,
        15,
        21,
        6,
        10,
        15,
        21,
        6,
        10,
        15,
        21,
        6,
        10,
        15,
        21,
    ];

    private static readonly uint[] K =
    [
        3614090360u,
        3905402710u,
        606105819u,
        3250441966u,
        4118548399u,
        1200080426u,
        2821735955u,
        4249261313u,
        1770035416u,
        2336552879u,
        4294925233u,
        2304563134u,
        1804603682u,
        4254626195u,
        2792965006u,
        1236535329u,
        4129170786u,
        3225465664u,
        643717713u,
        3921069994u,
        3593408605u,
        38016083u,
        3634488961u,
        3889429448u,
        568446438u,
        3275163606u,
        4107603335u,
        1163531501u,
        2850285829u,
        4243563512u,
        1735328473u,
        2368359562u,
        4294588738u,
        2272392833u,
        1839030562u,
        4259657740u,
        2763975236u,
        1272893353u,
        4139469664u,
        3200236656u,
        681279174u,
        3936430074u,
        3572445317u,
        76029189u,
        3654602809u,
        3873151461u,
        530742520u,
        3299628645u,
        4096336452u,
        1126891415u,
        2878612391u,
        4237533241u,
        1700485571u,
        2399980690u,
        4293915773u,
        2240044497u,
        1873313359u,
        4264355552u,
        2734768916u,
        1309151649u,
        4149444226u,
        3174756917u,
        718787259u,
        3951481745u,
    ];

    private static uint LeftRotate(uint x, int c)
    {
        return x << c | x >> 32 - c;
    }
}
