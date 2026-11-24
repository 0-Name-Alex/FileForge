import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

def create_refined_app_icon(output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    size = 1024  # Render at 1024x1024 for supersampling
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    # 1. Base Tile: Refined, matte dark squircle (Windows 11 Fluent style)
    pad = 72
    corner_r = 210
    squircle_box = [pad, pad, size - pad, size - pad]

    # Subtle elegant background gradient: Deep Obsidian Slate (#181A24 -> #12131A)
    bg_gradient = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg_gradient)
    for y in range(pad, size - pad):
        factor = (y - pad) / (size - 2 * pad)
        # Smooth dark neutral gradient
        r = int(28 * (1 - factor) + 16 * factor)
        g = int(30 * (1 - factor) + 18 * factor)
        b = int(40 * (1 - factor) + 26 * factor)
        bg_draw.line([(pad, y), (size - pad, y)], fill=(r, g, b, 255))

    # Squircle mask
    tile_mask = Image.new("L", (size, size), 0)
    tile_mask_draw = ImageDraw.Draw(tile_mask)
    tile_mask_draw.rounded_rectangle(squircle_box, radius=corner_r, fill=255)
    img.paste(bg_gradient, (0, 0), tile_mask)

    # Subtle inner bevel stroke (1px equivalent after downsample)
    stroke_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    stroke_draw = ImageDraw.Draw(stroke_layer)
    stroke_draw.rounded_rectangle(squircle_box, radius=corner_r, outline=(255, 255, 255, 28), width=3)
    img = Image.alpha_composite(img, stroke_layer)

    # 2. Central Symbolism: Two sleek overlapping Fluent document cards with depth
    cx, cy = size // 2, size // 2

    # CARD 1 (Back Card - Source file, slightly offset to top-left)
    # Dark translucent slate card with subtle blue rim
    card_w, card_h = 280, 360
    b_x1, b_y1 = cx - 180, cy - 210
    b_x2, b_y2 = b_x1 + card_w, b_y1 + card_h
    b_fold = 70
    b_cr = 20

    back_card = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bc_draw = ImageDraw.Draw(back_card)

    # Drop shadow under back card
    shadow_b = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sb_draw = ImageDraw.Draw(shadow_b)
    sb_draw.rounded_rectangle([b_x1 + 6, b_y1 + 14, b_x2 + 6, b_y2 + 14], radius=b_cr, fill=(0, 0, 0, 110))
    shadow_b = shadow_b.filter(ImageFilter.GaussianBlur(18))
    img = Image.alpha_composite(img, shadow_b)

    # Back card body: Dark slate with subtle gradient
    bc_draw.rounded_rectangle([b_x1, b_y1, b_x2, b_y2], radius=b_cr, fill=(35, 42, 56, 255), outline=(71, 85, 105, 180), width=4)

    # Back card subtle placeholder lines
    for ly in [b_y1 + 70, b_y1 + 115, b_y1 + 160]:
        bc_draw.rounded_rectangle([b_x1 + 36, ly, b_x1 + 160, ly + 10], radius=5, fill=(71, 85, 105, 120))

    img = Image.alpha_composite(img, back_card)

    # CARD 2 (Front Card - Primary Target File in Windows Fluent Blue)
    f_x1, f_y1 = cx - 100, cy - 150
    f_x2, f_y2 = f_x1 + card_w, f_y1 + card_h
    f_fold = 76
    f_cr = 22

    # Drop shadow under front card (giving realistic Fluent elevation)
    shadow_f = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sf_draw = ImageDraw.Draw(shadow_f)
    sf_draw.rounded_rectangle([f_x1 + 8, f_y1 + 18, f_x2 + 8, f_y2 + 18], radius=f_cr, fill=(0, 0, 0, 160))
    shadow_f = shadow_f.filter(ImageFilter.GaussianBlur(24))
    img = Image.alpha_composite(img, shadow_f)

    front_card = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fc_draw = ImageDraw.Draw(front_card)

    # Front card points with folded top-right corner
    # Poly points: (f_x1, f_y1) -> (f_x2 - f_fold, f_y1) -> (f_x2, f_y1 + f_fold) -> (f_x2, f_y2) -> (f_x1, f_y2)
    # To have rounded bottom corners and smooth edges:
    fc_draw.rounded_rectangle([f_x1, f_y1, f_x2, f_y2], radius=f_cr, fill=(0, 120, 212, 255))

    # Gradient overlay on front card: Top-left #0284C7 to bottom-right #005A9E
    f_grad = Image.new("RGBA", (card_w, card_h), (0, 0, 0, 0))
    fg_draw = ImageDraw.Draw(f_grad)
    for y in range(card_h):
        fac = y / card_h
        r = int(2 * (1 - fac) + 0 * fac)
        g = int(132 * (1 - fac) + 90 * fac)
        b = int(220 * (1 - fac) + 160 * fac)
        fg_draw.line([(0, y), (card_w, y)], fill=(r, g, b, 255))

    # Mask for front card
    f_mask = Image.new("L", (card_w, card_h), 0)
    fm_draw = ImageDraw.Draw(f_mask)
    fm_draw.rounded_rectangle([0, 0, card_w, card_h], radius=f_cr, fill=255)
    front_card.paste(f_grad, (f_x1, f_y1), f_mask)

    # Folded corner flap in top-right
    # A triangular notch cut from corner, replaced with subtle lighter blue flap
    flap_mask = Image.new("L", (size, size), 0)
    flm_draw = ImageDraw.Draw(flap_mask)
    flm_draw.polygon([(f_x2 - f_fold, f_y1), (f_x2, f_y1), (f_x2, f_y1 + f_fold)], fill=255)

    # Darker background under flap
    fc_draw.polygon([(f_x2 - f_fold, f_y1), (f_x2, f_y1), (f_x2, f_y1 + f_fold)], fill=(0, 90, 158, 255))

    # Folded flap itself (lighter tone #38BDF8)
    flap_poly = [(f_x2 - f_fold, f_y1), (f_x2 - f_fold, f_y1 + f_fold), (f_x2, f_y1 + f_fold)]
    fc_draw.polygon(flap_poly, fill=(56, 189, 248, 255))

    # Subtle shadow under the folded flap
    flap_shadow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fs_draw = ImageDraw.Draw(flap_shadow)
    fs_draw.line([(f_x2 - f_fold, f_y1 + f_fold + 2), (f_x2, f_y1 + f_fold + 2)], fill=(0, 0, 0, 90), width=6)
    fs_draw.line([(f_x2 - f_fold - 2, f_y1), (f_x2 - f_fold - 2, f_y1 + f_fold)], fill=(0, 0, 0, 90), width=6)
    flap_shadow = flap_shadow.filter(ImageFilter.GaussianBlur(6))
    front_card = Image.alpha_composite(front_card, flap_shadow)

    # 3. Clean, Minimalist Center Mark: Subtle White Conversion Exchange Symbol
    # A sleek, balanced circular exchange pair: two clean curved arrows
    center_x = f_x1 + card_w // 2 - 4
    center_y = f_y1 + card_h // 2 + 18
    r_outer = 66
    r_inner = 50
    box = [center_x - r_outer, center_y - r_outer, center_x + r_outer, center_y + r_outer]

    # Upper-right arc
    fc_draw.arc(box, start=205, end=350, fill=(255, 255, 255, 250), width=12)
    # Upper-right arrowhead
    fc_draw.polygon([
        (center_x + r_outer - 4, center_y - 20),
        (center_x + r_outer + 16, center_y - 2),
        (center_x + r_outer - 18, center_y - 2)
    ], fill=(255, 255, 255, 250))

    # Lower-left arc
    fc_draw.arc(box, start=25, end=170, fill=(255, 255, 255, 250), width=12)
    # Lower-left arrowhead
    fc_draw.polygon([
        (center_x - r_outer + 4, center_y + 20),
        (center_x - r_outer - 16, center_y + 2),
        (center_x - r_outer + 18, center_y + 2)
    ], fill=(255, 255, 255, 250))

    # Inner subtle dot for balance
    fc_draw.ellipse([center_x - 8, center_y - 8, center_x + 8, center_y + 8], fill=(255, 255, 255, 240))

    img = Image.alpha_composite(img, front_card)

    # 4. Save High-DPI 512x512 PNG and Multi-Size .ICO
    final_512 = img.resize((512, 512), Image.Resampling.LANCZOS)
    png_path = output_dir / "app_icon.png"
    final_512.save(png_path, "PNG")

    # Generate multi-size .ICO (256, 128, 64, 48, 32, 24, 16)
    ico_path = output_dir / "app_icon.ico"
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
    final_512.save(ico_path, format="ICO", sizes=sizes)
    print(f"[OK] Generated clean refined icon: {png_path} and {ico_path}")

if __name__ == "__main__":
    create_refined_app_icon(Path("assets"))
