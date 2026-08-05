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

    /// <summary>
    /// A sheep's back is eighteen units up, twelve of leg with a six deep body resting on them, and the head
    /// carried at the front reaches another four above that.
    /// </summary>
    private const float SheepHeightInUnits = 22;

    /// <summary>
    /// A pig is the squattest of the animals: six units of leg under an eight deep body, with the head held
    /// two units clear of the back so it reads as a head rather than as more of the same block.
    /// </summary>
    private const float PigHeightInUnits = 16;

    /// <summary>
    /// A cow's back is twenty two units up, twelve of leg with a ten deep body on them. The head is carried
    /// above the shoulder and the horns reach another unit past the top of it.
    /// </summary>
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

    /// <summary>How far a sheep's fleece stands out from the body it grows on, in model units.</summary>
    private const float SheepFleeceThickness = 1.75F;

    /// <summary>
    /// A sheep: a long body carried on four legs, with the head off the front of it. The body is the one part
    /// drawn lying down, since its net is unwrapped as though it were standing on end.
    /// <para>
    /// The head is set high enough to meet the shoulder rather than hang below it, and a leg stands under each
    /// corner of the sixteen long body, so the pairs sit a full body apart instead of bunched under its middle.
    /// </para>
    /// </summary>
    private static SkinBox[] BuildSheep() =>
    [
        // Head, four units of it above the back and the rest set into the front of the fleece.
        new(new Vector2i(0, 0), new Vector3i(6, 6, 8), new Vector3(-3, 16, 6)),
        // The body carries its own fleece rather than a second layer over it, since the sheet holds only the
        // one woolly body to draw with. Grown all round, it hangs over the tops of the legs.
        new(
            new Vector2i(28, 8),
            new Vector3i(8, 16, 6),
            new Vector3(-4, 12, -8),
            SkinBoxPose.Lying,
            SheepFleeceThickness),
        // The two front legs, then the two back ones.
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(1, 0, -9)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-5, 0, -9)),
    ];

    /// <summary>
    /// A pig: a body slung low on four short legs, with the head out in front of it and the snout off the
    /// front of that. Like the sheep, the body is the one part drawn lying down.
    /// </summary>
    private static SkinBox[] BuildPig() =>
    [
        // Head, carried two units above the back and dropping two below it, so the shoulder has a step in it
        // instead of head and body running together into one long block.
        new(new Vector2i(0, 0), new Vector3i(8, 8, 8), new Vector3(-4, 8, 6)),
        // The snout, set into the lower half of the face and standing a single unit proud of it.
        new(new Vector2i(16, 16), new Vector3i(4, 3, 1), new Vector3(-2, 9, 14)),
        new(new Vector2i(28, 8), new Vector3i(10, 16, 8), new Vector3(-5, 6, -8), SkinBoxPose.Lying),
        // The two front legs, then the two back ones, one under each corner of the sixteen long body.
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(1, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(-5, 0, 3)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(1, 0, -9)),
        new(new Vector2i(0, 16), new Vector3i(4, 6, 4), new Vector3(-5, 0, -9)),
    ];

    /// <summary>
    /// A cow: the largest of the animals, an eighteen long body on tall legs with a horned head carried off
    /// the front of it, and the udder hung under the back half of the belly.
    /// </summary>
    private static SkinBox[] BuildCow() =>
    [
        // Head, carried above the shoulder rather than level with it, so the neck has a step in it.
        new(new Vector2i(0, 0), new Vector3i(8, 8, 6), new Vector3(-4, 16, 9)),
        // A horn on each side of the crown, drawn from the one horn the sheet carries.
        new(new Vector2i(22, 0), new Vector3i(1, 3, 1), new Vector3(4, 22, 12)),
        new(new Vector2i(22, 0), new Vector3i(1, 3, 1), new Vector3(-5, 22, 12)),
        new(new Vector2i(18, 4), new Vector3i(12, 18, 10), new Vector3(-6, 12, -9), SkinBoxPose.Lying),
        // The udder, hung under the back of the belly. Lying with the body it belongs to, so its net is
        // unwrapped the same way round as the body's is.
        new(new Vector2i(52, 0), new Vector3i(4, 6, 1), new Vector3(-2, 11, -9), SkinBoxPose.Lying),
        // The two front legs, then the two back ones.
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(2, 0, 5)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-6, 0, 5)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(2, 0, -8)),
        new(new Vector2i(0, 16), new Vector3i(4, 12, 4), new Vector3(-6, 0, -8)),
    ];
}
