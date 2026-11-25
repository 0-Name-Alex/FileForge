using System.Security.Cryptography;
using System.Text;
using FileForge.Core.Converters;
using FileForge.Core.Models;

namespace FileForge.Tests;

public sealed class DeveloperConverterTests : IDisposable
{
    private readonly TempWorkspace _ws = new();
    private readonly DeveloperConverter _converter = new();

    [Fact]
    public async Task ConvertAsync_Base64AndHexRoundTrip_PreservesPayload()
    {
        const string payload = "FileForge round-trip check 12345!";
        var txtPath = _ws.WriteText("input.txt", payload);
        var b64Path = _ws.PathFor("encoded.b64");
        var decodedFromB64 = _ws.PathFor("decoded_b64.txt");
        var hexPath = _ws.PathFor("encoded.hex");
        var decodedFromHex = _ws.PathFor("decoded_hex.txt");

        Assert.True((await _converter.ConvertAsync(txtPath, b64Path, ConversionOptions.Default)).Success);
        Assert.True((await _converter.ConvertAsync(b64Path, decodedFromB64, ConversionOptions.Default)).Success);
        Assert.Equal(payload, await File.ReadAllTextAsync(decodedFromB64));

        Assert.True((await _converter.ConvertAsync(txtPath, hexPath, ConversionOptions.Default)).Success);
        Assert.True((await _converter.ConvertAsync(hexPath, decodedFromHex, ConversionOptions.Default)).Success);
        Assert.Equal(payload, await File.ReadAllTextAsync(decodedFromHex));
    }

    [Fact]
    public async Task ConvertAsync_Sha256AndMd5_MatchExpectedDigests()
    {
        var data = Encoding.UTF8.GetBytes("deterministic digest payload");
        var binPath = _ws.WriteBytes("payload.bin", data);
        var shaPath = _ws.PathFor("payload.sha256");
        var md5Path = _ws.PathFor("payload.md5");

        var shaRes = await _converter.ConvertAsync(binPath, shaPath, ConversionOptions.Default);
        var md5Res = await _converter.ConvertAsync(binPath, md5Path, ConversionOptions.Default);

        Assert.True(shaRes.Success);
        Assert.True(md5Res.Success);

        var expectedSha = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        var expectedMd5 = Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

        Assert.StartsWith(expectedSha, await File.ReadAllTextAsync(shaPath));
        Assert.StartsWith(expectedMd5, await File.ReadAllTextAsync(md5Path));
    }

    public void Dispose() => _ws.Dispose();
}
