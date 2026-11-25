using System.Text.Json;
using System.Xml.Linq;
using FileForge.Core.Converters;
using FileForge.Core.Models;

namespace FileForge.Tests;

public sealed class DataConverterTests : IDisposable
{
    private readonly TempWorkspace _ws = new();
    private readonly DataConverter _converter = new();

    [Fact]
    public async Task ConvertAsync_CsvToJsonAndBack_PreservesRows()
    {
        var csvPath = _ws.WriteText("sensors.csv", "id,name,voltage\n1,NodeA,230.5\n2,NodeB,229.1\n");
        var jsonPath = _ws.PathFor("sensors.json");
        var roundTripCsv = _ws.PathFor("sensors_roundtrip.csv");

        var toJson = await _converter.ConvertAsync(csvPath, jsonPath, ConversionOptions.Default);
        Assert.True(toJson.Success);
        using (var doc = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath)))
        {
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(2, doc.RootElement.GetArrayLength());
        }

        var toCsv = await _converter.ConvertAsync(jsonPath, roundTripCsv, ConversionOptions.Default);
        Assert.True(toCsv.Success);
        var csvOutput = await File.ReadAllTextAsync(roundTripCsv);
        Assert.Contains("NodeA", csvOutput);
        Assert.Contains("229.1", csvOutput);
    }

    [Fact]
    public async Task ConvertAsync_CsvToXmlAndXmlToJson_ProducesValidDocuments()
    {
        var csvPath = _ws.WriteText("devices.csv", "id,label\n10,Inverter\n20,Relay\n");
        var xmlPath = _ws.PathFor("devices.xml");
        var jsonPath = _ws.PathFor("devices.json");

        var toXml = await _converter.ConvertAsync(csvPath, xmlPath, ConversionOptions.Default);
        Assert.True(toXml.Success);
        var xdoc = XDocument.Parse(await File.ReadAllTextAsync(xmlPath));
        Assert.NotNull(xdoc.Root);

        var toJson = await _converter.ConvertAsync(xmlPath, jsonPath, ConversionOptions.Default);
        Assert.True(toJson.Success);
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task ConvertAsync_CsvToTsv_ChangesDelimiter()
    {
        var csvPath = _ws.WriteText("table.csv", "a,b\n1,2\n");
        var tsvPath = _ws.PathFor("table.tsv");

        var res = await _converter.ConvertAsync(csvPath, tsvPath, ConversionOptions.Default);

        Assert.True(res.Success);
        var tsv = await File.ReadAllTextAsync(tsvPath);
        Assert.Contains("a\tb", tsv);
        Assert.Contains("1\t2", tsv);
    }

    [Fact]
    public async Task ConvertAsync_InvalidJson_ReturnsFailWithoutThrowing()
    {
        var badJson = _ws.WriteText("corrupt.json", "{ not valid json");
        var outXml = _ws.PathFor("corrupt.xml");

        var res = await _converter.ConvertAsync(badJson, outXml, ConversionOptions.Default);

        Assert.False(res.Success);
        Assert.False(string.IsNullOrWhiteSpace(res.ErrorMessage));
    }

    public void Dispose() => _ws.Dispose();
}
