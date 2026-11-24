#!/usr/bin/env python3
"""
FileForge Python Conversion Bridge
Fast, lightweight, lazy-loaded worker for image, office, and document transcoding.
Invoked on-demand and exits immediately to release memory.
"""

import sys
import os
import argparse
import json
from pathlib import Path

def convert_image(source: Path, target: Path, quality: int = 85, width: int = None, height: int = None):
    from PIL import Image

    with Image.open(source) as img:
        target_ext = target.suffix.lower().lstrip('.')
        
        # Format normalization
        if target_ext in ('jpg', 'jpeg'):
            if img.mode in ('RGBA', 'LA', 'P'):
                # White background for transparency
                background = Image.new('RGB', img.size, (255, 255, 255))
                background.paste(img, mask=img.split()[-1] if img.mode in ('RGBA', 'LA') else None)
                img = background
            elif img.mode != 'RGB':
                img = img.convert('RGB')
        elif target_ext == 'png':
            pass
        elif target_ext == 'webp':
            if img.mode not in ('RGB', 'RGBA'):
                img = img.convert('RGBA')

        # Optional resize
        if width or height:
            orig_w, orig_h = img.size
            if width and height:
                new_size = (width, height)
            elif width:
                new_size = (width, int(orig_h * (width / orig_w)))
            else:
                new_size = (int(orig_w * (height / orig_h)), height)
            img = img.resize(new_size, Image.Resampling.LANCZOS)

        target.parent.mkdir(parents=True, exist_ok=True)
        
        save_kwargs = {}
        if target_ext in ('jpg', 'jpeg', 'webp'):
            save_kwargs['quality'] = quality
        elif target_ext == 'ico':
            save_kwargs['sizes'] = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]

        img.save(target, **save_kwargs)
        return True

def convert_excel(source: Path, target: Path):
    target_ext = target.suffix.lower().lstrip('.')
    source_ext = source.suffix.lower().lstrip('.')
    
    if source_ext == 'xlsx' and target_ext in ('csv', 'tsv'):
        import openpyxl
        import csv
        
        wb = openpyxl.load_workbook(source, data_only=True, read_only=True)
        sheet = wb.active
        delimiter = '\t' if target_ext == 'tsv' else ','
        
        target.parent.mkdir(parents=True, exist_ok=True)
        with open(target, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f, delimiter=delimiter)
            for row in sheet.iter_rows(values_only=True):
                writer.writerow([cell if cell is not None else '' for cell in row])
        return True

    elif source_ext in ('csv', 'tsv') and target_ext == 'xlsx':
        import openpyxl
        import csv
        
        delimiter = '\t' if source_ext == 'tsv' else ','
        wb = openpyxl.Workbook()
        ws = wb.active
        
        with open(source, 'r', encoding='utf-8', errors='replace') as f:
            reader = csv.reader(f, delimiter=delimiter)
            for row in reader:
                ws.append(row)
                
        target.parent.mkdir(parents=True, exist_ok=True)
        wb.save(target)
        return True

    elif source_ext == 'xlsx' and target_ext == 'json':
        import openpyxl
        
        wb = openpyxl.load_workbook(source, data_only=True, read_only=True)
        sheet = wb.active
        rows = list(sheet.iter_rows(values_only=True))
        if not rows:
            data = []
        else:
            headers = [str(c) if c is not None else f"col_{i}" for i, c in enumerate(rows[0])]
            data = []
            for r in rows[1:]:
                row_dict = {headers[i]: r[i] for i in range(min(len(headers), len(r)))}
                data.append(row_dict)
                
        target.parent.mkdir(parents=True, exist_ok=True)
        with open(target, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, default=str)
        return True

    return False

def convert_document(source: Path, target: Path):
    source_ext = source.suffix.lower().lstrip('.')
    target_ext = target.suffix.lower().lstrip('.')
    
    target.parent.mkdir(parents=True, exist_ok=True)

    # Word DOCX extraction
    if source_ext == 'docx':
        import docx
        doc = docx.Document(source)
        text_parts = [p.text for p in doc.paragraphs]
        content = "\n\n".join(text_parts)
        
        if target_ext in ('txt', 'md'):
            with open(target, 'w', encoding='utf-8') as f:
                f.write(content)
            return True
        elif target_ext == 'html':
            html_parts = [f"<p>{p.text}</p>" for p in doc.paragraphs if p.text]
            html = f"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{source.stem}</title></head><body>{''.join(html_parts)}</body></html>"
            with open(target, 'w', encoding='utf-8') as f:
                f.write(html)
            return True

    # PDF extraction
    elif source_ext == 'pdf':
        try:
            import pdfplumber
            text_pages = []
            with pdfplumber.open(source) as pdf:
                for page in pdf.pages:
                    txt = page.extract_text()
                    if txt:
                        text_pages.append(txt)
            content = "\n\n--- Page Break ---\n\n".join(text_pages)
        except Exception:
            # Fallback to pypdfium2
            import pypdfium2 as pdfium
            pdf = pdfium.PdfDocument(source)
            text_pages = []
            for i in range(len(pdf)):
                page = pdf[i]
                textpage = page.get_textpage()
                text_pages.append(textpage.get_text_range())
            content = "\n\n--- Page Break ---\n\n".join(text_pages)

        if target_ext in ('txt', 'md'):
            with open(target, 'w', encoding='utf-8') as f:
                f.write(content)
            return True
        elif target_ext == 'html':
            escaped = content.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            html = f"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{source.stem}</title></head><body><pre>{escaped}</pre></body></html>"
            with open(target, 'w', encoding='utf-8') as f:
                f.write(html)
            return True

    # Markdown to HTML
    elif source_ext == 'md' and target_ext == 'html':
        with open(source, 'r', encoding='utf-8', errors='replace') as f:
            md_text = f.read()
        import html
        escaped = html.escape(md_text)
        page = f"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{source.stem}</title><style>body{{font-family:sans-serif;max-width:800px;margin:2rem auto;line-height:1.6;}}</style></head><body><pre>{escaped}</pre></body></html>"
        with open(target, 'w', encoding='utf-8') as f:
            f.write(page)
        return True

    return False

def main():
    parser = argparse.ArgumentParser(description="FileForge Converter CLI")
    parser.add_argument("--source", required=True, help="Input source file path")
    parser.add_argument("--target", required=True, help="Output destination file path")
    parser.add_argument("--quality", type=int, default=85, help="Image quality (1-100)")
    parser.add_argument("--width", type=int, default=None, help="Resize width")
    parser.add_argument("--height", type=int, default=None, help="Resize height")

    args = parser.parse_args()
    source = Path(args.source)
    target = Path(args.target)

    if not source.exists():
        sys.stderr.write(f"Error: Source file '{source}' does not exist.\n")
        sys.exit(1)

    src_ext = source.suffix.lower().lstrip('.')
    tgt_ext = target.suffix.lower().lstrip('.')

    image_exts = {'png', 'jpg', 'jpeg', 'webp', 'bmp', 'ico', 'tiff', 'tif', 'gif'}
    excel_exts = {'xlsx', 'csv', 'tsv'}

    try:
        # Route to proper engine
        if src_ext in image_exts and tgt_ext in image_exts | {'pdf'}:
            ok = convert_image(source, target, quality=args.quality, width=args.width, height=args.height)
        elif (src_ext in excel_exts and tgt_ext in excel_exts | {'json'}) or (src_ext == 'xlsx' and tgt_ext in ('csv', 'tsv', 'json')):
            ok = convert_excel(source, target)
        else:
            ok = convert_document(source, target)

        if ok:
            print(json.dumps({"status": "success", "source": str(source), "target": str(target)}))
            sys.exit(0)
        else:
            sys.stderr.write(f"Conversion from {src_ext} to {tgt_ext} not supported by Python bridge.\n")
            sys.exit(2)
    except Exception as e:
        sys.stderr.write(f"Error: {str(e)}\n")
        sys.exit(3)

if __name__ == "__main__":
    main()
