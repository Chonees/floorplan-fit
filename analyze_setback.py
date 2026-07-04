"""Analiza un DXF: entidades por capa SETBACK, bbox por entidad y del conjunto.

Replica la logica de ResolveBuildableArea de IxMiliaSitePlanPreviewReader para
ver que area edificable calcularia la app contra lo que se dibuja en naranja.
"""
import sys
from collections import defaultdict

path = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan\PLANS\originalsSitePlans\158 DAWSON STREET.dxf"

with open(path, "r", errors="replace") as f:
    lines = [line.rstrip("\r\n") for line in f]

# Saltar hasta ENTITIES
i = 0
while i < len(lines) - 1:
    if lines[i].strip() == "2" and lines[i + 1].strip() == "ENTITIES":
        break
    i += 2

entities = []
current = None
while i < len(lines) - 1:
    code = lines[i].strip()
    value = lines[i + 1].strip()
    i += 2
    if code == "0":
        if value == "ENDSEC":
            break
        if current:
            entities.append(current)
        current = {"type": value, "layer": "", "closed": False, "xs": [], "ys": [], "text": ""}
        continue
    if current is None:
        continue
    if code == "8":
        current["layer"] = value
    elif code == "70" and current["type"] == "LWPOLYLINE":
        try:
            current["closed"] = (int(value) & 1) == 1
        except ValueError:
            pass
    elif code in ("10", "11"):
        try:
            current["xs"].append(float(value))
        except ValueError:
            pass
    elif code in ("20", "21"):
        try:
            current["ys"].append(float(value))
        except ValueError:
            pass
    elif code == "1":
        current["text"] = value
if current:
    entities.append(current)

print(f"=== Archivo: {path}")
print(f"=== Total entidades: {len(entities)}\n")

print("=== Capas presentes ===")
by_layer = defaultdict(list)
for e in entities:
    by_layer[e["layer"]].append(e)
for layer, items in sorted(by_layer.items(), key=lambda kv: -len(kv[1])):
    kinds = defaultdict(int)
    for e in items:
        kinds[e["type"]] += 1
    kinds_str = ", ".join(f"{k}x{v}" for k, v in kinds.items())
    print(f"{layer:<40} {len(items):>4} entidades  tipos: {kinds_str}")

def bbox(items):
    xs = [x for e in items for x in e["xs"]]
    ys = [y for e in items for y in e["ys"]]
    if not xs:
        return None
    return min(xs), max(xs), min(ys), max(ys)

setback = [e for e in entities if "SETBACK" in e["layer"].upper() or "SETBACK" in e.get("text", "").upper()]
print(f"\n=== Entidades setback (capa o texto contiene SETBACK): {len(setback)} ===")
for e in setback:
    b = bbox([e])
    if b:
        minx, maxx, miny, maxy = b
        print(f"{e['type']:<10} capa={e['layer']:<28} closed={str(e['closed']):<5} pts={len(e['xs']):>3}  "
              f"X[{minx:.3f} .. {maxx:.3f}] w={maxx-minx:.3f}  Y[{miny:.3f} .. {maxy:.3f}] h={maxy-miny:.3f}  "
              f"text={e['text'][:30]!r}")
    else:
        print(f"{e['type']:<10} capa={e['layer']:<28} (sin coords) text={e['text'][:40]!r}")

# Como ResolveBuildableArea: solo PATHS (no TEXT/MTEXT) de capas setback
geo = [e for e in setback if e["type"] not in ("TEXT", "MTEXT") and e["xs"]]
b = bbox(geo)
if b:
    minx, maxx, miny, maxy = b
    print(f"\n=== BBOX CONJUNTO setback-geometria (lo que usa ResolveBuildableArea):")
    print(f"    X[{minx:.3f} .. {maxx:.3f}] ANCHO={maxx-minx:.3f}   Y[{miny:.3f} .. {maxy:.3f}] ALTO={maxy-miny:.3f}")

print("\n=== Polylines CERRADAS (todas las capas), por area bbox desc ===")
rows = []
for e in entities:
    if e["closed"] and len(e["xs"]) > 1:
        minx, maxx, miny, maxy = bbox([e])
        rows.append((e["layer"], (maxx-minx)*(maxy-miny), minx, maxx, miny, maxy))
for layer, area, minx, maxx, miny, maxy in sorted(rows, key=lambda r: -r[1])[:8]:
    print(f"capa={layer:<28} area={area:>16.1f}  X[{minx:.3f}..{maxx:.3f}] w={maxx-minx:.3f}  Y[{miny:.3f}..{maxy:.3f}] h={maxy-miny:.3f}")

# Bbox global
b = bbox([e for e in entities if e["xs"]])
if b:
    minx, maxx, miny, maxy = b
    print(f"\n=== BBOX GLOBAL del site plan: X[{minx:.3f}..{maxx:.3f}] w={maxx-minx:.3f}  Y[{miny:.3f}..{maxy:.3f}] h={maxy-miny:.3f}")
