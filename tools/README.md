# Tools

Programs used to author assets. Nothing here is built, referenced or shipped — the game only ever reads what
these produce, which is checked in under `src/Minecraft.Core/Resources/`.

| Tool                 | Produces                                                                    |
| -------------------- | --------------------------------------------------------------------------- |
| `runnable-hiero.jar` | `Resources/arial.fnt` and `Resources/arial.png`, the bitmap font the UI draws |

## Hiero

[Hiero](https://libgdx.com/wiki/tools/hiero) rasterises a system font into a texture atlas and the `.fnt` file
that says where each glyph landed in it. Needs a Java runtime:

```sh
java -jar tools/runnable-hiero.jar
```

`FontRegistry` loads the pair at a fixed 512 by 512, so export at that size or the glyph coordinates in the
`.fnt` will not line up with the atlas beside it.
