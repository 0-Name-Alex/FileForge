import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

def create_app_icon(output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    size = 1024  # Render at 2x supersampling for ultra crispness
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Squircle background (rounded rectangle)
    pad = 48
    corner_r = 210
    squircle_box = [pad, pad, size - pad, size - pad]

    # Gradient background: Deep slate (#0B0F19) to Deep Indigo (#1E1B4B)
    bg_gradient = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg_gradient)
    for y in range(pad, size - pad):
        factor = (y - pad) / (size - 2 * pad)
        r = int(11 * (1 - factor) + 26 * factor)
        g = int(18 * (1 - factor) + 27 * factor)
        b = int(32 * (1 - factor) + 65 * factor)
        bg_draw.line([(pad, y), (size - pad, y)], fill=(r, g, b, 255))

    # Mask for squircle
    mask = Image.new("L", (size, size), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle(squircle_box, radius=corner_r, fill=255)
    img.paste(bg_gradient, (0, 0), mask)

    # 2. Glowing border rim (Cyan -> Indigo gradient stroke)
    border_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    border_draw = ImageDraw.Draw(border_img)
    border_draw.rounded_rectangle(squircle_box, radius=corner_r, outline=(56, 189, 248, 180), width=10)
    
    # Ambient rim glow
    glow = border_img.filter(ImageFilter.GaussianBlur(16))
    img = Image.alpha_composite(img, glow)
    img = Image.alpha_composite(img, border_img)

    # 3. Main Centerpiece: Stylized Digital Anvil + Folded Document
    overlay = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    odraw = ImageDraw.Draw(overlay)

    cx, cy = size // 2, size // 2

    # A) Futuristic Anvil Base
    # Points for a sleek modern anvil silhouette
    anvil_points = [
        (cx - 220, cy + 180), # Left foot
        (cx - 150, cy + 220),
        (cx + 150, cy + 220),
        (cx + 220, cy + 180), # Right foot
        (cx + 120, cy + 120), # Waist right
        (cx + 280, cy + 40),  # Anvil horn top right
        (cx + 260, cy - 10),  # Horn tip
        (cx + 160, cy + 10),  # Face right
        (cx - 180, cy + 10),  # Face left
        (cx - 260, cy - 20),  # Anvil horn top left (pointed)
        (cx - 240, cy + 40),
        (cx - 120, cy + 120), # Waist left
    ]
    
    # Anvil shadow/glow
    anvil_glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ag_draw = ImageDraw.Draw(anvil_glow)
    ag_draw.polygon(anvil_points, fill=(14, 165, 233, 140))
    anvil_glow = anvil_glow.filter(ImageFilter.GaussianBlur(24))
    img = Image.alpha_composite(img, anvil_glow)

    # Fill anvil with dark metallic gradient
    odraw.polygon(anvil_points, fill=(30, 41, 59, 255), outline=(56, 189, 248, 230), width=8)

    # Anvil top highlight line
    odraw.line([(cx - 170, cy + 12), (cx + 150, cy + 12)], fill=(224, 242, 254, 240), width=6)

    # B) Document Icon rising from the forge (White/Cyan folded document)
    doc_w, doc_h = 240, 310
    dx1, dy1 = cx - 120, cy - 220
    dx2, dy2 = dx1 + doc_w, dy1 + doc_h
    fold = 70

    doc_points = [
        (dx1, dy1),
        (dx2 - fold, dy1),
        (dx2, dy1 + fold),
        (dx2, dy2),
        (dx1, dy2)
    ]

    # Document glow
    doc_glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    dg_draw = ImageDraw.Draw(doc_glow)
    dg_draw.polygon(doc_points, fill=(56, 189, 248, 120))
    doc_glow = doc_glow.filter(ImageFilter.GaussianBlur(30))
    img = Image.alpha_composite(img, doc_glow)

    # Document body (frosted white-cyan gradient)
    odraw.polygon(doc_points, fill=(240, 249, 255, 245), outline=(56, 189, 248, 255), width=8)

    # Document folded corner flap
    flap_points = [
        (dx2 - fold, dy1),
        (dx2 - fold, dy1 + fold),
        (dx2, dy1 + fold)
    ]
    odraw.polygon(flap_points, fill=(186, 230, 253, 255), outline=(56, 189, 248, 255), width=8)

    # Text lines on the document
    line_x1 = dx1 + 40
    line_x2 = dx2 - 40
    for ly in [dy1 + 110, dy1 + 155, dy1 + 200, dy1 + 245]:
        width = line_x2 if ly != dy1 + 245 else line_x1 + 90
        odraw.rounded_rectangle([line_x1, ly, width, ly + 14], radius=7, fill=(14, 165, 233, 200))

    # C) Kinetic Conversion Vortex / Energy Loop Arrows
    # Draw two sleek orbital swooshes
    arc_box = [cx - 280, cy - 140, cx + 280, cy + 180]
    odraw.arc(arc_box, start=35, end=145, fill=(6, 182, 212, 255), width=12)
    odraw.arc(arc_box, start=215, end=325, fill=(168, 85, 247, 255), width=12)

    # Arrowhead 1 (bottom left)
    odraw.polygon([(cx - 200, cy + 160), (cx - 170, cy + 130), (cx - 165, cy + 180)], fill=(6, 182, 212, 255))
    # Arrowhead 2 (top right)
    odraw.polygon([(cx + 200, cy - 120), (cx + 170, cy - 90), (cx + 165, cy - 140)], fill=(168, 85, 247, 255))

    # D) Floating format sparks / gems
    # Small diamond spark top left
    for sx, sy, scolor in [(cx - 260, cy - 160, (56, 189, 248, 255)), (cx + 260, cy + 100, (244, 63, 94, 255)), (cx - 220, cy + 70, (16, 185, 129, 255))]:
        r = 16
        odraw.polygon([(sx, sy - r), (sx + r, sy), (sx, sy + r), (sx - r, sy)], fill=scolor)

    img = Image.alpha_composite(img, overlay)

    # Downsample with Lanczos for super-smooth rendering
    final_512 = img.resize((512, 512), Image.Resampling.LANCZOS)
    png_path = output_dir / "app_icon.png"
    final_512.save(png_path, "PNG")
    print(f"[OK] Generated {png_path} (512x512)")

    # Generate multi-size .ICO
    ico_path = output_dir / "app_icon.ico"
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
    final_512.save(ico_path, format="ICO", sizes=sizes)
    print(f"[OK] Generated {ico_path} with {len(sizes)} resolutions")

if __name__ == "__main__":
    create_app_icon(Path("assets"))
