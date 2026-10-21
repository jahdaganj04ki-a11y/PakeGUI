"""Erzeugt docs/theme-palette.png aus den echten Farbwerten in Theme.cs.

Aufruf:  python3 tools/make-theme-preview.py
"""
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
THEME = ROOT / "src" / "PakeGui.Core" / "GitHubDarkPalette.cs"
OUT = ROOT / "docs" / "theme-palette.png"
FONT = "/opt/freestyle/python/lib/python3.12/site-packages/matplotlib/mpl-data/fonts/ttf/DejaVuSans.ttf"
FONT_BOLD = "/opt/freestyle/python/lib/python3.12/site-packages/matplotlib/mpl-data/fonts/ttf/DejaVuSans-Bold.ttf"

WIDTH, ROW, PAD = 900, 46, 18


def parse_colors() -> list[tuple[str, str]]:
    text = THEME.read_text(encoding="utf-8")
    pattern = re.compile(r"public const string (\w+)Hex = \"(#[0-9a-fA-F]{6})\"")
    return [(m.group(1), m.group(2).lower()) for m in pattern.finditer(text)]


def luminance(hex_color: str) -> float:
    def channel(value: int) -> float:
        c = value / 255
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4

    r, g, b = (int(hex_color[i : i + 2], 16) for i in (1, 3, 5))
    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)


def contrast(a: str, b: str) -> float:
    la, lb = luminance(a), luminance(b)
    high, low = max(la, lb), min(la, lb)
    return (high + 0.05) / (low + 0.05)


def main() -> None:
    colors = parse_colors()
    if not colors:
        raise SystemExit("Keine Farben in Theme.cs gefunden")

    height = PAD * 2 + 60 + ROW * len(colors)
    img = Image.new("RGB", (WIDTH, height), (13, 17, 23))
    draw = ImageDraw.Draw(img)

    title = ImageFont.truetype(FONT_BOLD, 22)
    subtitle = ImageFont.truetype(FONT, 13)
    name_font = ImageFont.truetype(FONT_BOLD, 14)
    hex_font = ImageFont.truetype(FONT, 14)

    draw.text((PAD, PAD), "GitHub Dark Default – Palette der Pake GUI", font=title, fill=(240, 246, 252))
    draw.text(
        (PAD, PAD + 30),
        "Werte aus @primer/primitives (dark default), 1:1 in Theme.cs übernommen",
        font=subtitle,
        fill=(145, 152, 161),
    )

    y = PAD + 62
    for name, hex_color in colors:
        rgb = tuple(int(hex_color[i : i + 2], 16) for i in (1, 3, 5))
        draw.rectangle([PAD, y, PAD + 140, y + ROW - 10], fill=rgb, outline=(61, 68, 77))
        draw.text((PAD + 160, y + 6), name, font=name_font, fill=(240, 246, 252))
        draw.text((PAD + 400, y + 6), hex_color.upper(), font=hex_font, fill=(145, 152, 161))
        draw.text(
            (PAD + 520, y + 6),
            f"Kontrast zu Canvas {contrast(hex_color, '#0d1117'):.1f}:1",
            font=hex_font,
            fill=(145, 152, 161),
        )
        y += ROW

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(f"{OUT} geschrieben ({len(colors)} Farben)")
    print(f"Kontrast Text/Canvas: {contrast('#f0f6fc', '#0d1117'):.1f}:1")
    print(f"Kontrast Muted/Canvas: {contrast('#9198a1', '#0d1117'):.1f}:1")
    print(f"Kontrast Weiss/Primary: {contrast('#ffffff', '#238636'):.1f}:1")


if __name__ == "__main__":
    main()
