"""Regenera los 4 DXFs de ejemplo de setback en exports/ con el footprint ESTRUCTURAL.

Mide el footprint estructural del SEMINOLE2000 desde la DB con el MISMO algoritmo
que StructuralFootprint.cs (masa de pared, 2048 bins, cola 0.5%, snap a coordenadas
reales de pared) y construye cada setback restando el deficit del titulo sobre esa
medida. Asi el plano sobresale EXACTAMENTE lo que dice cada caso, en ambos ejes.
"""
import sqlite3

DB = r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan\src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db"
OUT = r"C:\Users\lucas\OneDrive\Escritorio\exports"
RID = "c2496c35-ad07-4846-a68c-6bcc34027d69"
BINS = 2048
TAIL = 0.005

con = sqlite3.connect(DB)
cur = con.cursor()
cur.execute("""SELECT CAST(s.start_x AS REAL),CAST(s.start_y AS REAL),CAST(s.end_x AS REAL),CAST(s.end_y AS REAL)
FROM extracted_wall_candidates w JOIN geometry_segments s ON s.geometry_path_id=w.geometry_path_id
WHERE w.wall_extraction_run_id=?""", (RID,))
segs = cur.fetchall()

def structural_extent(horizontal):
    proj = []
    amin, amax = float("inf"), float("-inf")
    for sx, sy, ex, ey in segs:
        a, b = (min(sx, ex), max(sx, ex)) if horizontal else (min(sy, ey), max(sy, ey))
        length = ((ex - sx) ** 2 + (ey - sy) ** 2) ** 0.5
        proj.append((a, b, length))
        amin, amax = min(amin, a), max(amax, b)
    step = (amax - amin) / BINS
    mass = [0.0] * (BINS + 1)
    total = 0.0
    for a, b, length in proj:
        if length <= 0:
            continue
        total += length
        if b - a <= step:
            mass[min(max(int((a - amin) / step), 0), BINS)] += length
        else:
            density = length / (b - a)
            i0 = min(max(int((a - amin) / step), 0), BINS)
            i1 = min(max(int((b - amin) / step), 0), BINS)
            for bn in range(i0, i1 + 1):
                bs = amin + bn * step
                overlap = min(b, bs + step) - max(a, bs)
                if overlap > 0:
                    mass[bn] += density * overlap
    tail = TAIL * total
    acc, lo = 0.0, 0
    for bn in range(BINS + 1):
        acc += mass[bn]
        if acc > tail:
            lo = bn
            break
    acc, hi = 0.0, BINS
    for bn in range(BINS, -1, -1):
        acc += mass[bn]
        if acc > tail:
            hi = bn
            break
    lo_t, hi_t = amin + lo * step, amin + (hi + 1) * step
    cmin = cmax = None
    for a, b, _ in proj:
        for c in (a, b):
            if lo_t - 1e-6 <= c <= hi_t + 1e-6:
                cmin = c if cmin is None else min(cmin, c)
                cmax = c if cmax is None else max(cmax, c)
    return cmin, cmax

x0, x1 = structural_extent(True)
y0, y1 = structural_extent(False)
W = x1 - x0
H = y1 - y0
print(f"Footprint ESTRUCTURAL medido: W={W:.6f}\"  H={H:.6f}\"  (X[{x0:.6f}..{x1:.6f}] Y[{y0:.6f}..{y1:.6f}])")

def fmt(v):
    s = f"{v:.6f}".rstrip("0").rstrip(".")
    return s if s else "0"

LAYERS = [
    ("PROPERTY", 8), ("SETBACK", 30), ("FLOORPLAN_FOOTPRINT_REFERENCE", 1),
    ("OVERFLOW_STRIP", 1), ("SETBACK_LABELS", 30), ("ANNOTATION", 7), ("REFERENCE", 5),
]

def dxf_header(extmin, extmax):
    return ["0","SECTION","2","HEADER",
            "9","$ACADVER","1","AC1027",
            "9","$INSUNITS","70","1",
            "9","$MEASUREMENT","70","0",
            "9","$EXTMIN","10",fmt(extmin[0]),"20",fmt(extmin[1]),"30","0",
            "9","$EXTMAX","10",fmt(extmax[0]),"20",fmt(extmax[1]),"30","0",
            "0","ENDSEC"]

def dxf_tables():
    out = ["0","SECTION","2","TABLES",
           "0","TABLE","2","LTYPE","70","1",
           "0","LTYPE","2","CONTINUOUS","70","0","3","Solid line","72","65","73","0","40","0",
           "0","ENDTAB",
           "0","TABLE","2","LAYER","70",str(len(LAYERS))]
    for name, color in LAYERS:
        out += ["0","LAYER","2",name,"70","0","62",str(color),"6","CONTINUOUS"]
    out += ["0","ENDTAB",
            "0","TABLE","2","STYLE","70","1",
            "0","STYLE","2","STANDARD","70","0","40","0","41","1","50","0","71","0","42","2.5","3","txt","4","",
            "0","ENDTAB","0","ENDSEC"]
    return out

def line(layer, color, p1, p2):
    return ["0","LINE","8",layer,"62",str(color),
            "10",fmt(p1[0]),"20",fmt(p1[1]),"30","0",
            "11",fmt(p2[0]),"21",fmt(p2[1]),"31","0"]

def rect(layer, color, minx, miny, maxx, maxy):
    out = []
    out += line(layer, color, (minx, miny), (maxx, miny))
    out += line(layer, color, (maxx, miny), (maxx, maxy))
    out += line(layer, color, (maxx, maxy), (minx, maxy))
    out += line(layer, color, (minx, maxy), (minx, miny))
    return out

def text(layer, color, x, y, height, value):
    return ["0","TEXT","8",layer,"62",str(color),
            "10",fmt(x),"20",fmt(y),"30","0","40",str(height),"1",value,"50","0"]

def build_example(number, axis, deficit):
    # Footprint estructural de referencia colocado con su esquina en (100, 100).
    ref = (100.0, 100.0, 100.0 + W, 100.0 + H)
    if axis == "ancho":
        sb = (ref[0] + deficit / 2, ref[1], ref[2] - deficit / 2, ref[3])
        overflow = "L/R/B/T = {0}\"/{0}\"/0\"/0\"".format(fmt(deficit / 2))
        titulo = f"EJEMPLO 0{number} - SETBACK {deficit:.0f} INCH MENOS DE ANCHO ESTRUCTURAL"
        deficit_line = f"Deficit total W/H = {deficit:.0f}\"/0\". Overflow {overflow}."
    else:
        sb = (ref[0], ref[1] + deficit / 2, ref[2], ref[3] - deficit / 2)
        overflow = "L/R/B/T = 0\"/0\"/{0}\"/{0}\"".format(fmt(deficit / 2))
        titulo = f"EJEMPLO 0{number} - SETBACK {deficit:.0f} INCH MENOS DE ALTO ESTRUCTURAL"
        deficit_line = f"Deficit total W/H = 0\"/{deficit:.0f}\". Overflow {overflow}."

    prop = (ref[0] - 60, ref[1] - 60, ref[2] + 60, ref[3] + 60)
    ents = []
    ents += rect("PROPERTY", 8, *prop)
    ents += rect("FLOORPLAN_FOOTPRINT_REFERENCE", 1, *ref)
    ents += rect("SETBACK", 30, *sb)
    # Franjas de sobresalto esperado (solo informativas, capa filtrada por la app)
    if axis == "ancho":
        ents += rect("OVERFLOW_STRIP", 1, ref[0], ref[1], sb[0], ref[3])
        ents += rect("OVERFLOW_STRIP", 1, sb[2], ref[1], ref[2], ref[3])
    else:
        ents += rect("OVERFLOW_STRIP", 1, ref[0], ref[1], ref[2], sb[1])
        ents += rect("OVERFLOW_STRIP", 1, ref[0], sb[3], ref[2], ref[3])

    ty = ref[3] + 128
    ents += text("SETBACK_LABELS", 30, prop[0], ty, 10, titulo)
    ents += text("ANNOTATION", 7, prop[0], ty - 18, 7,
                 f"Footprint ESTRUCTURAL (masa de pared) {fmt(W)}\" x {fmt(H)}\". Setback centrado: {fmt(sb[2]-sb[0])}\" x {fmt(sb[3]-sb[1])}\".")
    ents += text("ANNOTATION", 7, ty and ty - 0 and prop[0], ty - 34, 7, deficit_line)
    ents += text("ANNOTATION", 7, prop[0], ty - 50, 7,
                 "Medicion robusta: apendices con masa despreciable no cuentan para el ancho/alto.")
    ents += text("SETBACK", 30, sb[0] + 18, sb[3] - 32, 8, "SETBACK BUILDABLE CENTRADO Y MENOR")

    extmin = (prop[0] - 80, prop[1] - 80)
    extmax = (prop[2] + 150, ty + 62)
    doc = dxf_header(extmin, extmax) + dxf_tables() + ["0","SECTION","2","ENTITIES"] + ents + ["0","ENDSEC","0","EOF"]
    return "\n".join(doc) + "\n", sb

CASES = [
    (1, "ancho", 1.0, "setback ejemplo 01 - DEFICIT TOTAL - ancho menos 1 inch.dxf"),
    (2, "ancho", 2.0, "setback ejemplo 02 - DEFICIT TOTAL - ancho menos 2 inches.dxf"),
    (3, "alto",  1.0, "setback ejemplo 03 - DEFICIT TOTAL - alto menos 1 inch.dxf"),
    (4, "alto",  2.0, "setback ejemplo 04 - DEFICIT TOTAL - alto menos 2 inches.dxf"),
]

print()
for number, axis, deficit, filename in CASES:
    content, sb = build_example(number, axis, deficit)
    path = f"{OUT}\\{filename}"
    with open(path, "w", newline="\n") as f:
        f.write(content)
    sbw, sbh = sb[2] - sb[0], sb[3] - sb[1]

    # Verificacion: replica del pipeline de la app (centrado estructural + deficit)
    bcx, bcy = (sb[0] + sb[2]) / 2, (sb[1] + sb[3]) / 2
    fcx, fcy = (x0 + x1) / 2, (y0 + y1) / 2
    ox, oy = bcx - fcx, bcy - fcy
    pw = (x0 + ox, x1 + ox, y0 + oy, y1 + oy)
    wdef = max(0.0, W - sbw)
    hdef = max(0.0, H - sbh)
    left = max(0.0, sb[0] - pw[0]); right = max(0.0, pw[1] - sb[2])
    bottom = max(0.0, sb[1] - pw[2]); top = max(0.0, pw[3] - sb[3])
    print(f"{filename}")
    print(f"  setback {sbw:.3f} x {sbh:.3f}  |  la app dira: Deficit ancho {wdef:.3f}\" alto {hdef:.3f}\"")
    print(f"  sobresalto visual: izq {left:.3f}\"  der {right:.3f}\"  abajo {bottom:.3f}\"  arriba {top:.3f}\"")
