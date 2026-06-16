"""Replica EXACTA del camino SitePlanAdjustmentPreviewProjector.Build con datos reales.

Carga la sesion real desde app.db (version current, run, curacion activa),
replica la seleccion de geometry paths de la sesion, el ResolvePlacementGeometryPaths
(con su fallback), el ResolveFloorToSiteScale y el centrado, y reporta donde caen
las PAREDES proyectadas vs el buildable del DXF "ancho menos 1 inch".
"""
import sqlite3

DB = r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan\src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db"
EXAMPLE = r"C:\Users\lucas\OneDrive\Escritorio\exports\setback ejemplo 01 - DEFICIT TOTAL - ancho menos 1 inch.dxf"

con = sqlite3.connect(DB)
cur = con.cursor()

print("=== TEMPLATES / VERSIONES / RUNS ===")
cur.execute("""
SELECT t.code, t.current_version_id, t.active_published_curation_id, v.id, v.version_number, v.deleted_at_utc
FROM floorplan_templates t JOIN floorplan_versions v ON v.floorplan_template_id = t.id
ORDER BY t.code, v.version_number""")
rows = cur.fetchall()
current_version = None
template_curation = None
for code, cur_vid, active_cur, vid, vnum, deleted in rows:
    is_current = "  <-- CURRENT" if vid == cur_vid else ""
    print(f"  {code} v{vnum} id={vid} deleted={deleted}{is_current}")
    if vid == cur_vid and code.lower().startswith("seminole"):
        current_version = vid
        template_curation = active_cur

cur.execute("SELECT id, floorplan_version_id, status, finished_at_utc, error_message FROM wall_extraction_runs ORDER BY started_at_utc")
print("\n=== RUNS ===")
runs_by_version = {}
for rid, vid, status, fin, err in cur.fetchall():
    print(f"  run={rid} version={vid} status={status} finished={fin} err={err}")
    runs_by_version.setdefault(vid, []).append(rid)

if current_version is None:
    raise SystemExit("No hay version current de seminole")

run_ids = runs_by_version.get(current_version, [])
print(f"\nVersion current: {current_version}  runs: {run_ids}")
rid = run_ids[-1]

cur.execute("""SELECT id, status, curation_version FROM floorplan_curations
               WHERE floorplan_version_id=? ORDER BY curation_version""", (current_version,))
curations = cur.fetchall()
print(f"Curaciones de la version: {curations}")
print(f"Active published curation (template): {template_curation}")
active_curation = template_curation

def fetch_ids(sql, args):
    cur.execute(sql, args)
    return [r[0] for r in cur.fetchall() if r[0]]

wall_path_ids = fetch_ids("SELECT geometry_path_id FROM extracted_wall_candidates WHERE wall_extraction_run_id=?", (rid,))
opening_ids = fetch_ids("SELECT geometry_path_id FROM extracted_opening_candidates WHERE wall_extraction_run_id=?", (rid,))
fixed_ids = fetch_ids("""SELECT p.geometry_path_id FROM extracted_fixed_plan_component_paths p
    JOIN extracted_fixed_plan_components c ON c.id=p.fixed_plan_component_id WHERE c.wall_extraction_run_id=?""", (rid,))
asm_ids = fetch_ids("""SELECT p.geometry_path_id FROM extracted_protected_detail_assembly_paths p
    JOIN extracted_protected_detail_assemblies a ON a.id=p.protected_detail_assembly_id WHERE a.wall_extraction_run_id=?""", (rid,))
marker_ids = []
if active_curation:
    marker_ids = fetch_ids("SELECT geometry_path_id FROM pinch_markers WHERE floorplan_curation_id=?", (active_curation,))

session_ids = list(dict.fromkeys(wall_path_ids + opening_ids + fixed_ids + asm_ids + marker_ids))
print(f"\nPaths sesion: walls={len(wall_path_ids)} openings={len(opening_ids)} fixed={len(fixed_ids)} asm={len(asm_ids)} markers={len(marker_ids)} total unicos={len(session_ids)}")

def bbox(ids):
    if not ids:
        return None
    marks = ",".join("?" * len(ids))
    cur.execute(f"""SELECT MIN(MIN(CAST(start_x AS REAL)),MIN(CAST(end_x AS REAL))),
                           MAX(MAX(CAST(start_x AS REAL)),MAX(CAST(end_x AS REAL))),
                           MIN(MIN(CAST(start_y AS REAL)),MIN(CAST(end_y AS REAL))),
                           MAX(MAX(CAST(start_y AS REAL)),MAX(CAST(end_y AS REAL)))
                    FROM geometry_segments WHERE geometry_path_id IN ({marks})""", ids)
    r = cur.fetchone()
    return None if r[0] is None else r

# Replica ResolvePlacementGeometryPaths: placement = wall candidate ids presentes en la sesion
session_set = set(session_ids)
placement = [pid for pid in wall_path_ids if pid in session_set]
print(f"Placement paths que matchean la sesion: {len(placement)} de {len(wall_path_ids)} wall candidates")
used = placement if placement else session_ids
fallback = not placement
print(f"FALLBACK a todos los paths: {fallback}")

fb = bbox(used)
minx, maxx, miny, maxy = fb
print(f"\nbbox usado para CENTRAR: X[{minx:.4f}..{maxx:.4f}] w={maxx-minx:.4f}  Y[{miny:.4f}..{maxy:.4f}] h={maxy-miny:.4f}")

wb = bbox([p for p in wall_path_ids if p in session_set] or wall_path_ids)
print(f"bbox PAREDES:            X[{wb[0]:.4f}..{wb[1]:.4f}] w={wb[1]-wb[0]:.4f}  Y[{wb[2]:.4f}..{wb[3]:.4f}] h={wb[3]-wb[2]:.4f}")

ab = bbox(session_ids)
print(f"bbox TODO dibujado:      X[{ab[0]:.4f}..{ab[1]:.4f}] w={ab[1]-ab[0]:.4f}  Y[{ab[2]:.4f}..{ab[3]:.4f}] h={ab[3]-ab[2]:.4f}")

# Measurement context del floor plan
cur.execute("""SELECT m.source_unit, m.to_millimeters_factor FROM imported_documents d
    JOIN floorplan_versions v ON v.imported_document_id = d.id
    JOIN measurement_contexts m ON m.id = d.measurement_context_id WHERE v.id=?""", (current_version,))
mc = cur.fetchone()
print(f"\nMeasurementContext floor plan: {mc}")
floor_factor = float(mc[1]) if mc else None

# Parse del DXF ejemplo: INSUNITS + bbox setback
def parse_site(path):
    with open(path, "r", errors="replace") as f:
        lines = [l.rstrip("\r\n") for l in f]
    insunits = None
    measurement = None
    i = 0
    while i < len(lines) - 1:
        if lines[i].strip() == "9" and lines[i+1].strip() == "$INSUNITS":
            insunits = int(lines[i+3].strip())
        if lines[i].strip() == "9" and lines[i+1].strip() == "$MEASUREMENT":
            measurement = int(lines[i+3].strip())
        if lines[i].strip() == "2" and lines[i+1].strip() == "ENTITIES":
            break
        i += 2
    xs, ys = [], []
    current_layer = ""
    etype = None
    pend = {}
    while i < len(lines) - 1:
        code, value = lines[i].strip(), lines[i+1].strip()
        i += 2
        if code == "0":
            if value == "ENDSEC":
                break
            etype = value
            current_layer = ""
        elif code == "8":
            current_layer = value
        elif code in ("10", "11", "20", "21") and "SETBACK" == current_layer.upper() and etype == "LINE":
            v = float(value)
            (xs if code in ("10", "11") else ys).append(v)
    return insunits, measurement, (min(xs), max(xs), min(ys), max(ys))

insunits, measurement, sb = parse_site(EXAMPLE)
unit_factor = {1: 25.4, 2: 304.8, 4: 1.0, 5: 10.0, 6: 1000.0}.get(insunits or 0)
if unit_factor is None:
    unit_factor = 25.4 if measurement == 0 else (1.0 if measurement == 1 else 1.0)
print(f"\nDXF ejemplo: INSUNITS={insunits} MEASUREMENT={measurement} -> site_factor={unit_factor}")
print(f"Buildable (bbox setback): X[{sb[0]}..{sb[1]}] w={sb[1]-sb[0]:.4f}  Y[{sb[2]}..{sb[3]}] h={sb[3]-sb[2]:.4f}")

scale = (floor_factor / unit_factor) if floor_factor and unit_factor else 1.0
bcx, bcy = (sb[0]+sb[1])/2, (sb[2]+sb[3])/2
fcx, fcy = (minx+maxx)/2*scale, (miny+maxy)/2*scale
ox, oy = bcx - fcx, bcy - fcy
print(f"\nscale={scale}  offset=({ox:.4f}, {oy:.4f})")

pw = (wb[0]*scale+ox, wb[1]*scale+ox, wb[2]*scale+oy, wb[3]*scale+oy)
print(f"\nPAREDES proyectadas: X[{pw[0]:.4f}..{pw[1]:.4f}]  Y[{pw[2]:.4f}..{pw[3]:.4f}]")
print(f"vs naranja:          X[{sb[0]}..{sb[1]}]          Y[{sb[2]}..{sb[3]}]")
print(f"\nSOBRESALTO PAREDES vs NARANJA:")
print(f"  izquierda: {sb[0]-pw[0]:+.4f}\"  (positivo = muro AFUERA del naranja)")
print(f"  derecha:   {pw[1]-sb[1]:+.4f}\"")
print(f"  abajo:     {sb[2]-pw[2]:+.4f}\"")
print(f"  arriba:    {pw[3]-sb[3]:+.4f}\"")
