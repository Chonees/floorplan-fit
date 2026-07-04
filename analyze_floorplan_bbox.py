"""Bbox por capa (solo geometria: LINE/LWPOLYLINE/ARC/CIRCLE/POLYLINE) de un DXF.

Para comparar el footprint estructural (paredes) contra el dibujo completo.
"""
import sys
import math
from collections import defaultdict

path = sys.argv[1]

with open(path, "r", errors="replace") as f:
    lines = [line.rstrip("\r\n") for line in f]

i = 0
while i < len(lines) - 1:
    if lines[i].strip() == "2" and lines[i + 1].strip() == "ENTITIES":
        break
    i += 2

GEOM = {"LINE", "LWPOLYLINE", "POLYLINE", "VERTEX", "ARC", "CIRCLE", "ELLIPSE", "SPLINE", "SOLID", "3DFACE"}

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
        current = {"type": value, "layer": "", "xs": [], "ys": [], "r": 0.0, "cx": None, "cy": None,
                   "a1": 0.0, "a2": 360.0}
        continue
    if current is None:
        continue
    if code == "8":
        current["layer"] = value
    elif code in ("10", "11"):
        try:
            v = float(value)
            if current["type"] in ("ARC", "CIRCLE") and code == "10":
                current["cx"] = v
            else:
                current["xs"].append(v)
        except ValueError:
            pass
    elif code in ("20", "21"):
        try:
            v = float(value)
            if current["type"] in ("ARC", "CIRCLE") and code == "20":
                current["cy"] = v
            else:
                current["ys"].append(v)
        except ValueError:
            pass
    elif code == "40" and current["type"] in ("ARC", "CIRCLE"):
        try:
            current["r"] = float(value)
        except ValueError:
            pass
    elif code == "50" and current["type"] == "ARC":
        try:
            current["a1"] = float(value)
        except ValueError:
            pass
    elif code == "51" and current["type"] == "ARC":
        try:
            current["a2"] = float(value)
        except ValueError:
            pass
if current:
    entities.append(current)

# Expandir arcos/circulos a puntos como hace el reader (flatten)
for e in entities:
    if e["type"] in ("ARC", "CIRCLE") and e["cx"] is not None:
        a1, a2 = e["a1"], e["a2"]
        if e["type"] == "CIRCLE":
            a1, a2 = 0.0, 360.0
        if a2 < a1:
            a2 += 360.0
        steps = max(2, int((a2 - a1) / 10.0))
        for k in range(steps + 1):
            ang = math.radians(a1 + (a2 - a1) * k / steps)
            e["xs"].append(e["cx"] + math.cos(ang) * e["r"])
            e["ys"].append(e["cy"] + math.sin(ang) * e["r"])

geo = [e for e in entities if e["type"] in GEOM and e["xs"]]
by_layer = defaultdict(list)
for e in geo:
    by_layer[e["layer"]].append(e)

def bbox(items):
    xs = [x for e in items for x in e["xs"]]
    ys = [y for e in items for y in e["ys"]]
    return min(xs), max(xs), min(ys), max(ys)

print(f"=== {path}")
print(f"=== Geometria por capa (bbox) ===")
rows = []
for layer, items in by_layer.items():
    minx, maxx, miny, maxy = bbox(items)
    kinds = defaultdict(int)
    for e in items:
        kinds[e["type"]] += 1
    rows.append((layer, len(items), minx, maxx, miny, maxy, dict(kinds)))
for layer, n, minx, maxx, miny, maxy, kinds in sorted(rows, key=lambda r: -(r[3]-r[2])):
    print(f"{layer:<28} n={n:>4}  X[{minx:>10.3f} .. {maxx:>10.3f}] w={maxx-minx:>9.3f}   "
          f"Y[{miny:>10.3f} .. {maxy:>10.3f}] h={maxy-miny:>9.3f}  {kinds}")

minx, maxx, miny, maxy = bbox(geo)
print(f"\n=== BBOX GLOBAL geometria: X[{minx:.3f} .. {maxx:.3f}] w={maxx-minx:.6f}   Y[{miny:.3f} .. {maxy:.3f}] h={maxy-miny:.6f}")
print(f"=== Footprint anotado en los ejemplos: w=483.785586  h=930")
