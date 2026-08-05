namespace Minecraft.Core.Worlds.Storage;

/// <summary>
/// How renaming a saved world went. Reported rather than phrased, since what to tell somebody about it is
/// the menu's business and not the storage layer's.
/// </summary>
public enum WorldRenameResult
{
    Renamed,

    /// <summary>The new name was the one it already had, so nothing was moved.</summary>
    Unchanged,

    /// <summary>Nothing is saved under the old name any more.</summary>
    SourceMissing,

    /// <summary>Another world already answers to the new name.</summary>
    NameTaken,

    /// <summary>The file system refused the move.</summary>
    Failed,
}
