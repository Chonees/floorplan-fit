using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.ViewModels;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class CommissionedHouseFitPreviewProjectorTests
{
    [Fact]
    public void Apply_projects_source_recipe_once_and_preserves_rigid_and_fixed_invariants()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var opening = LinePath(10m, 2m, 11m, 2m);
        var unrelated = LinePath(0m, 5m, 5m, 5m);
        var action = Action(firstWall, secondWall, opening, unrelated);

        var projectedGeometry = new[] { firstWall, secondWall, opening, unrelated }
            .Select(path => Project(path, scale: 2m, offsetX: 100m, offsetY: 200m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [action],
            projectionScale: 2m,
            projectionOffsetX: 100m,
            projectionOffsetY: 200m);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.Equal(16m, Length(result.GeometryPaths.Single(path => path.Id == firstWall.Id)));
        Assert.Equal(16m, Length(result.GeometryPaths.Single(path => path.Id == secondWall.Id)));

        var projectedOpening = result.GeometryPaths.Single(path => path.Id == opening.Id);
        Assert.Equal(2m, Length(projectedOpening));
        Assert.Equal(116m, projectedOpening.Segments[0].StartX);
        Assert.Equal(118m, projectedOpening.Segments[0].EndX);

        var expectedUnrelated = projectedGeometry.Single(path => path.Id == unrelated.Id);
        var actualUnrelated = result.GeometryPaths.Single(path => path.Id == unrelated.Id);
        Assert.Equal(expectedUnrelated.IsClosed, actualUnrelated.IsClosed);
        Assert.Equal(expectedUnrelated.Segments.ToArray(), actualUnrelated.Segments.ToArray());

        Assert.Equal(10m, action.CutCoordinate);
        Assert.Equal(2m, action.DeltaSourceUnits);
        Assert.Equal(5m, action.MaxDeltaSourceUnits);
        Assert.Equal(0.001m, action.CoordinateTolerance);
        Assert.Equal(new AdjustmentRecipeBoundsDto(0m, 0m, 11m, 5m), action.CanonicalSourceBounds);
        Assert.Equal(0m, action.TargetSpans[0].StartX);
        Assert.Equal(10m, action.TargetSpans[0].EndX);
    }

    [Fact]
    public void Apply_fails_closed_and_returns_the_untouched_preview_when_the_recipe_is_invalid()
    {
        var wall = LinePath(0m, 0m, 10m, 0m);
        var projected = Project(wall, 1m, 0m, 0m);
        var invalid = Action(wall, LinePath(0m, 1m, 10m, 1m), null, null) with
        {
            DeltaSourceUnits = 10m,
            MaxDeltaSourceUnits = 1m
        };

        var result = CommissionedHouseFitPreviewProjector.Apply(
            [projected],
            [invalid],
            1m,
            0m,
            0m);

        Assert.False(result.Succeeded);
        Assert.Equal(projected, Assert.Single(result.GeometryPaths));
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    [Fact]
    public void Apply_keeps_a_fixed_source_only_label_value_equivalent()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var label = RoomLabel("LABEL:FIXED", 104m, 206m) with
        {
            DetectedX = 103m,
            DetectedY = 205m
        };
        var sourceRoles = Action(firstWall, secondWall, null, null).CanonicalEntityRoles
            .Append(SourceOnlyRole(label.SourceEntityRef, "Fixed"))
            .ToArray();
        var action = Action(firstWall, secondWall, null, null) with
        {
            CanonicalEntityRoles = sourceRoles
        };
        var sourceActions = new[] { action };
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 2m, offsetX: 100m, offsetY: 200m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [label],
            [],
            [],
            sourceActions,
            projectionScale: 2m,
            projectionOffsetX: 100m,
            projectionOffsetY: 200m);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.Same(label, Assert.Single(result.RoomLabels));
        Assert.Same(action, sourceActions[0]);
        Assert.Same(sourceRoles, action.CanonicalEntityRoles);
        var annotation = Assert.Single(action.CanonicalEntityRoles.Where(role => role.EntityRef == label.SourceEntityRef));
        Assert.Equal("Fixed", annotation.Role);
        Assert.Null(annotation.GeometryPathId);
        Assert.Null(annotation.SegmentSortOrder);
        Assert.Empty(annotation.VertexIndices);
    }

    [Fact]
    public void Apply_translates_a_rigid_source_only_label_by_the_scaled_signed_vector_once()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var label = RoomLabel("LABEL:RIGID", 124m, 206m) with
        {
            DetectedX = 123m,
            DetectedY = 205m
        };
        var action = WithSourceOnlyRole(
            Action(firstWall, secondWall, null, null),
            label.SourceEntityRef,
            "RigidMove");
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 2m, offsetX: 100m, offsetY: 200m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [label],
            [],
            [],
            [action],
            projectionScale: 2m,
            projectionOffsetX: 100m,
            projectionOffsetY: 200m);

        Assert.True(result.Succeeded, result.RejectionReason);
        var translated = Assert.Single(result.RoomLabels);
        Assert.Equal(120m, translated.X);
        Assert.Equal(206m, translated.Y);
        Assert.Equal(119m, translated.DetectedX);
        Assert.Equal(205m, translated.DetectedY);
        Assert.Equal(label with { X = 120m, DetectedX = 119m }, translated);
    }

    [Fact]
    public void Apply_translates_every_dimension_coordinate_and_preserves_size_angle_measurement_and_payload()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var dimension = Dimension("DIMENSION:RIGID");
        var action = WithSourceOnlyRole(
            Action(firstWall, secondWall, null, null),
            dimension.SourceEntityRef,
            "RigidMove");
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 3m, offsetX: 0m, offsetY: 0m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [],
            [],
            [dimension],
            [action],
            projectionScale: 3m,
            projectionOffsetX: 0m,
            projectionOffsetY: 0m);

        Assert.True(result.Succeeded, result.RejectionReason);
        var translated = Assert.Single(result.Dimensions);
        Assert.Equal(dimension.DefPointX - 6m, translated.DefPointX);
        Assert.Equal(dimension.DefPoint2X - 6m, translated.DefPoint2X);
        Assert.Equal(dimension.DefPoint3X - 6m, translated.DefPoint3X);
        Assert.Equal(dimension.RenderTextX - 6m, translated.RenderTextX);
        Assert.Equal(dimension.LineSegments[0].StartX - 6m, translated.LineSegments[0].StartX);
        Assert.Equal(dimension.LinePrimitives[0].EndX - 6m, translated.LinePrimitives[0].EndX);
        Assert.Equal(dimension.TextPrimitives[0].X - 6m, translated.TextPrimitives[0].X);
        Assert.Equal(dimension.InsertPrimitives[0].X - 6m, translated.InsertPrimitives[0].X);
        Assert.Equal(dimension.CirclePrimitives[0].CenterX - 6m, translated.CirclePrimitives[0].CenterX);
        Assert.Equal(dimension.ArcPrimitives[0].CenterX - 6m, translated.ArcPrimitives[0].CenterX);
        Assert.Equal(dimension.SolidPrimitives[0].Point4X - 6m, translated.SolidPrimitives[0].Point4X);
        Assert.Equal(dimension.DefPointY, translated.DefPointY);
        Assert.Equal(dimension.MeasurementSourceUnits, translated.MeasurementSourceUnits);
        Assert.Equal(dimension.MeasurementMillimeters, translated.MeasurementMillimeters);
        Assert.Equal(dimension.Angle, translated.Angle);
        Assert.Equal(dimension.ObliqueAngle, translated.ObliqueAngle);
        Assert.Equal(dimension.RenderTextHeight, translated.RenderTextHeight);
        Assert.Equal(dimension.RenderTextRotationDegrees, translated.RenderTextRotationDegrees);
        Assert.Equal(dimension.TextPrimitives[0].Height, translated.TextPrimitives[0].Height);
        Assert.Equal(dimension.TextPrimitives[0].RotationDegrees, translated.TextPrimitives[0].RotationDegrees);
        Assert.Equal(dimension.InsertPrimitives[0].ScaleX, translated.InsertPrimitives[0].ScaleX);
        Assert.Equal(dimension.CirclePrimitives[0].Radius, translated.CirclePrimitives[0].Radius);
        Assert.Equal(dimension.ArcPrimitives[0].Radius, translated.ArcPrimitives[0].Radius);
        Assert.Equal(dimension.ArcPrimitives[0].StartAngleDegrees, translated.ArcPrimitives[0].StartAngleDegrees);
        Assert.Equal(dimension.DisplayText, translated.DisplayText);
        Assert.Equal(dimension.RawTextOverride, translated.RawTextOverride);
    }

    [Fact]
    public void Apply_fails_closed_for_an_incomplete_dimension_payload()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var dimension = Dimension("DIMENSION:INCOMPLETE") with { LineSegments = null! };
        var action = WithSourceOnlyRole(
            Action(firstWall, secondWall, null, null),
            dimension.SourceEntityRef,
            "RigidMove");
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 2m, offsetX: 0m, offsetY: 0m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [],
            [],
            [dimension],
            [action],
            projectionScale: 2m,
            projectionOffsetX: 0m,
            projectionOffsetY: 0m);

        Assert.False(result.Succeeded);
        Assert.Same(dimension, Assert.Single(result.Dimensions));
        Assert.Contains(dimension.SourceEntityRef, result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("incomplete", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_accumulates_width_and_height_rigid_moves_per_source_only_annotation()
    {
        var widthA = LinePath(0m, 0m, 10m, 0m);
        var widthB = LinePath(0m, 1m, 10m, 1m);
        var heightA = LinePath(20m, 0m, 20m, 10m);
        var heightB = LinePath(21m, 0m, 21m, 10m);
        var label = new OpeningLabelDto(
            Guid.NewGuid(), "OPENING-LABEL:CUMULATIVE", "OPENINGS", "Door", "D1",
            50m, 60m, 1m, null, 1, "TEXT", 2.25m, 17m);
        var width = WithSourceOnlyRole(
            Action(widthA, widthB, null, null),
            label.SourceEntityRef,
            "RigidMove");
        var height = WithSourceOnlyRole(
            Action(heightA, heightB, null, null, axis: "Height", edge: "Top", delta: 3m),
            label.SourceEntityRef,
            "RigidMove");
        var projectedGeometry = new[] { widthA, widthB, heightA, heightB }
            .Select(path => Project(path, scale: 2m, offsetX: 0m, offsetY: 0m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [],
            [label],
            [],
            [width, height],
            projectionScale: 2m,
            projectionOffsetX: 0m,
            projectionOffsetY: 0m);

        Assert.True(result.Succeeded, result.RejectionReason);
        var translated = Assert.Single(result.OpeningLabels);
        Assert.Equal(46m, translated.X);
        Assert.Equal(54m, translated.Y);
        Assert.Equal(label.TextHeight, translated.TextHeight);
        Assert.Equal(label.RotationDegrees, translated.RotationDegrees);
    }

    [Fact]
    public void Apply_fails_closed_when_a_source_only_annotation_has_no_role_for_an_action()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var label = RoomLabel("LABEL:MISSING", 20m, 30m);
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 1m, offsetX: 0m, offsetY: 0m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [label],
            [],
            [],
            [Action(firstWall, secondWall, null, null)],
            projectionScale: 1m,
            projectionOffsetX: 0m,
            projectionOffsetY: 0m);

        Assert.False(result.Succeeded);
        Assert.Same(label, Assert.Single(result.RoomLabels));
        Assert.Contains(label.SourceEntityRef, result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("width-action", result.RejectionReason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("duplicate", "duplicate roles")]
    [InlineData("unsupported", "unsupported role")]
    [InlineData("contradictory", "contradictory role binding")]
    [InlineData("incomplete", "incomplete or contradictory")]
    public void Apply_fails_closed_with_entity_and_action_for_invalid_source_only_roles(
        string defect,
        string expectedReason)
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var label = RoomLabel("LABEL:INVALID", 20m, 30m);
        var action = Action(firstWall, secondWall, null, null);
        AdjustmentRecipeEntityRoleDto[] invalidRoles = defect switch
        {
            "duplicate" =>
            [
                SourceOnlyRole(label.SourceEntityRef, "Fixed"),
                SourceOnlyRole(label.SourceEntityRef, "RigidMove")
            ],
            "unsupported" => [SourceOnlyRole(label.SourceEntityRef, "Stretch")],
            "contradictory" =>
            [
                new AdjustmentRecipeEntityRoleDto(
                    label.SourceEntityRef,
                    firstWall.Id,
                    firstWall.Segments[0].SortOrder,
                    "Fixed",
                    [])
            ],
            _ => [new AdjustmentRecipeEntityRoleDto(label.SourceEntityRef, null, null, "Fixed", [0])]
        };
        action = action with
        {
            CanonicalEntityRoles = action.CanonicalEntityRoles.Concat(invalidRoles).ToArray()
        };
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 1m, offsetX: 0m, offsetY: 0m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [label],
            [],
            [],
            [action],
            projectionScale: 1m,
            projectionOffsetX: 0m,
            projectionOffsetY: 0m);

        Assert.False(result.Succeeded);
        Assert.Contains(label.SourceEntityRef, result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("width-action", result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains(expectedReason, result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_fails_closed_instead_of_ignoring_a_stretch_role_without_geometry_identity()
    {
        var firstWall = LinePath(0m, 0m, 10m, 0m);
        var secondWall = LinePath(0m, 1m, 10m, 1m);
        var action = Action(firstWall, secondWall, null, null);
        var malformedRoles = action.CanonicalEntityRoles
            .Append(new AdjustmentRecipeEntityRoleDto("WALL:MALFORMED", null, null, "Stretch", []))
            .ToArray();
        var malformedAction = action with { CanonicalEntityRoles = malformedRoles };
        var projectedGeometry = new[] { firstWall, secondWall }
            .Select(path => Project(path, scale: 2m, offsetX: 100m, offsetY: 200m))
            .ToArray();

        var result = CommissionedHouseFitPreviewProjector.Apply(
            projectedGeometry,
            [],
            [],
            [],
            [malformedAction],
            projectionScale: 2m,
            projectionOffsetX: 100m,
            projectionOffsetY: 200m);

        Assert.False(result.Succeeded);
        Assert.Equal(projectedGeometry, result.GeometryPaths);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    private static AdjustmentRecipeStretchActionDto Action(
        GeometryPathDto firstWall,
        GeometryPathDto secondWall,
        GeometryPathDto? opening,
        GeometryPathDto? unrelated,
        string axis = "Width",
        string edge = "Right",
        decimal delta = 2m)
    {
        var roles = new List<AdjustmentRecipeEntityRoleDto>
        {
            Role(firstWall, "WALL:A", "Stretch", [1]),
            Role(secondWall, "WALL:B", "Stretch", [1])
        };
        if (opening is not null)
        {
            roles.Add(Role(opening, "OPENING:A", "RigidMove", []));
        }

        if (unrelated is not null)
        {
            roles.Add(Role(unrelated, "WALL:FIXED", "Fixed", []));
        }

        return new AdjustmentRecipeStretchActionDto(
            string.Equals(axis, "Width", StringComparison.OrdinalIgnoreCase) ? "width-action" : "height-action",
            axis,
            edge,
            10m,
            delta,
            5m,
            0.001m,
            new AdjustmentRecipeBoundsDto(0m, 0m, 11m, 5m),
            [Span(firstWall, "WALL:A"), Span(secondWall, "WALL:B")],
            roles);
    }

    private static AdjustmentRecipeStretchActionDto WithSourceOnlyRole(
        AdjustmentRecipeStretchActionDto action,
        string sourceEntityRef,
        string role)
        => action with
        {
            CanonicalEntityRoles = action.CanonicalEntityRoles
                .Append(SourceOnlyRole(sourceEntityRef, role))
                .ToArray()
        };

    private static AdjustmentRecipeEntityRoleDto SourceOnlyRole(string sourceEntityRef, string role)
        => new(sourceEntityRef, null, null, role, []);

    private static AdjustmentRecipeEntityRoleDto Role(
        GeometryPathDto path,
        string entityRef,
        string role,
        IReadOnlyList<int> vertices)
        => new(entityRef, path.Id, path.Segments[0].SortOrder, role, vertices);

    private static AdjustmentRecipeTargetSpanDto Span(GeometryPathDto path, string entityRef)
    {
        var segment = path.Segments[0];
        return new AdjustmentRecipeTargetSpanDto(
            entityRef,
            path.Id,
            segment.SortOrder,
            segment.StartX,
            segment.StartY,
            segment.EndX,
            segment.EndY,
            1);
    }

    private static GeometryPathDto LinePath(decimal startX, decimal startY, decimal endX, decimal endY)
    {
        var id = Guid.NewGuid();
        return new GeometryPathDto(
            id,
            false,
            [new GeometrySegmentDto(id, 1, startX, startY, endX, endY)]);
    }

    private static GeometryPathDto Project(
        GeometryPathDto path,
        decimal scale,
        decimal offsetX,
        decimal offsetY)
        => new(
            path.Id,
            path.IsClosed,
            path.Segments.Select(segment => new GeometrySegmentDto(
                path.Id,
                segment.SortOrder,
                (segment.StartX * scale) + offsetX,
                (segment.StartY * scale) + offsetY,
                (segment.EndX * scale) + offsetX,
                (segment.EndY * scale) + offsetY)).ToArray());

    private static decimal Length(GeometryPathDto path)
        => Math.Abs(path.Segments[0].EndX - path.Segments[0].StartX);

    private static RoomLabelDto RoomLabel(string sourceEntityRef, decimal x, decimal y)
        => new(
            Guid.NewGuid(), sourceEntityRef, "ROOMS", "ROOM", x, y, 0.99m, "kept", 1,
            "MTEXT", 2.125m, 23m, "ARCH", "Center", "Middle", "MiddleCenter", "#FF010203", true,
            x, y, true, 2.125m);

    private static DimensionDto Dimension(string sourceEntityRef)
        => new(
            Guid.NewGuid(), sourceEntityRef, "DIMS", "DIMENSION", "*D1", "10'-4\"", "GeometryBlock", "<> TYP",
            124.125m, 3152.775m, "Inch", 0, 12.5m, 3.25m,
            100m, 200m, 7m,
            224m, 200m, 8m,
            100m, 240m, 9m,
            0.99m, "payload", 4)
        {
            SourceHandle = "A1",
            RenderTextX = 162m,
            RenderTextY = 248m,
            RenderTextHeight = 3.14159265m,
            RenderTextRotationDegrees = 12.5m,
            RenderTextStyleName = "DIMSTYLE",
            LineSegments = [new DimensionLineSegmentDto(100m, 240m, 224m, 240m)],
            LinePrimitives = [new DimensionLinePrimitiveDto("LINE-1", 1, 100m, 240m, 224m, 240m)],
            TextPrimitives = [new DimensionTextPrimitiveDto("TEXT-1", 1, "10'-4\"", 162m, 248m, 3.14159265m, 12.5m)],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto("INSERT-1", 1, "_Dot", 100m, 240m, 7m)
                {
                    RotationDegrees = 33m,
                    ScaleX = 0.123456789m,
                    ScaleY = 0.987654321m
                }
            ],
            CirclePrimitives = [new DimensionCirclePrimitiveDto("CIRCLE-1", 1, 100m, 240m, 1.23456789m)],
            ArcPrimitives = [new DimensionArcPrimitiveDto("ARC-1", 1, 224m, 240m, 2.34567891m, 15m, 75m)],
            SolidPrimitives = [new DimensionSolidPrimitiveDto("SOLID-1", 1, 100m, 239m, 101m, 240m, 100m, 241m, 99m, 240m)],
            IsEdited = true,
            IsDirty = true,
            LastExportedAtUtc = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc)
        };
}
