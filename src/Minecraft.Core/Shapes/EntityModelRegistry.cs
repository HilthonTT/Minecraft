using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

namespace Minecraft.Core.Shapes;

[SupportedOSPlatform("windows")]
public sealed class EntityModelRegistry
{
    private const float HumanoidHeightInUnits = 32;

    private const float SheepHeightInUnits = 22;

    private const float PigHeightInUnits = 16;

    private const float CowHeightInUnits = 25;

    public ReadOnlyDictionary<EntityType, EntityModel> Models { get; }

    public EntityModelRegistry()
    {
        var playerSkin = new Texture(Assets.Path("Resources/steve.png"), 64, 32);
        var zombieSkin = new Texture(Assets.Path("Resources/zombie.png"), 64, 64);
        var sheepSkin = new Texture(Assets.Path("Resources/sheep.png"), 64, 32);
        var pigSkin = new Texture(Assets.Path("Resources/pig.png"), 64, 32);
        var cowSkin = new Texture(Assets.Path("Resources/cow.png"), 64, 32);

        var player = new SkinnedEntityModel(
            playerSkin,
            BuildHumanoid(),
            HumanoidHeightInUnits,
            new Vector3(Constants.PLAYER_WIDTH, Constants.PLAYER_HEIGHT, Constants.PLAYER_LENGTH));

        Models = new ReadOnlyDictionary<EntityType, EntityModel>(new Dictionary<EntityType, EntityModel>
        {
            { EntityType.Player, player },
            {
                EntityType.Dummy,
                new SkinnedEntityModel(playerSkin, BuildHumanoid(), HumanoidHeightInUnits, new Vector3(0.5F, 2.0F, 0.5F))
            },
            {
                EntityType.Zombie,
                new SkinnedEntityModel(
                    zombieSkin,
                    BuildHumanoid(),
                    HumanoidHeightInUnits,
                    new Vector3(Zombie.BodyWidth, Zombie.BodyHeight, Zombie.BodyLength))
            },
            {
                EntityType.Sheep,
                new SkinnedEntityModel(
                    sheepSkin,
                    BuildSheep(),
                    SheepHeightInUnits,
                    new Vector3(Sheep.BodyWidth, Sheep.BodyHeight, Sheep.BodyLength))
            },
            {
                EntityType.Pig,
                new SkinnedEntityModel(
                    pigSkin,
                    BuildPig(),
                    PigHeightInUnits,
                    new Vector3(Pig.BodyWidth, Pig.BodyHeight, Pig.BodyLength))
            },
            {
                EntityType.Cow,
                new SkinnedEntityModel(
                    cowSkin,
                    BuildCow(),
                    CowHeightInUnits,
                    new Vector3(Cow.BodyWidth, Cow.BodyHeight, Cow.BodyLength))
            },
        });
    }

    private static SkinBox[] BuildHumanoid() =>
    [
        new(new Vector2i(0, 0), new Vector3i(8, 8, 8), new Vector3(-4, 24, -4)),
        new(new Vector2i(16, 16), new Vector3i(8, 12, 4), new Vector3(-4, 12, -2)),
        new(new Vector2i(40, 16), new Vector3i(4, 12, 4), new Vector3(4, 12, -2)),
        new(new Vector2i(40, 16), new Vector3i(4, 12, 4), new Vector3(-8, 12, -2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(0, 0, -2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-4, 0, -2)),
    ];

    private const float SheepFleeceThickness = 1.75F;

    private static SkinBox[] BuildSheep() =>
    [
        new(new Vector2i(0, 0), new Vector3i(6, 6, 8), new Vector3(-3, 16, 6)),
        new(
            new Vector2i(28, 8),
            new Vector3i(8, 16, 6),
            new Vector3(-4, 12, -8),
            SkinBoxPose.Lying,
            SheepFleeceThickness),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, -9)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, -9)),
    ];

    private static SkinBox[] BuildPig() =>
    [
        new(new Vector2i(0, 0), new Vector3i(8, 8, 8), new Vector3(-4, 8, 6)),
        new(new Vector2i(16, 16), new Vector3i(4, 3, 1), new Vector3(-2, 9, 14)),
        new(new Vector2i(28, 8), new Vector3i(10, 16, 8), new Vector3(-5, 6, -8), SkinBoxPose.Lying),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(1, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(-5, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(1, 0, -9)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(-5, 0, -9)),
    ];

    private static SkinBox[] BuildCow() =>
    [
        new(new Vector2i(0, 0), new Vector3i(8, 8, 6), new Vector3(-4, 16, 9)),
        new(new Vector2i(22, 0), new Vector3i(1, 3, 1), new Vector3(4, 22, 12)),
        new(new Vector2i(22, 0), new Vector3i(1, 3, 1), new Vector3(-5, 22, 12)),
        new(new Vector2i(18, 4), new Vector3i(12, 18, 10), new Vector3(-6, 12, -9), SkinBoxPose.Lying),
        new(new Vector2i(52, 0), new Vector3i(4, 6, 1), new Vector3(-2, 11, -9), SkinBoxPose.Lying),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(2, 0, 5)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-6, 0, 5)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(2, 0, -8)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-6, 0, -8)),
    ];
}
