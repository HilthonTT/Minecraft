using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using System.Collections.ObjectModel;
using System.Runtime.Versioning;

namespace Minecraft.Core.Shapes;

/// <summary>
/// Builds the model each kind of entity is drawn with, out of the skin sheets in the resources folder.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class EntityModelRegistry
{
    /// <summary>A humanoid stands thirty two units tall, split eight head, twelve body and twelve leg.</summary>
    private const float HumanoidHeightInUnits = 32;

    /// <summary>A sheep's back is eighteen units up: twelve of leg with a six deep body resting on them.</summary>
    private const float SheepHeightInUnits = 18;

    public ReadOnlyDictionary<EntityType, EntityModel> Models { get; }

    public EntityModelRegistry()
    {
        var playerSkin = new Texture(Assets.Path("Resources/steve.png"), 64, 32);
        var zombieSkin = new Texture(Assets.Path("Resources/zombie.png"), 64, 64);
        var sheepSkin = new Texture(Assets.Path("Resources/sheep.png"), 64, 32);

        var player = new SkinnedEntityModel(
            playerSkin,
            BuildHumanoid(),
            HumanoidHeightInUnits,
            new Vector3(Constants.PLAYER_WIDTH, Constants.PLAYER_HEIGHT, Constants.PLAYER_LENGTH));

        Models = new ReadOnlyDictionary<EntityType, EntityModel>(new Dictionary<EntityType, EntityModel>
        {
            { EntityType.Player, player },
            {
                // The dummy has no artwork of its own, so it borrows the player's and is simply built taller.
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
        });
    }

    /// <summary>
    /// The player shape every humanoid skin is drawn on. Both arms and both legs are cut from the same part of
    /// the sheet, which is all a sheet in the original thirty two row layout carries.
    /// </summary>
    private static SkinBox[] BuildHumanoid() =>
    [
        new(new Vector2i(0, 0), new Vector3i(8, 8, 8), new Vector3(-4, 24, -4)),
        new(new Vector2i(16, 16), new Vector3i(8, 12, 4), new Vector3(-4, 12, -2)),
        new(new Vector2i(40, 16), new Vector3i(4, 12, 4), new Vector3(4, 12, -2)),
        new(new Vector2i(40, 16), new Vector3i(4, 12, 4), new Vector3(-8, 12, -2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(0, 0, -2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-4, 0, -2)),
    ];

    /// <summary>
    /// A sheep: a long body carried on four legs, with the head hanging off the front of it. The body is the
    /// one part drawn lying down, since its net is unwrapped as though it were standing on end.
    /// </summary>
    private static SkinBox[] BuildSheep() =>
    [
        new(new Vector2i(0, 0), new Vector3i(6, 6, 8), new Vector3(-3, 9, 5)),
        new(new Vector2i(28, 8), new Vector3i(8, 16, 6), new Vector3(-4, 12, -8), SkinBoxPose.Lying),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, 2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, 2)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, -6)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, -6)),
    ];
}
