using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class EntityTest : GameTest
    {
        [SidedDependency(Side.Server)] private readonly IMapManager _mapManager = default!;
        [SidedDependency(Side.Server)] private readonly MapSystem _mapSys = default!;

        public override PoolSettings PoolSettings => new()
        {
            Connected = true,
            Dirty = true
        };

        [Test]
        [Description("Spawns every distinct entity in the game on its own map, and ensures it survives networking to the client.")]
        public async Task SpawnAndDirtyAllEntities()
        {

            var protoIds = SProtoMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !Pair.IsTestPrototype(p))
                .Where(p => !p.Components.ContainsKey("MapGrid")) // This will smash stuff otherwise.
                .Select(p => p.ID)
                .ToList();

            await Server.WaitPost(() =>
            {
                foreach (var protoId in protoIds)
                {
                    _mapSys.CreateMap(out var mapId);
                    var grid = _mapManager.CreateGridEntity(mapId);
                    _ = SEntMan.SpawnEntity(protoId, new EntityCoordinates(grid.Owner, 0.5f, 0.5f));
                }   // Just spawning them is enough, they'll be dirty already.
            });

            await RunUntilSynced();

            // Make sure the client actually received the entities
            // 500 is completely arbitrary. Note that the client & sever entity counts aren't expected to match.
            Assert.That(CEntMan.EntityCount, Is.GreaterThan(500));

            await Server.WaitPost(() =>
            {
                static IEnumerable<(EntityUid, TComp)> Query<TComp>(IEntityManager entityMan)
                    where TComp : Component
                {
                    var query = entityMan.AllEntityQueryEnumerator<TComp>();
                    while (query.MoveNext(out var uid, out var meta))
                    {
                        yield return (uid, meta);
                    }
                }

                var entityMetas = Query<MetaDataComponent>(SEntMan).ToList();
                foreach (var (uid, meta) in entityMetas)
                {
                    if (!meta.EntityDeleted)
                        SEntMan.DeleteEntity(uid);
                }

                Assert.That(SEntMan.EntityCount, Is.Zero);
            });
        }
    }
}
