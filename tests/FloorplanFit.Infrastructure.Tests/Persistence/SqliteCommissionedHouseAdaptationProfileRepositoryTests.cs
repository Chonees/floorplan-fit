using System.Text.Json;
using System.Text.Json.Nodes;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Persistence;

public sealed class SqliteCommissionedHouseAdaptationProfileRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_round_trips_the_complete_profile_after_reopening_the_session()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var profile = CreateProfile(Guid.NewGuid(), "original");

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(profile, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);

            var loaded = await reopenedRepository.GetByFloorPlanVersionIdAsync(
                profile.FloorPlanVersionId,
                profile.PublishedCurationId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(profile.PublishedCurationId, loaded.PublishedCurationId);
            Assert.Equivalent(profile, loaded, strict: true);
        });
    }

    [Fact]
    public async Task UpsertAsync_round_trips_distinct_opening_geometry_and_structural_wall_host_evidence()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var profile = CreateProfileWithOpeningHost(Guid.NewGuid(), "host-round-trip");

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(profile, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);
            var loaded = await reopenedRepository.GetByFloorPlanVersionIdAsync(
                profile.FloorPlanVersionId,
                profile.PublishedCurationId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equivalent(profile, loaded, strict: true);
            var opening = Assert.Single(loaded.AuxiliaryEntityBindings);
            Assert.NotNull(opening.GeometryPathId);
            Assert.NotNull(opening.HostGeometryPathId);
            Assert.NotEqual(opening.GeometryPathId, opening.HostGeometryPathId);
            var openingRole = Assert.Single(
                loaded.Variables.SelectMany(variable => variable.ActionTemplates)
                    .SelectMany(action => action.CanonicalEntityRoles),
                role => role.EntityRef == opening.SourceEntityRef);
            Assert.Equal(opening.GeometryPathId, openingRole.GeometryPathId);
            Assert.Equal(opening.HostGeometryPathId, openingRole.HostGeometryPathId);
            Assert.Equal(opening.HostSegmentSortOrder, openingRole.HostSegmentSortOrder);
        });
    }

    [Fact]
    public async Task GetByFloorPlanVersionIdAsync_deserializes_legacy_missing_host_fields_but_readiness_fails_closed()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var profile = CreateProfileWithOpeningHost(Guid.NewGuid(), "legacy-no-host");
            var json = JsonNode.Parse(JsonSerializer.Serialize(profile))!.AsObject();
            var opening = json[nameof(CommissionedHouseAdaptationProfile.AuxiliaryEntityBindings)]!
                .AsArray()[0]!
                .AsObject();
            opening.Remove(nameof(CommissionExistingCurationAuxiliaryEntityBinding.HostGeometryPathId));
            opening.Remove(nameof(CommissionExistingCurationAuxiliaryEntityBinding.HostSegmentSortOrder));
            foreach (var variable in json[nameof(CommissionedHouseAdaptationProfile.Variables)]!.AsArray())
            {
                foreach (var action in variable![nameof(CommissionedAdaptationVariable.ActionTemplates)]!.AsArray())
                {
                    foreach (var role in action![nameof(AdjustmentRecipeStretchActionDto.CanonicalEntityRoles)]!.AsArray())
                    {
                        if (role![nameof(AdjustmentRecipeEntityRoleDto.EntityRef)]!.GetValue<string>() != opening[nameof(CommissionExistingCurationAuxiliaryEntityBinding.SourceEntityRef)]!.GetValue<string>())
                        {
                            continue;
                        }

                        role.AsObject().Remove(nameof(AdjustmentRecipeEntityRoleDto.HostGeometryPathId));
                        role.AsObject().Remove(nameof(AdjustmentRecipeEntityRoleDto.HostSegmentSortOrder));
                    }
                }
            }

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                using var command = session.Connection.CreateCommand();
                command.Transaction = session.Transaction;
                command.CommandText =
                    "INSERT INTO commissioned_house_adaptation_profiles (floorplan_version_id, profile_json) VALUES ($id, $json)";
                command.Parameters.AddWithValue("$id", profile.FloorPlanVersionId.ToString());
                command.Parameters.AddWithValue("$json", json.ToJsonString());
                await command.ExecuteNonQueryAsync();
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository repository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);
            var loaded = await repository.GetByFloorPlanVersionIdAsync(
                profile.FloorPlanVersionId,
                profile.PublishedCurationId,
                CancellationToken.None);

            Assert.NotNull(loaded);
            var readiness = CommissionedHouseAdaptationProfileReadiness.Evaluate(loaded);
            Assert.False(readiness.IsReady);
            Assert.Contains(
                readiness.Reasons,
                reason => reason.Contains("host", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task UpsertAsync_replaces_the_profile_for_the_same_floor_plan_version()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var floorPlanVersionId = Guid.NewGuid();
            var original = CreateProfile(floorPlanVersionId, "original");
            var replacement = CreateProfile(floorPlanVersionId, "replacement");

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(original, CancellationToken.None);
                await repository.UpsertAsync(replacement, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using (var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);
                var loaded = await reopenedRepository.GetByFloorPlanVersionIdAsync(
                    floorPlanVersionId,
                    replacement.PublishedCurationId,
                    CancellationToken.None);

                Assert.NotNull(loaded);
                Assert.Equivalent(replacement, loaded, strict: true);
            }

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM commissioned_house_adaptation_profiles WHERE floorplan_version_id = $id";
            command.Parameters.AddWithValue("$id", floorPlanVersionId.ToString());

            Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        });
    }

    [Fact]
    public async Task RemoveByFloorPlanVersionIdAsync_deletes_only_the_requested_profile_and_missing_lookup_is_null()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var removed = CreateProfile(Guid.NewGuid(), "removed");
            var retained = CreateProfile(Guid.NewGuid(), "retained");

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(removed, CancellationToken.None);
                await repository.UpsertAsync(retained, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.RemoveByFloorPlanVersionIdAsync(
                    removed.FloorPlanVersionId,
                    CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);

            Assert.Null(await reopenedRepository.GetByFloorPlanVersionIdAsync(
                removed.FloorPlanVersionId,
                removed.PublishedCurationId,
                CancellationToken.None));
            Assert.Null(await reopenedRepository.GetByFloorPlanVersionIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CancellationToken.None));

            var loadedRetained = await reopenedRepository.GetByFloorPlanVersionIdAsync(
                retained.FloorPlanVersionId,
                retained.PublishedCurationId,
                CancellationToken.None);
            Assert.NotNull(loadedRetained);
            Assert.Equivalent(retained, loadedRetained, strict: true);
        });
    }

    [Fact]
    public async Task GetByFloorPlanVersionIdAsync_returns_null_when_the_profile_targets_a_stale_published_curation()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var profile = CreateProfile(Guid.NewGuid(), "stale");

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(profile, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);

            var loaded = await reopenedRepository.GetByFloorPlanVersionIdAsync(
                profile.FloorPlanVersionId,
                Guid.NewGuid(),
                CancellationToken.None);

            Assert.Null(loaded);
        });
    }

    [Fact]
    public async Task GetByFloorPlanVersionIdAsync_rejects_an_empty_json_published_curation_identity()
    {
        await WithDatabaseAsync(async databasePath =>
        {
            var profile = CreateProfile(Guid.NewGuid(), "missing-curation") with
            {
                PublishedCurationId = Guid.Empty
            };

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                ICommissionedHouseAdaptationProfileRepository repository =
                    new SqliteCommissionedHouseAdaptationProfileRepository(session);
                await repository.UpsertAsync(profile, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var reopenedSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            ICommissionedHouseAdaptationProfileRepository reopenedRepository =
                new SqliteCommissionedHouseAdaptationProfileRepository(reopenedSession);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                reopenedRepository.GetByFloorPlanVersionIdAsync(
                    profile.FloorPlanVersionId,
                    Guid.NewGuid(),
                    CancellationToken.None));

            Assert.Contains("PublishedCurationId", exception.Message, StringComparison.Ordinal);
        });
    }

    private static async Task WithDatabaseAsync(Func<string, Task> test)
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"floorplan-fit-commissioned-profile-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);
            await test(workspace.DatabasePath);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static CommissionedHouseAdaptationProfile CreateProfile(Guid floorPlanVersionId, string suffix)
        => new(
            FloorPlanVersionId: floorPlanVersionId,
            PublishedCurationId: Guid.NewGuid(),
            SourceToMillimetersFactor: 25.4m,
            Variables:
            [
                CreateVariable($"width-{suffix}", HouseAdaptationAxis.Width, priority: 10),
                CreateVariable($"depth-{suffix}", HouseAdaptationAxis.Depth, priority: 20)
            ],
            ImmutableSizeEntityRefs: [$"OPENING:{suffix}:IMMUTABLE"],
            ProtectedEntityRefs: [$"FIXTURE:{suffix}:PROTECTED"]);

    private static CommissionedHouseAdaptationProfile CreateProfileWithOpeningHost(
        Guid floorPlanVersionId,
        string suffix)
    {
        var openingRef = $"OPENING:{suffix}";
        var openingPathId = Guid.NewGuid();
        var rawAction = CreateAction($"width-{suffix}-action", HouseAdaptationAxis.Width);
        var action = rawAction with
        {
            TargetSpans = rawAction.TargetSpans
                .Select(target => target with { ClosingVertexIndex = 1 })
                .ToArray(),
            CanonicalEntityRoles = rawAction.CanonicalEntityRoles
                .Select(role => role.Role == "Stretch"
                    ? role with { VertexIndices = [1] }
                    : role)
                .ToArray()
        };
        var host = action.CanonicalEntityRoles.First(role => role.Role == "Stretch");
        var openingRole = new AdjustmentRecipeEntityRoleDto(
            openingRef,
            openingPathId,
            SegmentSortOrder: 0,
            Role: "RigidMove",
            VertexIndices: [],
            Reason: "Preserve commissioned opening raw identity")
        {
            HostGeometryPathId = host.GeometryPathId,
            HostSegmentSortOrder = host.SegmentSortOrder
        };
        var variable = new CommissionedAdaptationVariable(
            $"width-{suffix}",
            $"Commissioned Width {suffix}",
            HouseAdaptationAxis.Width,
            Priority: 1,
            ActionTemplates:
            [
                action with
                {
                    CanonicalEntityRoles = action.CanonicalEntityRoles.Append(openingRole).ToArray()
                }
            ]);

        return new CommissionedHouseAdaptationProfile(
            floorPlanVersionId,
            Guid.NewGuid(),
            25.4m,
            [variable],
            [openingRef],
            [])
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    openingRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Opening,
                    new AdjustmentRecipeBoundsDto(10m, 1m, 10m, 2m),
                    openingPathId,
                    SegmentSortOrder: 0,
                    IsImmutableSize: true,
                    IsProtected: false)
                {
                    HostGeometryPathId = host.GeometryPathId,
                    HostSegmentSortOrder = host.SegmentSortOrder
                }
            ]
        };
    }

    private static CommissionedAdaptationVariable CreateVariable(
        string id,
        HouseAdaptationAxis axis,
        int priority)
        => new(
            id,
            Name: $"Commissioned {axis} {id}",
            axis,
            priority,
            ActionTemplates: [CreateAction($"{id}-action", axis)]);

    private static AdjustmentRecipeStretchActionDto CreateAction(string actionId, HouseAdaptationAxis axis)
    {
        var firstEntityRef = $"{actionId}:A";
        var secondEntityRef = $"{actionId}:B";
        var firstPathId = Guid.NewGuid();
        var secondPathId = Guid.NewGuid();

        return new AdjustmentRecipeStretchActionDto(
            actionId,
            AxisTag: axis == HouseAdaptationAxis.Width ? "Width" : "Height",
            Edge: axis == HouseAdaptationAxis.Width ? "Right" : "Top",
            CutCoordinate: 10.25m,
            DeltaSourceUnits: 0m,
            MaxDeltaSourceUnits: 6.5m,
            CoordinateTolerance: 0.001m,
            CanonicalSourceBounds: new AdjustmentRecipeBoundsDto(1m, 2m, 101m, 82m),
            TargetSpans:
            [
                new AdjustmentRecipeTargetSpanDto(firstEntityRef, firstPathId, 3, 1m, 2m, 11m, 2m, 4),
                new AdjustmentRecipeTargetSpanDto(secondEntityRef, secondPathId, 7, 1m, 6m, 11m, 6m, 8)
            ],
            CanonicalEntityRoles:
            [
                new AdjustmentRecipeEntityRoleDto(firstEntityRef, firstPathId, 3, "Stretch", [4, 5]),
                new AdjustmentRecipeEntityRoleDto(secondEntityRef, secondPathId, 7, "Stretch", [8, 9]),
                new AdjustmentRecipeEntityRoleDto(
                    $"{actionId}:OPENING",
                    null,
                    null,
                    "RigidMove",
                    [],
                    "Preserve commissioned opening")
            ]);
    }
}
