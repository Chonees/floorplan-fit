using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaFixedPlanComponentExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_fixture_cabinet_and_insert_block_components_from_santa_barbara()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(components, item => item.Kind == "Cabinet" && item.SourceLayer == "CABS" && item.SourceEntityKind == "COMPONENT-GROUP" && item.GeometryPaths.Count > 1);
        Assert.Contains(components, item => item.Kind == "Toilet" && item.SourceBlockName == "TOILET1" && item.GeometryPaths.Count > 1);
        Assert.Contains(components, item => item.Kind == "Fixture" && item.SourceLayer == "FIXTURES" && item.SourceEntityKind == "COMPONENT-GROUP" && item.GeometryPaths.Count > 1);
        Assert.Contains(components, item => item.Kind == "Appliance" && item.SourceBlockName == "STOVE");
        Assert.Contains(components, item => item.Kind == "Fixture" && item.SourceBlockName == "TUB");
        Assert.Contains(components, item => item.ColorArgb is { Length: 9 } color && color.StartsWith("#FF", StringComparison.Ordinal));
        Assert.All(components, item =>
        {
            Assert.NotEmpty(item.SourceEntityRef);
            Assert.NotEmpty(item.SourceLayer);
            Assert.InRange(item.Confidence, 0.80m, 1m);
            Assert.NotEmpty(item.GeometryPaths);
            Assert.All(item.GeometryPaths, path => Assert.True(path.Count >= 2));
        });
    }

    [Fact]
    public async Task ExtractAsync_reads_seminole_toilet_blocks_and_cabinet_floorplan_geometry()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(components, item => item.Kind == "Toilet" && item.SourceBlockName == "TOILET1");
        Assert.Contains(components, item => item.Kind == "Cabinet" && item.SourceLayer == "CABS-FLOORPLAN" && item.SourceEntityKind == "COMPONENT-GROUP" && item.GeometryPaths.Count > 1);
        Assert.Contains(components, item => item.Kind == "Fixture" && item.SourceLayer == "FIXTURES" && item.SourceEntityKind == "COMPONENT-GROUP" && item.GeometryPaths.Count > 1);
    }

    [Fact]
    public async Task ExtractAsync_relabels_seminole_cabinet_layer_groups_from_nearby_fixture_text()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(components, item =>
            item.SourceLayer == "CABS-FLOORPLAN" &&
            item.SourceEntityKind == "COMPONENT-GROUP" &&
            item.Kind == "Appliance");
        Assert.Contains(components, item =>
            item.SourceLayer == "CABS-FLOORPLAN" &&
            item.SourceEntityKind == "COMPONENT-GROUP" &&
            item.Kind == "Fixture");
    }

    [Fact]
    public async Task ExtractAsync_detects_nested_cabinet_insert_when_outer_insert_uses_generic_layer_and_name()
    {
        var sourcePath = CreateNestedCabinetFixture();
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        try
        {
            var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

            var cabinet = Assert.Single(components, item => item.Kind == "Cabinet");
            Assert.Equal("CAB_ASSEMBLY", cabinet.SourceBlockName);
            Assert.Equal("CABS-FLOORPLAN", cabinet.SourceLayer);
            Assert.NotEmpty(cabinet.GeometryPaths);
            Assert.All(cabinet.GeometryPaths, path => Assert.True(path.Count >= 2));
            Assert.NotNull(cabinet.ColorArgb);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task ExtractAsync_groups_direct_fixture_geometry_into_curable_components()
    {
        var sourcePath = CreateDirectFixtureGroupingFixture();
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        try
        {
            var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

            Assert.Equal(2, components.Count);
            Assert.All(components, component =>
            {
                Assert.Equal("Fixture", component.Kind);
                Assert.Equal("FIXTURES", component.SourceLayer);
                Assert.True(component.GeometryPaths.Count >= 2);
            });
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task ExtractAsync_keeps_cabinet_geometry_separate_when_sink_text_marks_only_inner_symbol()
    {
        var sourcePath = CreateCabinetWithInnerSinkFixture();
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        try
        {
            var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

            Assert.Contains(components, item =>
                item.SourceLayer == "CABS-FLOORPLAN" &&
                item.Kind == "Cabinet");
            Assert.Contains(components, item =>
                item.SourceLayer == "CABS-FLOORPLAN" &&
                item.Kind == "Fixture");
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task ExtractAsync_includes_supported_l1_fixture_geometry_when_layer_is_opened_for_review()
    {
        var sourcePath = CreateL1SinkFixture();
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        try
        {
            var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

            Assert.Contains(components, item =>
                item.SourceLayer == "L1" &&
                item.Kind == "Fixture" &&
                item.GeometryPaths.Count >= 3);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    private static string CreateNestedCabinetFixture()
    {
        var dxf = new DxfFile();
        dxf.Layers.Add(new DxfLayer("2"));
        dxf.Layers.Add(new DxfLayer("CABS-FLOORPLAN", DxfColor.FromIndex(4)));

        var cabinetDetail = new DxfBlock
        {
            Name = "CAB_DETAIL",
            Layer = "0",
            BasePoint = new DxfPoint(0d, 0d, 0d)
        };
        cabinetDetail.Entities.Add(new DxfCircle(new DxfPoint(1d, 1d, 0d), 0.5d)
        {
            Layer = "CABS-FLOORPLAN"
        });
        cabinetDetail.Entities.Add(new DxfLine(new DxfPoint(0d, 0d, 0d), new DxfPoint(2d, 0d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });

        var cabinetAssembly = new DxfBlock
        {
            Name = "CAB_ASSEMBLY",
            Layer = "0",
            BasePoint = new DxfPoint(0d, 0d, 0d)
        };
        cabinetAssembly.Entities.Add(new DxfInsert
        {
            Name = "CAB_DETAIL",
            Layer = "2",
            Location = new DxfPoint(5d, 5d, 0d),
            XScaleFactor = 1d,
            YScaleFactor = 1d,
            ZScaleFactor = 1d
        });

        dxf.Blocks.Add(cabinetDetail);
        dxf.Blocks.Add(cabinetAssembly);
        dxf.Entities.Add(new DxfInsert
        {
            Name = "CAB_ASSEMBLY",
            Layer = "2",
            Location = new DxfPoint(100d, 200d, 0d),
            XScaleFactor = 1d,
            YScaleFactor = 1d,
            ZScaleFactor = 1d
        });

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(sourcePath, false);
        return sourcePath;
    }

    private static string CreateDirectFixtureGroupingFixture()
    {
        var dxf = new DxfFile();
        dxf.Layers.Add(new DxfLayer("FIXTURES", DxfColor.FromIndex(1)));

        dxf.Entities.Add(new DxfLine(new DxfPoint(0d, 0d, 0d), new DxfPoint(4d, 0d, 0d))
        {
            Layer = "FIXTURES"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(4d, 0d, 0d), new DxfPoint(4d, 2d, 0d))
        {
            Layer = "FIXTURES"
        });
        dxf.Entities.Add(new DxfArc
        {
            Layer = "FIXTURES",
            Center = new DxfPoint(2d, 1d, 0d),
            Radius = 0.5d,
            StartAngle = 0d,
            EndAngle = 180d
        });

        dxf.Entities.Add(new DxfLine(new DxfPoint(20d, 0d, 0d), new DxfPoint(24d, 0d, 0d))
        {
            Layer = "FIXTURES"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(24d, 0d, 0d), new DxfPoint(24d, 2d, 0d))
        {
            Layer = "FIXTURES"
        });
        dxf.Entities.Add(new DxfCircle(new DxfPoint(22d, 1d, 0d), 0.5d)
        {
            Layer = "FIXTURES"
        });

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(sourcePath, false);
        return sourcePath;
    }

    private static string CreateCabinetWithInnerSinkFixture()
    {
        var dxf = new DxfFile();
        dxf.Layers.Add(new DxfLayer("CABS-FLOORPLAN", DxfColor.FromIndex(4)));
        dxf.Layers.Add(new DxfLayer("NOTES", DxfColor.FromIndex(7)));

        dxf.Entities.Add(new DxfLine(new DxfPoint(0d, 0d, 0d), new DxfPoint(40d, 0d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(40d, 0d, 0d), new DxfPoint(40d, 20d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(40d, 20d, 0d), new DxfPoint(0d, 20d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(0d, 20d, 0d), new DxfPoint(0d, 0d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });

        dxf.Entities.Add(new DxfCircle(new DxfPoint(8d, 10d, 0d), 4d)
        {
            Layer = "CABS-FLOORPLAN"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(4d, 10d, 0d), new DxfPoint(12d, 10d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });
        dxf.Entities.Add(new DxfLine(new DxfPoint(0d, 10d, 0d), new DxfPoint(4d, 10d, 0d))
        {
            Layer = "CABS-FLOORPLAN"
        });

        dxf.Entities.Add(new DxfText(new DxfPoint(8d, 24d, 0d), 2.5d, "SINK")
        {
            Layer = "NOTES"
        });

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(sourcePath, false);
        return sourcePath;
    }

    private static string CreateL1SinkFixture()
    {
        var dxf = new DxfFile();
        dxf.Layers.Add(new DxfLayer("L1", DxfColor.FromIndex(12)));

        dxf.Entities.Add(new Dxf3DFace
        {
            FirstCorner = new DxfPoint(0d, 0d, 0d),
            SecondCorner = new DxfPoint(12d, 0d, 0d),
            ThirdCorner = new DxfPoint(12d, 8d, 0d),
            FourthCorner = new DxfPoint(0d, 8d, 0d),
            Layer = "L1"
        });
        dxf.Entities.Add(new Dxf3DFace
        {
            FirstCorner = new DxfPoint(0d, 8d, 0d),
            SecondCorner = new DxfPoint(12d, 8d, 0d),
            ThirdCorner = new DxfPoint(12d, 16d, 0d),
            FourthCorner = new DxfPoint(0d, 16d, 0d),
            Layer = "L1"
        });
        dxf.Entities.Add(new Dxf3DFace
        {
            FirstCorner = new DxfPoint(0d, 16d, 0d),
            SecondCorner = new DxfPoint(12d, 16d, 0d),
            ThirdCorner = new DxfPoint(12d, 24d, 0d),
            FourthCorner = new DxfPoint(0d, 24d, 0d),
            Layer = "L1"
        });
        dxf.Entities.Add(new DxfEllipse
        {
            Layer = "L1",
            Center = new DxfPoint(20d, 12d, 0d),
            MajorAxis = new DxfPoint(8d, 0d, 0d),
            MinorAxisRatio = 0.5d,
            StartParameter = 0d,
            EndParameter = Math.PI * 2d
        });
        dxf.Entities.Add(new DxfCircle(new DxfPoint(20d, 12d, 0d), 1d)
        {
            Layer = "L1"
        });
        dxf.Entities.Add(new DxfCircle(new DxfPoint(12d, 4d, 0d), 0.8d)
        {
            Layer = "L1"
        });
        dxf.Entities.Add(new DxfCircle(new DxfPoint(20d, 4d, 0d), 0.8d)
        {
            Layer = "L1"
        });
        dxf.Entities.Add(new DxfCircle(new DxfPoint(28d, 4d, 0d), 0.8d)
        {
            Layer = "L1"
        });
        dxf.Entities.Add(new DxfArc
        {
            Layer = "L1",
            Center = new DxfPoint(20d, 12d, 0d),
            Radius = 8d,
            StartAngle = 0d,
            EndAngle = 180d
        });
        dxf.Entities.Add(new DxfArc
        {
            Layer = "L1",
            Center = new DxfPoint(20d, 12d, 0d),
            Radius = 6d,
            StartAngle = 180d,
            EndAngle = 360d
        });
        dxf.Entities.Add(new DxfArc
        {
            Layer = "L1",
            Center = new DxfPoint(26d, 2d, 0d),
            Radius = 3d,
            StartAngle = 90d,
            EndAngle = 180d
        });

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(sourcePath, false);
        return sourcePath;
    }
}
