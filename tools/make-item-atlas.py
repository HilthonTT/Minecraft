"""Draws Resources/items.png, the sheet everything that is not a block is shown as.

The block sheet is photographs of surfaces and had to be drawn by hand. This one is twenty tools that are
four shapes in five colours, and six odds and ends, so it is generated: the shapes are written down once
below and stamped out in each material's palette. Re-run it after editing a shape or a palette.

    python tools/make-item-atlas.py

The output is committed, so a build never depends on this having been run. Keep the cell coordinates in step
with Shapes/ItemAtlas.cs, which is what the game reads them back out by.

Requires Pillow.
"""

import os
from PIL import Image

# One cell is drawn at 16x16 and scaled up to the size the block sheet uses, so an item's pixels are the
# same size on screen as a block's texels rather than half of them.
SPRITE = 16
CELLS_PER_ROW = 16
SCALE = 2
CELL = SPRITE * SCALE

CLEAR = (0, 0, 0, 0)

# light, mid, dark. The three shades every shape is drawn in.
MATERIALS = [
    ("wood",    (168, 124, 72),  (140, 100, 58),  (105, 73, 42)),
    ("stone",   (176, 176, 176), (136, 136, 136), (96, 96, 96)),
    ("iron",    (226, 226, 226), (188, 188, 188), (138, 138, 138)),
    ("gold",    (252, 222, 80),  (231, 188, 40),  (176, 138, 24)),
    ("diamond", (122, 238, 228), (78, 208, 204),  (44, 158, 158)),
]

HANDLE_LIGHT = (150, 108, 64)
HANDLE_DARK = (108, 76, 44)


class Sprite:
    """A 16x16 square of pixels, with the origin at the top left."""

    def __init__(self):
        self.pixels = {}

    def put(self, x, y, color):
        if 0 <= x < SPRITE and 0 <= y < SPRITE:
            self.pixels[(x, y)] = color

    def rect(self, x0, y0, x1, y1, color):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, color)

    def diagonal(self, x, y, steps, color, dx=1, dy=-1):
        """A run of pixels stepping one across and one up, which every tool is built out of."""
        for i in range(steps):
            self.put(x + i * dx, y + i * dy, color)

    def to_image(self):
        image = Image.new("RGBA", (SPRITE, SPRITE), CLEAR)
        for (x, y), color in self.pixels.items():
            image.putpixel((x, y), color + (255,))
        return image.resize((CELL, CELL), Image.NEAREST)


def handle(sprite, steps=9, x=2, y=13):
    """The shaft, running from the bottom left corner up towards the middle. Shared by all four tools."""
    sprite.diagonal(x, y, steps, HANDLE_DARK, dx=1, dy=-1)
    sprite.diagonal(x + 1, y, steps, HANDLE_LIGHT, dx=1, dy=-1)


def pickaxe(light, mid, dark):
    s = Sprite()
    # A bar across the top with both ends turned down, which is the shape that reads as a pick at this size.
    s.rect(6, 2, 13, 2, light)
    s.rect(5, 3, 14, 3, mid)
    s.put(5, 4, dark)
    s.put(14, 4, dark)
    s.put(4, 4, mid)
    s.put(15, 4, mid)
    s.put(9, 4, mid)
    s.put(10, 4, mid)
    handle(s)
    return s


def axe(light, mid, dark):
    s = Sprite()
    # A blade hung off one side of the shaft: a straight cutting edge down the left, tapering back in to the
    # haft on the right. Lopsided on purpose, which is the whole of what tells it apart from the shovel.
    s.rect(9, 1, 11, 1, light)
    s.rect(7, 2, 11, 2, light)
    s.rect(6, 3, 11, 3, light)
    s.rect(6, 4, 11, 4, mid)
    s.rect(7, 5, 10, 5, mid)
    s.rect(8, 6, 10, 6, dark)
    s.put(6, 5, dark)
    s.put(7, 6, dark)
    s.put(12, 2, dark)
    s.put(12, 3, dark)
    handle(s)
    return s


def shovel(light, mid, dark):
    s = Sprite()
    # A spade sitting square on the end of the shaft: narrow, upright and symmetric about it, where the axe
    # is wide and hangs off one side.
    s.rect(9, 1, 11, 1, light)
    s.rect(9, 2, 11, 2, light)
    s.rect(9, 3, 11, 3, mid)
    s.rect(10, 4, 11, 4, mid)
    s.put(8, 2, dark)
    s.put(8, 3, dark)
    s.put(12, 1, dark)
    s.put(12, 2, dark)
    s.put(12, 3, dark)
    s.put(9, 4, dark)
    s.put(11, 5, dark)
    handle(s)
    return s


def sword(light, mid, dark):
    s = Sprite()
    # The one tool whose shaft is not most of its length: a short grip, a guard laid across it, and a blade
    # running most of the way to the far corner.
    s.diagonal(7, 10, 7, mid)
    s.diagonal(7, 9, 7, light)
    s.put(14, 1, light)
    s.put(13, 1, light)
    s.put(14, 2, mid)

    # The guard crosses the blade rather than running along it, so it is drawn on the other diagonal.
    s.diagonal(4, 9, 5, dark, dx=1, dy=1)

    s.diagonal(4, 12, 3, HANDLE_DARK)
    s.diagonal(4, 11, 3, HANDLE_LIGHT)
    s.put(3, 13, HANDLE_DARK)
    return s


TOOLS = [
    ("pickaxe", 0, pickaxe),
    ("axe", 1, axe),
    ("shovel", 2, shovel),
    ("sword", 3, sword),
]


def stick():
    s = Sprite()
    s.diagonal(4, 12, 9, HANDLE_DARK)
    s.diagonal(5, 12, 9, HANDLE_LIGHT)
    return s


def lump(light, mid, dark):
    """An irregular nugget, which is what coal and redstone both come out of the rock as."""
    s = Sprite()
    s.rect(6, 5, 10, 5, mid)
    s.rect(5, 6, 11, 9, mid)
    s.rect(6, 10, 10, 10, mid)
    s.rect(6, 6, 8, 7, light)
    s.rect(9, 9, 10, 9, dark)
    s.rect(6, 10, 9, 10, dark)
    s.put(5, 9, dark)
    s.put(11, 6, dark)
    return s


def ingot(light, mid, dark):
    """A bar with its top face turned towards the viewer, which is what separates it from a lump."""
    s = Sprite()
    s.rect(5, 6, 10, 6, light)
    s.rect(4, 7, 11, 8, light)
    s.rect(4, 9, 11, 10, mid)
    s.rect(4, 11, 11, 11, dark)
    s.rect(4, 7, 4, 7, mid)
    s.rect(11, 7, 11, 7, mid)
    return s


def gem(light, mid, dark):
    """A cut stone: a table across the top and facets falling away from it."""
    s = Sprite()
    s.rect(5, 5, 10, 5, light)
    s.rect(4, 6, 11, 8, mid)
    s.rect(5, 9, 10, 9, mid)
    s.rect(6, 10, 9, 10, dark)
    s.rect(7, 11, 8, 11, dark)
    s.rect(5, 6, 7, 7, light)
    s.rect(10, 8, 11, 8, dark)
    return s


def dust(light, mid, dark):
    """Scattered specks rather than a solid body, since redstone comes up as powder."""
    s = Sprite()
    for x, y in [(5, 5), (9, 4), (6, 8), (10, 7), (4, 10), (8, 10), (11, 10), (7, 12), (10, 12)]:
        s.put(x, y, light)
        s.put(x + 1, y, mid)
        s.put(x, y + 1, dark)
    return s


def build():
    sheet = Image.new("RGBA", (CELLS_PER_ROW * CELL, CELLS_PER_ROW * CELL), CLEAR)

    def place(sprite, column, row):
        sheet.paste(sprite.to_image(), (column * CELL, row * CELL))

    for _, row, draw in TOOLS:
        for column, (_, light, mid, dark) in enumerate(MATERIALS):
            place(draw(light, mid, dark), column, row)

    # The loose materials, in the order ItemAtlas.cs names them.
    place(stick(), 0, 4)
    place(lump((60, 60, 60), (38, 38, 38), (20, 20, 20)), 1, 4)
    place(ingot(*MATERIALS[2][1:]), 2, 4)
    place(ingot(*MATERIALS[3][1:]), 3, 4)
    place(gem(*MATERIALS[4][1:]), 4, 4)
    place(dust((255, 96, 96), (208, 40, 40), (140, 16, 16)), 5, 4)

    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.join(here, "..", "src", "Minecraft.Core", "Resources", "items.png")
    sheet.save(os.path.normpath(out))
    print("wrote", os.path.normpath(out), sheet.size)


if __name__ == "__main__":
    build()
