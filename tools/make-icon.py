"""Erzeugt src/PakeGuiWin/assets/app.ico (Pake-GUI-Icon).

Aufruf:  python3 tools/make-icon.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SIZE = 1024
OUT = Path(__file__).resolve().parent.parent / "src" / "PakeGuiWin" / "assets" / "app.ico"
FONT = "/opt/freestyle/python/lib/python3.12/site-packages/matplotlib/mpl-data/fonts/ttf/DejaVuSans-Bold.ttf"


def rounded_gradient(size: int) -> Image.Image:
    top = (30, 58, 138)
    bottom = (59, 130, 246)
    base = Image.new("RGB", (size, size), bottom)
    draw = ImageDraw.Draw(base)
    for y in range(size):
        t = y / (size - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (size, y)], fill=color)

    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, size - 1, size - 1], radius=int(size * 0.22), fill=255
    )
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    img.paste(base, (0, 0), mask)

    # feine helle Kante oben
    shine = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(shine).rounded_rectangle(
        [int(size * 0.035), int(size * 0.035), size - int(size * 0.035), size - int(size * 0.035)],
        radius=int(size * 0.19),
        outline=(255, 255, 255, 46),
        width=max(2, int(size * 0.012)),
    )
    return Image.alpha_composite(img, shine)


def add_letter(img: Image.Image) -> Image.Image:
    """Weisses "P" mit dezentem Schatten."""
    font = ImageFont.truetype(FONT, int(SIZE * 0.66))
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    box = draw.textbbox((0, 0), "P", font=font)
    x = (SIZE - (box[2] - box[0])) / 2 - box[0]
    y = (SIZE - (box[3] - box[1])) / 2 - box[1] - SIZE * 0.02

    draw.text((x + SIZE * 0.012, y + SIZE * 0.012), "P", font=font, fill=(6, 20, 51, 70))
    draw.text((x, y), "P", font=font, fill=(255, 255, 255, 255))
    return Image.alpha_composite(img, layer)


def main() -> None:
    img = add_letter(rounded_gradient(SIZE))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    img.save(OUT, format="ICO", sizes=sizes)
    print(f"{OUT} geschrieben ({OUT.stat().st_size} Bytes, {len(sizes)} Groessen)")


if __name__ == "__main__":
    main()
