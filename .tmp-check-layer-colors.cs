using System;
using System.Linq;
using IxMilia.Dxf;

foreach (var file in new[] { @".\\PLANS\\originalFloorPlans\\SANTA-BARBARA.dxf", @".\\PLANS\\originalFloorPlans\\SEMINOLE2000.dxf" })
{
    var dxf = DxfFile.Load(file);
    Console.WriteLine($"FILE: {System.IO.Path.GetFileName(file)}");
    foreach (var layerName in new[] { "DOORS", "WIN", "WINS", "DOORTEXT", "WINDWS LBLS" })
    {
        var layer = dxf.Layers.FirstOrDefault(l => string.Equals(l.Name, layerName, StringComparison.OrdinalIgnoreCase));
        if (layer is null)
        {
            Console.WriteLine($"  {layerName}: <missing>");
            continue;
        }

        var rgb = layer.Color.ToRGB();
        var red = (rgb >> 16) & 0xFF;
        var green = (rgb >> 8) & 0xFF;
        var blue = rgb & 0xFF;
        Console.WriteLine($"  {layerName}: index={layer.Color.RawValue} rgb=({red},{green},{blue})");
    }
}
