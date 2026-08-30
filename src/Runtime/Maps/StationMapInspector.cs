using System;
using System.Collections.Generic;
using System.IO;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Maps
{
    public class StationMapReport
    {
        public string MapPath { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public int TotalTileDefinitions { get; set; }
        public int TotalTurfsLoaded { get; set; }
        public int TotalObjectsLoaded { get; set; }
        public int TotalAirlocks { get; set; }
        public int TotalMachines { get; set; }
        public int TotalLights { get; set; }
    }

    public static class StationMapInspector
    {
        public static StationMapReport InspectMapFile(string mapFilePath)
        {
            StationMapReport report = new StationMapReport();
            report.MapPath = mapFilePath;

            if (!File.Exists(mapFilePath))
            {
                return report;
            }

            string content = File.ReadAllText(mapFilePath);
            DMMModel model = DMMParser.Parse(content);

            report.SizeX = model.SizeX;
            report.SizeY = model.SizeY;
            report.SizeZ = model.SizeZ;
            report.TotalTileDefinitions = model.TileDefinitions.Count;

            int turfs = 0;
            int objects = 0;
            int airlocks = 0;
            int machines = 0;
            int lights = 0;

            for (int z = 1; z <= model.SizeZ; z++)
            {
                for (int x = 1; x <= model.SizeX; x++)
                {
                    for (int y = 1; y <= model.SizeY; y++)
                    {
                        DM_turf t = DMSpatialGrid.Instance.GetTurf(x, y, z);
                        if (t != null)
                        {
                            turfs++;
                            foreach (DMValue c in t.contents)
                            {
                                if (c.IsObject)
                                {
                                    objects++;
                                    string objName = c.AsObject.name.AsString.ToLowerInvariant();
                                    string typeStr = c.AsObject.TypePath.PathString.ToLowerInvariant();

                                    if (typeStr.Contains("door") || typeStr.Contains("airlock") || objName.Contains("airlock")) airlocks++;
                                    if (typeStr.Contains("machine") || typeStr.Contains("computer")) machines++;
                                    if (typeStr.Contains("light")) lights++;
                                }
                            }
                        }
                    }
                }
            }

            report.TotalTurfsLoaded = turfs;
            report.TotalObjectsLoaded = objects;
            report.TotalAirlocks = airlocks;
            report.TotalMachines = machines;
            report.TotalLights = lights;

            return report;
        }
    }
}
