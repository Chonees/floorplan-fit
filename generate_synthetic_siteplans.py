"""Genera site plans sinteticos REALISTAS con apariencia Pointe (estilo 158 DAWSON).

Cada archivo tiene un deficit EXACTO de ancho o alto contra el footprint ESTRUCTURAL
del SEMINOLE2000 (medido por masa de pared desde la DB, mismo algoritmo que la app).
Formas variadas (rectangular, chaflan, fillets, frente curvo, lote trapezoidal) pero
los vertices extremos del setback siempre clavan el bbox exacto, asi el buildable que
lee la app mide footprint - deficit al micron.

Layers y rotulo de la empresa (copiados de 158 DAWSON STREET.dxf):
  SETBACKS (color 31) / 2312-001-BM$0$C-PROP-SUBD (color 7, ltype PHANTOM2) /
  E (color 4) / TEXT (color 6) / 0.

La geometria inventada conserva los deficits exactos; el texto visible evita diagnosticos
tipo "BUILDABLE BBOX" y usa un rotulo corto/falso con la misma familia visual del plano real.
Los nombres de archivo son descriptivos para que el picker muestre que deficit se prueba.
"""
import math
import os
import sqlite3

DB = r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan\src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db"
OUT = r"D:\PointAIData\PLANS\originalsSitePlans"
RID = "c2496c35-ad07-4846-a68c-6bcc34027d69"
BINS, TAIL = 2048, 0.005

os.makedirs(OUT, exist_ok=True)

con = sqlite3.connect(DB)
cur = con.cursor()
cur.execute("""SELECT CAST(s.start_x AS REAL),CAST(s.start_y AS REAL),CAST(s.end_x AS REAL),CAST(s.end_y AS REAL)
FROM extracted_wall_candidates w JOIN geometry_segments s ON s.geometry_path_id=w.geometry_path_id
WHERE w.wall_extraction_run_id=?""", (RID,))
segs = cur.fetchall()

def structural_extent(horizontal):
    proj, amin, amax = [], float("inf"), float("-inf")
    for sx, sy, ex, ey in segs:
        a, b = (min(sx, ex), max(sx, ex)) if horizontal else (min(sy, ey), max(sy, ey))
        length = ((ex - sx) ** 2 + (ey - sy) ** 2) ** 0.5
        proj.append((a, b, length)); amin, amax = min(amin, a), max(amax, b)
    step = (amax - amin) / BINS
    mass, total = [0.0] * (BINS + 1), 0.0
    for a, b, length in proj:
        if length <= 0: continue
        total += length
        if b - a <= step:
            mass[min(max(int((a - amin) / step), 0), BINS)] += length
        else:
            density = length / (b - a)
            i0 = min(max(int((a - amin) / step), 0), BINS)
            i1 = min(max(int((b - amin) / step), 0), BINS)
            for bn in range(i0, i1 + 1):
                bs = amin + bn * step
                ov = min(b, bs + step) - max(a, bs)
                if ov > 0: mass[bn] += density * ov
    tail = TAIL * total
    acc, lo = 0.0, 0
    for bn in range(BINS + 1):
        acc += mass[bn]
        if acc > tail: lo = bn; break
    acc, hi = 0.0, BINS
    for bn in range(BINS, -1, -1):
        acc += mass[bn]
        if acc > tail: hi = bn; break
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
W, H = x1 - x0, y1 - y0
print(f"Footprint estructural: W={W:.6f}  H={H:.6f}")

def fmt(v):
    s = f"{v:.6f}".rstrip("0").rstrip(".")
    return s if s else "0"

PROP_LAYER = "2312-001-BM$0$C-PROP-SUBD"
PHANTOM = "2312-001-BM$0$PHANTOM2"
TEMPLATE = os.path.join(OUT, "158 DAWSON STREET.dxf")
TITLE_BLOCK_SCALE = 5.0
MAX_NORMAL_ADAPTATION_INCHES = 4.0

def read_dxf_pairs(path):
    with open(path, encoding="latin1", errors="ignore") as f:
        lines = f.read().splitlines()
    return [(lines[i].strip(), lines[i + 1]) for i in range(0, len(lines) - 1, 2)]

def write_dxf_pairs(pairs):
    return "".join(f"{code}\n{value}\n" for code, value in pairs)

def tokens_to_pairs(tokens):
    return [(tokens[i].strip(), tokens[i + 1]) for i in range(0, len(tokens) - 1, 2)]

def read_record(pairs, index):
    record = [pairs[index]]
    index += 1
    while index < len(pairs) and pairs[index][0] != "0":
        record.append(pairs[index])
        index += 1
    return record, index

def first_group_value(record, code):
    for item_code, value in record:
        if item_code == code:
            return value.strip()
    return None

def replace_first(record, code, value):
    for index, (item_code, _) in enumerate(record):
        if item_code == code:
            record[index] = (item_code, value)
            return

def resolve_model_space_owner(pairs):
    index = 0
    while index < len(pairs):
        if pairs[index][0] == "0" and pairs[index][1].strip().upper() == "BLOCK_RECORD":
            record, index = read_record(pairs, index)
            if (first_group_value(record, "2") or "").upper() == "*MODEL_SPACE":
                return first_group_value(record, "5")
            continue
        index += 1
    raise RuntimeError("No se encontro BLOCK_RECORD *MODEL_SPACE en el template Pointe.")

def max_dxf_handle(pairs):
    values = []
    for code, value in pairs:
        if code != "5":
            continue
        stripped = value.strip()
        try:
            values.append(int(stripped, 16))
        except ValueError:
            pass
    return max(values) if values else 0

def replace_entities_section(template_pairs, entity_pairs):
    result = []
    index = 0
    while index < len(template_pairs):
        code, value = template_pairs[index]
        if (code == "0" and value.strip().upper() == "SECTION" and
                index + 1 < len(template_pairs) and
                template_pairs[index + 1][0] == "2" and
                template_pairs[index + 1][1].strip().upper() == "ENTITIES"):
            result.extend([template_pairs[index], template_pairs[index + 1]])
            result.extend(entity_pairs)
            index += 2
            while index < len(template_pairs):
                if template_pairs[index][0] == "0" and template_pairs[index][1].strip().upper() == "ENDSEC":
                    result.append(template_pairs[index])
                    index += 1
                    break
                index += 1
            continue
        result.append(template_pairs[index])
        index += 1
    return result

def update_header_point(pairs, variable, x, y, z=0.0):
    result = pairs[:]
    index = 0
    while index + 1 < len(result):
        if result[index][0] == "9" and result[index][1].strip().upper() == variable.upper():
            cursor = index + 1
            while cursor < len(result) and result[cursor][0] not in ("0", "9"):
                if result[cursor][0] == "10":
                    result[cursor] = ("10", fmt(x))
                elif result[cursor][0] == "20":
                    result[cursor] = ("20", fmt(y))
                elif result[cursor][0] == "30":
                    result[cursor] = ("30", fmt(z))
                cursor += 1
            return result
        index += 1
    return result

def update_handseed(pairs, next_handle):
    result = pairs[:]
    index = 0
    while index + 1 < len(result):
        if result[index][0] == "9" and result[index][1].strip().upper() == "$HANDSEED":
            cursor = index + 1
            while cursor < len(result) and result[cursor][0] not in ("0", "9"):
                if result[cursor][0] == "5":
                    result[cursor] = ("5", next_handle)
                    return result
                cursor += 1
        index += 1
    return result

def update_active_vport(pairs, extmin, extmax):
    result = []
    index = 0
    cx = (extmin[0] + extmax[0]) / 2
    cy = (extmin[1] + extmax[1]) / 2
    width = max(1.0, extmax[0] - extmin[0])
    height = max(1.0, extmax[1] - extmin[1])
    margin = 1.15

    while index < len(pairs):
        if pairs[index][0] == "0" and pairs[index][1].strip().upper() == "VPORT":
            record, index = read_record(pairs, index)
            if (first_group_value(record, "2") or "").upper() == "*ACTIVE":
                replace_first(record, "12", fmt(cx))
                replace_first(record, "22", fmt(cy))
                replace_first(record, "40", fmt(height * margin))
                replace_first(record, "41", fmt(width / height))
            result.extend(record)
            continue
        result.append(pairs[index])
        index += 1
    return result

TEMPLATE_PAIRS = read_dxf_pairs(TEMPLATE)
MODEL_SPACE_OWNER = resolve_model_space_owner(TEMPLATE_PAIRS)
TEMPLATE_MAX_HANDLE = max_dxf_handle(TEMPLATE_PAIRS)
next_entity_handle = TEMPLATE_MAX_HANDLE + 1

def reset_handles():
    global next_entity_handle
    next_entity_handle = TEMPLATE_MAX_HANDLE + 1

def next_handle():
    global next_entity_handle
    handle = f"{next_entity_handle:X}"
    next_entity_handle += 1
    return handle

def dxf_doc(extmin, extmax, entities):
    pairs = replace_entities_section(TEMPLATE_PAIRS, tokens_to_pairs(entities))
    pairs = update_header_point(pairs, "$EXTMIN", extmin[0], extmin[1])
    pairs = update_header_point(pairs, "$EXTMAX", extmax[0], extmax[1])
    pairs = update_active_vport(pairs, extmin, extmax)
    pairs = update_handseed(pairs, f"{next_entity_handle:X}")
    return write_dxf_pairs(pairs)

def line(layer, color, p1, p2):
    return ["0","LINE","5",next_handle(),"330",MODEL_SPACE_OWNER,
            "100","AcDbEntity","8",layer,"100","AcDbLine",
            "10",fmt(p1[0]),"20",fmt(p1[1]),"30","0",
            "11",fmt(p2[0]),"21",fmt(p2[1]),"31","0"]

def polyline_lines(layer, color, pts, closed=True):
    out = []
    n = len(pts)
    last = n if closed else n - 1
    for i in range(last):
        out += line(layer, color, pts[i], pts[(i + 1) % n])
    return out

def text(layer, color, x, y, height, value, rotation=0, style="", width_factor=None, oblique=None):
    out = ["0","TEXT","5",next_handle(),"330",MODEL_SPACE_OWNER,
           "100","AcDbEntity","8",layer,"100","AcDbText",
           "10",fmt(x),"20",fmt(y),"30","0","40",fmt(height),"1",value]
    if abs(rotation) > 1e-9:
        out += ["50",fmt(rotation)]
    if width_factor is not None:
        out += ["41",fmt(width_factor)]
    if oblique is not None:
        out += ["51",fmt(oblique)]
    if style:
        out += ["7",style]
    out += ["100","AcDbText"]
    return out

def wide_polyline(layer, pts, width=1.0, closed=False):
    out = ["0","LWPOLYLINE","5",next_handle(),"330",MODEL_SPACE_OWNER,
           "100","AcDbEntity","8",layer,"100","AcDbPolyline",
           "90",str(len(pts)),"70","1" if closed else "0","43",fmt(width)]
    for x, y in pts:
        out += ["10",fmt(x),"20",fmt(y)]
    return out

def add_pointe_title_block(entities, lot_minx, lot_miny, lot_maxx, house_number, street_name, lot_number):
    """Agrega el rotulo tipo Dawson: mismas capas, estilos y alturas del DXF real."""
    center_x = (lot_minx + lot_maxx) / 2
    # El rotulo real vive aproximadamente entre x=18..117, y=14..60.
    # Lo usamos como patron, pero escalado porque los synths estan en un
    # canvas de footprint mucho mas grande que el Dawson de referencia.
    reference_min_x = 18.36979550124266
    reference_max_x = 116.6500198207901
    reference_center_x = (reference_min_x + reference_max_x) / 2
    reference_min_y = 14.02013320275622
    reference_top_y = 60.0
    title_top_y = lot_miny - 45.0

    def x(reference_x):
        return center_x + (reference_x - reference_center_x) * TITLE_BLOCK_SCALE

    def y(reference_y):
        return title_top_y - (reference_top_y - reference_y) * TITLE_BLOCK_SCALE

    def h(reference_height):
        return reference_height * TITLE_BLOCK_SCALE

    entities += text("E", 4, x(57.02538578976959), y(54.72022205809693), h(5.203124999999999),
                     house_number, rotation=359.9357572311935, style="HOUSE")
    entities += text("E", 4, x(33.09759286848872), y(47.78214591224669), h(5.203124999999999),
                     street_name, rotation=359.9357572311935, style="HOUSE")
    entities += text("TEXT", 6, x(45.86430551896643), y(43.42013658487545), h(1.792900433341439),
                     "(50' WIDE PUBLIC R.O.W.)", rotation=0.4308352463661623,
                     style="RS", width_factor=1.1, oblique=5.0)
    entities += text("E", 4, x(21.46135207220101), y(31.3667897758482), h(8.0),
                     "SITE PLAN", style="SITE")
    entities += text("E", 4, x(96.45501988009669), y(30.43613414856959), h(1.6),
                     "SCALE 1'=20'", style="L80")
    entities += wide_polyline("E", [(x(18.85802909639784), y(28.44706587517289)),
                                    (x(116.6500198207901), y(28.44706587517289))],
                              width=TITLE_BLOCK_SCALE)
    entities += text("E", 4, x(20.80979151442767), y(25.1424454018183), h(1.6),
                     f"BEING LOT {lot_number}, BLOCK 25, RANCHO SANTA TERESA UNIT TWO", style="L80")
    entities += text("E", 4, x(20.80979151442767), y(22.24337284541111), h(1.6),
                     "CITY OF SUNLAND PARK, DO\\U+00D1A ANA COUNTY, NEW MEXICO", style="L80")

    table_headers = [
        (18.36979550124266, 17.36613209429783, "CURVE"),
        (28.14383914345996, 17.36613209429783, "RADIUS"),
        (38.29672168661961, 17.41664394771801, "LENGTH"),
        (48.53571489803613, 17.36613209429783, "TANGENT"),
        (62.13041687059337, 17.36613209429783, "CHORD"),
        (78.79923254508645, 17.36613209429783, "BEARING"),
        (96.30158976489799, 17.41664394771801, "DELTA"),
    ]
    for rx, ry, value in table_headers:
        entities += text("E", 4, x(rx), y(ry), h(1.40625), value)

    table_values = [
        (21.1732033675176, 14.43644459429783, "C186"),
        (28.62882734300689, 14.59289715829658, "325.00"),
        (39.01601770497979, 14.61762381520243, "49.16"),
        (49.87465649774418, 14.65697024117111, "24.63"),
        (63.46935847006858, 14.65697024117111, "49.12"),
        (76.23933478189752, 14.48742187250759, "N16%%d44'49\"W"),
        (94.36559917662412, 14.57276528591178, "008%%d40'03\""),
    ]
    for rx, ry, value in table_values:
        entities += text("E", 4, x(rx), y(ry), h(1.40625), value)

    return (
        x(reference_min_x),
        y(reference_min_y),
        x(reference_max_x),
        title_top_y,
    )

def add_survey_labels(entities, lot):
    """Rotulos de mensura tipo Dawson, decorativos; no gobiernan el fit."""
    minx, miny, maxx, maxy = shape_bounds(lot)
    labels = [
        ("S24%%d31'25\"W      75.28'", (minx, maxy), (maxx, maxy), 2.25, 0.0, (0, 24)),
        ("N61%%d18'13\"W       90.08'", (minx, miny), (minx, maxy), 2.25, 90.0, (-28, 0)),
        ("N33%%d39'42\"W       106.62'", (maxx, miny), (maxx, maxy), 2.25, 90.0, (24, 0)),
        ("C186", (minx, miny), (maxx, miny), 2.25, 0.0, (0, -22)),
    ]
    for value, p1, p2, height, rotation, offset in labels:
        mx = (p1[0] + p2[0]) / 2 + offset[0]
        my = (p1[1] + p2[1]) / 2 + offset[1]
        entities += text("E", 4, mx, my, height, value, rotation=rotation, style="ARCHITECTURAL")

def concave_front(p_left, p_right, sagitta, steps=24):
    """Puntos de un frente curvo entre p_left y p_right combado hacia ADENTRO (arriba).
    Los endpoints definen el minY exacto; los puntos intermedios quedan adentro."""
    (xa, ya), (xb, _) = p_left, p_right
    pts = []
    for k in range(1, steps):
        t = k / steps
        x = xa + (xb - xa) * t
        y = ya + sagitta * math.sin(math.pi * t)
        pts.append((x, y))
    return pts

# --- formas de setback: cada una devuelve lista de vertices (cerrada) con bbox EXACTO ---
def shape_rect(minx, miny, maxx, maxy):
    return [(minx, miny), (maxx, miny), (maxx, maxy), (minx, maxy)]

def shape_chamfer_ne(minx, miny, maxx, maxy, cut=60.0):
    return [(minx, miny), (maxx, miny), (maxx, maxy - cut), (maxx - cut, maxy), (minx, maxy)]

def shape_fillets(minx, miny, maxx, maxy, r=36.0, steps=6):
    def corner(cx, cy, a0, a1):
        return [(cx + r * math.cos(a0 + (a1 - a0) * k / steps),
                 cy + r * math.sin(a0 + (a1 - a0) * k / steps)) for k in range(steps + 1)]
    pts = []
    pts += [(minx + r, miny), (maxx - r, miny)]
    pts += corner(maxx - r, miny + r, -math.pi / 2, 0)          # SE
    pts += [(maxx, miny + r), (maxx, maxy - r)]
    pts += corner(maxx - r, maxy - r, 0, math.pi / 2)           # NE
    pts += [(maxx - r, maxy), (minx + r, maxy)]
    pts += corner(minx + r, maxy - r, math.pi / 2, math.pi)     # NW
    pts += [(minx, maxy - r), (minx, miny + r)]
    pts += corner(minx + r, miny + r, math.pi, 3 * math.pi / 2) # SW
    return pts

def shape_curved_front(minx, miny, maxx, maxy, sagitta=35.0):
    pts = [(minx, miny)]
    pts += concave_front((minx, miny), (maxx, miny), sagitta)
    pts += [(maxx, miny), (maxx, maxy), (minx, maxy)]
    return pts

def shape_chamfer_and_curve(minx, miny, maxx, maxy, cut=50.0, sagitta=30.0):
    pts = [(minx, miny)]
    pts += concave_front((minx, miny), (maxx, miny), sagitta)
    pts += [(maxx, miny), (maxx, maxy - cut), (maxx - cut, maxy), (minx, maxy)]
    return pts

def shape_bounds(pts):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)

def lot_from_setback_shape(setback_pts, side, front, rear):
    """El terreno exterior calca la forma del setback, expandida a los retiros."""
    minx, miny, maxx, maxy = shape_bounds(setback_pts)
    width = max(maxx - minx, 1e-9)
    height = max(maxy - miny, 1e-9)
    lot_minx = minx - side
    lot_maxx = maxx + side
    lot_miny = miny - front
    lot_maxy = maxy + rear
    lot_width = lot_maxx - lot_minx
    lot_height = lot_maxy - lot_miny

    return [
        (
            lot_minx + ((x - minx) / width) * lot_width,
            lot_miny + ((y - miny) / height) * lot_height,
        )
        for x, y in setback_pts
    ]

def format_deficit_label(deficit):
    return str(int(deficit)) if float(deficit).is_integer() else fmt(deficit)

def axis_label(axis):
    if axis == "ancho":
        return "ANCHO"
    if axis == "alto":
        return "ALTO"
    raise ValueError(f"Eje de deficit desconocido: {axis}")

def synth_file_name(axis, deficit, style, short_name):
    return f"SYNTH FALTA {format_deficit_label(deficit)} {axis_label(axis)} - {style} - {short_name}.dxf"

def cleanup_stale_synth_outputs(expected_names):
    """Deja el picker limpio: remueve synths viejos que no tienen titulo descriptivo actual."""
    removed = []
    locked = []
    for file_name in os.listdir(OUT):
        if not file_name.upper().startswith("SYNTH ") or not file_name.lower().endswith(".dxf"):
            continue
        if file_name in expected_names:
            continue
        path = os.path.join(OUT, file_name)
        try:
            os.remove(path)
            removed.append(file_name)
        except PermissionError:
            locked.append(file_name)

    if removed:
        print("Synths viejos removidos del picker:")
        for file_name in removed:
            print(f"  - {file_name}")
    if locked:
        print("WARNING: no pude borrar synths viejos porque estan abiertos/bloqueados:")
        for file_name in locked:
            print(f"  - {file_name}")

CASES = [
    (1, "ancho", 1.0, "RECTANGULAR", shape_rect, {}, "OAK", "101", "OAK STREET"),
    (2, "ancho", 2.0, "CHAFLAN", shape_chamfer_ne, {"cut": 60.0}, "PINE", "214", "PINE WAY"),
    (3, "ancho", 2.0, "FILLETS", shape_fillets, {"r": 36.0}, "CEDAR", "32", "CEDAR COURT"),
    (4, "alto",  1.0, "FRENTE CURVO", shape_curved_front, {"sagitta": 35.0}, "MESA", "8", "MESA LOOP"),
    (5, "alto",  2.0, "RECTANGULAR", shape_rect, {}, "RIO", "57", "RIO DRIVE"),
    (6, "alto",  2.0, "CHAFLAN CURVO", shape_chamfer_and_curve, {"cut": 50.0, "sagitta": 30.0}, "PARK", "16", "PARK LANE"),
]

SIDE, FRONT, REAR = 90.0, 300.0, 240.0  # retiros tipicos: 7.5' / 25' / 20'

print()
results = []
expected_output_names = {
    synth_file_name(axis, deficit, style, short_name)
    for _, axis, deficit, style, _, _, short_name, _, _ in CASES
}
cleanup_stale_synth_outputs(expected_output_names)

for number, axis, deficit, style, shape_fn, kwargs, short_name, house_number, street_name in CASES:
    if deficit > MAX_NORMAL_ADAPTATION_INCHES:
        raise ValueError(
            f"{style} {short_name} pide {deficit}\" de {axis}, "
            f"pero los synths normales no pueden superar {MAX_NORMAL_ADAPTATION_INCHES}\"."
        )

    reset_handles()
    sbw = W - deficit if axis == "ancho" else W
    sbh = H if axis == "ancho" else H - deficit
    minx, miny = 400.0, 400.0
    maxx, maxy = minx + sbw, miny + sbh

    sb_pts = shape_fn(minx, miny, maxx, maxy, **kwargs)

    # El lote exterior calca la misma forma del setback. Si hay chaflan/curva/fillet
    # en el area construible, viene del terreno; no inventamos un rectangulo alrededor.
    lot = lot_from_setback_shape(sb_pts, SIDE, FRONT, REAR)
    lot_minx, lot_miny, lot_maxx, lot_maxy = shape_bounds(lot)

    ents = []
    ents += polyline_lines(PROP_LAYER, 7, lot, closed=True)
    ents += polyline_lines("SETBACKS", 31, sb_pts, closed=True)

    add_survey_labels(ents, lot)
    title_bounds = add_pointe_title_block(ents, lot_minx, lot_miny, lot_maxx, house_number, street_name, number)

    extmin = (min(lot_minx - 200, title_bounds[0] - 100), min(title_bounds[1] - 80, lot_miny - 200))
    extmax = (max(lot_maxx + 200, title_bounds[2] + 100), max(lot_maxy + 200, title_bounds[3] + 80))
    name = synth_file_name(axis, deficit, style, short_name)
    path = os.path.join(OUT, name)
    content = dxf_doc(extmin, extmax, ents)
    try:
        with open(path, "w", newline="\n", encoding="latin1") as f:
            f.write(content)
    except PermissionError:
        fixed_name = name[:-4] + " - AUTOCAD FIXED.dxf"
        path = os.path.join(OUT, fixed_name)
        with open(path, "w", newline="\n", encoding="latin1") as f:
            f.write(content)
        name = fixed_name

    # verificacion: bbox real de las LINEAS setback escritas + pipeline replica
    xs = [c[0] for c in sb_pts]; ys = [c[1] for c in sb_pts]
    bb = (min(xs), min(ys), max(xs), max(ys))
    bw, bh = bb[2] - bb[0], bb[3] - bb[1]
    ox = (bb[0] + bb[2]) / 2 - (x0 + x1) / 2
    oy = (bb[1] + bb[3]) / 2 - (y0 + y1) / 2
    pw = (x0 + ox, x1 + ox, y0 + oy, y1 + oy)
    wdef, hdef = max(0.0, W - bw), max(0.0, H - bh)
    left, right = max(0.0, bb[0] - pw[0]), max(0.0, pw[1] - bb[2])
    bottom, top = max(0.0, bb[1] - pw[2]), max(0.0, pw[3] - bb[3])
    results.append((name, bw, bh, wdef, hdef, left, right, bottom, top))
    print(f"{name}")
    print(f"  buildable bbox {bw:.6f} x {bh:.6f}  ->  Deficit ancho {wdef:.3f}\" alto {hdef:.3f}\"")
    print(f"  sobresalto: izq {left:.3f}  der {right:.3f}  abajo {bottom:.3f}  arriba {top:.3f}")

with open(os.path.join(OUT, "README.txt"), "w", encoding="latin1") as f:
    f.write("Site plans sinteticos con layers y rotulo Pointe (estilo 158 DAWSON STREET)\n")
    f.write("Unidades: pulgadas ($INSUNITS=1). Capas: SETBACKS / " + PROP_LAYER + " / E / TEXT.\n")
    f.write("Los nombres de archivo describen el deficit testeado: FALTA 1/2 ANCHO o FALTA 1/2 ALTO.\n")
    f.write("Los rotulos visibles dentro del CAD siguen siendo cortos/falsos: OAK, PINE, CEDAR, MESA, RIO, PARK.\n")
    f.write(f"Referencia: footprint ESTRUCTURAL SEMINOLE2000 = {fmt(W)}\" x {fmt(H)}\" (masa de pared).\n\n")
    for name, bw, bh, wdef, hdef, left, right, bottom, top in results:
        f.write(f"- {name}\n")
        f.write(f"  buildable {bw:.3f} x {bh:.3f} | deficit W/H = {wdef:.0f}\"/{hdef:.0f}\" | overflow L/R/B/T = {left:.1f}/{right:.1f}/{bottom:.1f}/{top:.1f}\n")
print(f"\nGenerados en: {OUT}")
