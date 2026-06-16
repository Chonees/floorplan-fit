"""Bbox real de lo que se DIBUJA vs lo que se MIDE en el preview de ajuste.

Mide: paths de wall candidates (base del deficit/centrado) vs el conjunto
completo de geometry paths de la sesion (walls + openings + fixed components
+ assemblies + pinch markers) + cotas. Para SEMINOLE2000.
"""
import sqlite3

db = r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan\src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db"
con = sqlite3.connect(db)
cur = con.cursor()

cur.execute("""
    SELECT t.code, v.id, v.version_number, r.id
    FROM floorplan_templates t
    JOIN floorplan_versions v ON v.floorplan_template_id = t.id
    JOIN wall_extraction_runs r ON r.floorplan_version_id = v.id
    WHERE r.status = 'Completed' OR r.status = 'Succeeded' OR r.finished_at_utc IS NOT NULL
    ORDER BY t.code, v.version_number DESC, r.finished_at_utc DESC
""")
runs = cur.fetchall()
print("Runs disponibles:")
seen = set()
latest = {}
for code, vid, vnum, rid in runs:
    print(f"  {code} v{vnum} run={rid}")
    if code not in latest:
        latest[code] = (vid, vnum, rid)

def bbox_of_paths(path_ids):
    if not path_ids:
        return None
    marks = ",".join("?" * len(path_ids))
    cur.execute(f"""
        SELECT MIN(MIN(CAST(start_x AS REAL)), MIN(CAST(end_x AS REAL))),
               MAX(MAX(CAST(start_x AS REAL)), MAX(CAST(end_x AS REAL))),
               MIN(MIN(CAST(start_y AS REAL)), MIN(CAST(end_y AS REAL))),
               MAX(MAX(CAST(start_y AS REAL)), MAX(CAST(end_y AS REAL)))
        FROM geometry_segments WHERE geometry_path_id IN ({marks})
    """, path_ids)
    return cur.fetchone()

for code, (vid, vnum, rid) in latest.items():
    print(f"\n================ {code} v{vnum} (run {rid}) ================")

    cur.execute("SELECT geometry_path_id, status FROM extracted_wall_candidates WHERE wall_extraction_run_id=?", (rid,))
    wall_rows = cur.fetchall()
    wall_ids = [r[0] for r in wall_rows if r[0]]
    print(f"wall candidates: {len(wall_rows)} (con path: {len(wall_ids)})  estados: {set(r[1] for r in wall_rows)}")

    cur.execute("SELECT geometry_path_id FROM extracted_opening_candidates WHERE wall_extraction_run_id=?", (rid,))
    opening_ids = [r[0] for r in cur.fetchall() if r[0]]

    cur.execute("""
        SELECT p.geometry_path_id, c.source_layer FROM extracted_fixed_plan_component_paths p
        JOIN extracted_fixed_plan_components c ON c.id = p.fixed_plan_component_id
        WHERE c.wall_extraction_run_id=?""", (rid,))
    fixed_rows = cur.fetchall()
    fixed_ids = [r[0] for r in fixed_rows]

    cur.execute("""
        SELECT p.geometry_path_id, a.source_layer FROM extracted_protected_detail_assembly_paths p
        JOIN extracted_protected_detail_assemblies a ON a.id = p.protected_detail_assembly_id
        WHERE a.wall_extraction_run_id=?""", (rid,))
    asm_rows = cur.fetchall()
    asm_ids = [r[0] for r in asm_rows]

    wb = bbox_of_paths(wall_ids)
    if not wb:
        print("  (sin paths de pared)")
        continue
    wminx, wmaxx, wminy, wmaxy = wb
    print(f"\nBBOX PAREDES (lo que se MIDE/CENTRA):  X[{wminx:.3f} .. {wmaxx:.3f}] w={wmaxx-wminx:.4f}   Y[{wminy:.3f} .. {wmaxy:.3f}] h={wmaxy-wminy:.4f}")

    for name, ids in [("openings", opening_ids), ("fixed components", fixed_ids), ("assemblies", asm_ids)]:
        b = bbox_of_paths(ids)
        if b:
            minx, maxx, miny, maxy = b
            over = (f"izq {wminx-minx:+.2f}  der {maxx-wmaxx:+.2f}  abajo {wminy-miny:+.2f}  arriba {maxy-wmaxy:+.2f}")
            print(f"  {name:<18} n={len(ids):>4}  X[{minx:.2f}..{maxx:.2f}]  Y[{miny:.2f}..{maxy:.2f}]   sobresale: {over}")

    all_ids = list(dict.fromkeys(wall_ids + opening_ids + fixed_ids + asm_ids))
    b = bbox_of_paths(all_ids)
    minx, maxx, miny, maxy = b
    print(f"\nBBOX TODO LO DIBUJADO (geometry paths): X[{minx:.3f} .. {maxx:.3f}] w={maxx-minx:.4f}   Y[{miny:.3f} .. {maxy:.3f}] h={maxy-miny:.4f}")
    print(f"  sobresale de paredes: izq {wminx-minx:+.3f}  der {maxx-wmaxx:+.3f}  abajo {wminy-miny:+.3f}  arriba {maxy-wmaxy:+.3f}")

    # desglose de fixed components por capa, solo los que sobresalen en X
    from collections import defaultdict
    layer_ids = defaultdict(list)
    for pid, layer in fixed_rows + asm_rows:
        layer_ids[layer].append(pid)
    print("\n  Desglose components/assemblies por capa:")
    for layer, ids in sorted(layer_ids.items()):
        b = bbox_of_paths(ids)
        if b:
            minx, maxx, miny, maxy = b
            print(f"    {layer:<18} n={len(ids):>4}  X[{minx:.2f}..{maxx:.2f}]  Y[{miny:.2f}..{maxy:.2f}]")

    # cotas (se dibujan si ArePreviewDimensionsVisible)
    cur.execute("""SELECT MIN(MIN(CAST(start_x AS REAL)), MIN(CAST(end_x AS REAL))),
                          MAX(MAX(CAST(start_x AS REAL)), MAX(CAST(end_x AS REAL))),
                          MIN(MIN(CAST(start_y AS REAL)), MIN(CAST(end_y AS REAL))),
                          MAX(MAX(CAST(start_y AS REAL)), MAX(CAST(end_y AS REAL)))
                   FROM extracted_dimension_line_segments
                   WHERE dimension_id IN (SELECT id FROM extracted_dimensions WHERE wall_extraction_run_id=?)""", (rid,))
    b = cur.fetchone()
    if b and b[0] is not None:
        minx, maxx, miny, maxy = b
        print(f"\n  BBOX cotas (line segments): X[{minx:.2f}..{maxx:.2f}]  Y[{miny:.2f}..{maxy:.2f}]")
        print(f"    sobresale de paredes: izq {wminx-minx:+.2f}  der {maxx-wmaxx:+.2f}  abajo {wminy-miny:+.2f}  arriba {maxy-wmaxy:+.2f}")
