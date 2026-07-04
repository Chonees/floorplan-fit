"""Verify generated synthetic site plans keep Pointe Homes layer/title appearance.

This is intentionally a tiny DXF group-code verifier instead of a CAD dependency:
the requirement is about the layer table metadata and title-block lettering that
make our synths look like the real Pointe site plans in AutoCAD and in the app.
"""
from __future__ import annotations

import subprocess
import sqlite3
from pathlib import Path


SITE_PLAN_DIR = Path(r"D:\PointAIData\PLANS\originalsSitePlans")
REFERENCE_DXF = SITE_PLAN_DIR / "158 DAWSON STREET.dxf"
ACCORECONSOLE = Path(r"C:\Program Files\Autodesk\AutoCAD LT 2026\accoreconsole.exe")
ACCORE_SCRIPT = Path(".testartifacts/verify-synth-accore.scr")
TITLE_BLOCK_SCALE = 5.0
FLOORPLAN_DB = Path(
    r"C:\Users\lucas\OneDrive\Escritorio\floorplan adjustments-to site plan"
    r"\src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db"
)
FLOORPLAN_WALL_RUN_ID = "c2496c35-ad07-4846-a68c-6bcc34027d69"
STRUCTURAL_BINS = 2048
STRUCTURAL_TAIL = 0.005
MAX_NORMAL_ADAPTATION_INCHES = 4.0

EXPECTED_SYNTH_CASES = {
    "SYNTH FALTA 1 ANCHO - RECTANGULAR - OAK.dxf": {
        "house": "101",
        "street": "OAK STREET",
        "legal": "BEING LOT 1, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "ancho",
        "deficit": 1.0,
    },
    "SYNTH FALTA 2 ANCHO - CHAFLAN - PINE.dxf": {
        "house": "214",
        "street": "PINE WAY",
        "legal": "BEING LOT 2, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "ancho",
        "deficit": 2.0,
    },
    "SYNTH FALTA 2 ANCHO - FILLETS - CEDAR.dxf": {
        "house": "32",
        "street": "CEDAR COURT",
        "legal": "BEING LOT 3, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "ancho",
        "deficit": 2.0,
    },
    "SYNTH FALTA 1 ALTO - FRENTE CURVO - MESA.dxf": {
        "house": "8",
        "street": "MESA LOOP",
        "legal": "BEING LOT 4, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "alto",
        "deficit": 1.0,
    },
    "SYNTH FALTA 2 ALTO - RECTANGULAR - RIO.dxf": {
        "house": "57",
        "street": "RIO DRIVE",
        "legal": "BEING LOT 5, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "alto",
        "deficit": 2.0,
    },
    "SYNTH FALTA 2 ALTO - CHAFLAN CURVO - PARK.dxf": {
        "house": "16",
        "street": "PARK LANE",
        "legal": "BEING LOT 6, BLOCK 25, RANCHO SANTA TERESA UNIT TWO",
        "axis": "alto",
        "deficit": 2.0,
    },
}

REQUIRED_LAYER_NAMES = [
    "0",
    "TEXT",
    "E",
    "SETBACKS",
    "2312-001-BM$0$C-PROP-SUBD",
]

PROPERTY_LAYER = "2312-001-BM$0$C-PROP-SUBD"

REQUIRED_TABLE_NAMES = [
    "VPORT",
    "LTYPE",
    "LAYER",
    "STYLE",
    "VIEW",
    "UCS",
    "APPID",
    "DIMSTYLE",
    "BLOCK_RECORD",
]

def title_height(reference_height: float) -> float:
    return reference_height * TITLE_BLOCK_SCALE


REQUIRED_TITLE_TEXTS = [
    ("SITE PLAN", "E", "SITE", title_height(8.0)),
    ("SCALE 1'=20'", "E", "L80", title_height(1.6)),
    ("(50' WIDE PUBLIC R.O.W.)", "TEXT", "RS", title_height(1.792900433341439)),
    ("CITY OF SUNLAND PARK, DO\\U+00D1A ANA COUNTY, NEW MEXICO", "E", "L80", title_height(1.6)),
    ("CURVE", "E", "", title_height(1.40625)),
    ("RADIUS", "E", "", title_height(1.40625)),
    ("LENGTH", "E", "", title_height(1.40625)),
    ("TANGENT", "E", "", title_height(1.40625)),
    ("CHORD", "E", "", title_height(1.40625)),
    ("BEARING", "E", "", title_height(1.40625)),
    ("DELTA", "E", "", title_height(1.40625)),
]

FORBIDDEN_VISIBLE_TEXT_SNIPPETS = [
    "BUILDABLE BBOX",
    "FOOTPRINT ESTRUCTURAL",
    "SYNTHETIC STREET",
    "ANCHO MENOS",
    "ALTO MENOS",
]


def read_pairs(path: Path) -> list[tuple[str, str]]:
    lines = path.read_text(encoding="latin1", errors="ignore").splitlines()
    return [
        (lines[index].strip(), lines[index + 1].strip())
        for index in range(0, len(lines) - 1, 2)
    ]


def read_layer_records(path: Path) -> dict[str, dict[str, list[str]]]:
    pairs = read_pairs(path)
    layers: dict[str, dict[str, list[str]]] = {}
    in_layer_table = False
    index = 0

    while index < len(pairs):
        code, value = pairs[index]
        if code == "0" and value.upper() == "TABLE":
            index += 1
            record: list[tuple[str, str]] = []
            while index < len(pairs) and pairs[index][0] != "0":
                record.append(pairs[index])
                index += 1

            if any(item_code == "2" and item_value.upper() == "LAYER" for item_code, item_value in record):
                in_layer_table = True
            continue

        if in_layer_table and code == "0" and value.upper() == "ENDTAB":
            break

        if in_layer_table and code == "0" and value.upper() == "LAYER":
            index += 1
            values: dict[str, list[str]] = {}
            while index < len(pairs) and pairs[index][0] != "0":
                item_code, item_value = pairs[index]
                values.setdefault(item_code, []).append(item_value)
                index += 1

            layer_name = first(values, "2")
            if layer_name:
                layers[layer_name] = values
            continue

        index += 1

    return layers


def read_table_names(path: Path) -> list[str]:
    pairs = read_pairs(path)
    table_names: list[str] = []

    for index, (code, value) in enumerate(pairs[:-1]):
        if code != "0" or value.upper() != "TABLE":
            continue

        cursor = index + 1
        while cursor < len(pairs) and pairs[cursor][0] != "0":
            if pairs[cursor][0] == "2":
                table_names.append(pairs[cursor][1])
                break
            cursor += 1

    return table_names


def read_text_records(path: Path) -> list[dict[str, str]]:
    pairs = read_pairs(path)
    records: list[dict[str, str]] = []
    index = 0

    while index < len(pairs):
        code, value = pairs[index]
        if code != "0" or value.upper() not in {"TEXT", "MTEXT"}:
            index += 1
            continue

        record_type = value.upper()
        index += 1
        values: dict[str, list[str]] = {}
        while index < len(pairs) and pairs[index][0] != "0":
            item_code, item_value = pairs[index]
            values.setdefault(item_code, []).append(item_value)
            index += 1

        text_value = "".join(values.get("1", []) + values.get("3", [])).strip()
        records.append(
            {
                "type": record_type,
                "text": text_value,
                "layer": first(values, "8"),
                "style": first(values, "7"),
                "height": first(values, "40"),
            }
        )

    return records


def first(values: dict[str, list[str]], code: str) -> str:
    return values.get(code, [""])[0]


def layer_signature(values: dict[str, list[str]]) -> dict[str, str]:
    return {
        "color": first(values, "62"),
        "linetype": first(values, "6"),
        "lineweight": first(values, "370"),
        "plotstyle": first(values, "390"),
        "material": first(values, "347"),
        "shadow": first(values, "348"),
    }


def has_text(
    records: list[dict[str, str]],
    text_value: str,
    layer: str,
    style: str,
    height: float,
) -> bool:
    for record in records:
        if record["text"].rstrip() != text_value:
            continue
        if record["layer"] != layer:
            continue
        if record["style"] != style:
            continue
        try:
            actual_height = float(record["height"])
        except ValueError:
            continue
        if abs(actual_height - height) <= 1e-6:
            return True

    return False


def has_any_survey_bearing_label(records: list[dict[str, str]]) -> bool:
    return any(
        (
            ("°" in record["text"] or "%%d" in record["text"])
            and "'" in record["text"]
            and record["style"] == "ARCHITECTURAL"
        )
        for record in records
    )


def count_entities_on_layer(path: Path, layer_name: str) -> int:
    pairs = read_pairs(path)
    count = 0
    index = 0
    while index < len(pairs):
        code, value = pairs[index]
        if code != "0" or value.upper() in {"SECTION", "ENDSEC", "EOF", "TABLE", "ENDTAB", "LAYER", "LTYPE", "STYLE"}:
            index += 1
            continue

        index += 1
        found_layer = False
        while index < len(pairs) and pairs[index][0] != "0":
            if pairs[index] == ("8", layer_name):
                found_layer = True
            index += 1

        if found_layer:
            count += 1

    return count


def line_vertices_on_layer(path: Path, layer_name: str) -> list[tuple[float, float]]:
    pairs = read_pairs(path)
    vertices: list[tuple[float, float]] = []
    index = 0

    while index < len(pairs):
        code, value = pairs[index]
        if code != "0" or value.upper() != "LINE":
            index += 1
            continue

        index += 1
        values: dict[str, list[str]] = {}
        while index < len(pairs) and pairs[index][0] != "0":
            item_code, item_value = pairs[index]
            values.setdefault(item_code, []).append(item_value)
            index += 1

        if first(values, "8") != layer_name:
            continue

        try:
            vertices.append((float(first(values, "10")), float(first(values, "20"))))
        except ValueError:
            continue

    return vertices


def setback_size(path: Path) -> tuple[float, float]:
    vertices = line_vertices_on_layer(path, "SETBACKS")
    if not vertices:
        raise ValueError(f"{path.name}: missing SETBACKS vertices")

    xs = [vertex[0] for vertex in vertices]
    ys = [vertex[1] for vertex in vertices]
    return max(xs) - min(xs), max(ys) - min(ys)


def read_structural_wall_segments() -> list[tuple[float, float, float, float]]:
    if not FLOORPLAN_DB.exists():
        raise FileNotFoundError(f"Floor-plan workspace DB not found: {FLOORPLAN_DB}")

    with sqlite3.connect(FLOORPLAN_DB) as connection:
        cursor = connection.cursor()
        cursor.execute(
            """
            SELECT
                CAST(s.start_x AS REAL),
                CAST(s.start_y AS REAL),
                CAST(s.end_x AS REAL),
                CAST(s.end_y AS REAL)
            FROM extracted_wall_candidates w
            JOIN geometry_segments s ON s.geometry_path_id = w.geometry_path_id
            WHERE w.wall_extraction_run_id = ?
            """,
            (FLOORPLAN_WALL_RUN_ID,),
        )
        segments = cursor.fetchall()

    if not segments:
        raise ValueError(f"No structural wall segments found for run {FLOORPLAN_WALL_RUN_ID}")

    return segments


def structural_extent(
    segments: list[tuple[float, float, float, float]],
    horizontal: bool,
) -> tuple[float, float]:
    projected: list[tuple[float, float, float]] = []
    minimum = float("inf")
    maximum = float("-inf")

    for start_x, start_y, end_x, end_y in segments:
        start, end = (
            (min(start_x, end_x), max(start_x, end_x))
            if horizontal
            else (min(start_y, end_y), max(start_y, end_y))
        )
        length = ((end_x - start_x) ** 2 + (end_y - start_y) ** 2) ** 0.5
        projected.append((start, end, length))
        minimum = min(minimum, start)
        maximum = max(maximum, end)

    step = (maximum - minimum) / STRUCTURAL_BINS
    mass = [0.0] * (STRUCTURAL_BINS + 1)
    total = 0.0

    for start, end, length in projected:
        if length <= 0:
            continue

        total += length
        if end - start <= step:
            bucket = min(max(int((start - minimum) / step), 0), STRUCTURAL_BINS)
            mass[bucket] += length
            continue

        density = length / (end - start)
        first_bucket = min(max(int((start - minimum) / step), 0), STRUCTURAL_BINS)
        last_bucket = min(max(int((end - minimum) / step), 0), STRUCTURAL_BINS)
        for bucket in range(first_bucket, last_bucket + 1):
            bucket_start = minimum + bucket * step
            overlap = min(end, bucket_start + step) - max(start, bucket_start)
            if overlap > 0:
                mass[bucket] += density * overlap

    tail = STRUCTURAL_TAIL * total
    accumulated = 0.0
    low_bucket = 0
    for bucket in range(STRUCTURAL_BINS + 1):
        accumulated += mass[bucket]
        if accumulated > tail:
            low_bucket = bucket
            break

    accumulated = 0.0
    high_bucket = STRUCTURAL_BINS
    for bucket in range(STRUCTURAL_BINS, -1, -1):
        accumulated += mass[bucket]
        if accumulated > tail:
            high_bucket = bucket
            break

    low_threshold = minimum + low_bucket * step
    high_threshold = minimum + (high_bucket + 1) * step
    concrete_minimum = None
    concrete_maximum = None

    for start, end, _ in projected:
        for coordinate in (start, end):
            if low_threshold - 1e-6 <= coordinate <= high_threshold + 1e-6:
                concrete_minimum = (
                    coordinate if concrete_minimum is None else min(concrete_minimum, coordinate)
                )
                concrete_maximum = (
                    coordinate if concrete_maximum is None else max(concrete_maximum, coordinate)
                )

    if concrete_minimum is None or concrete_maximum is None:
        raise ValueError("Could not resolve structural footprint extent from wall candidates")

    return concrete_minimum, concrete_maximum


def structural_footprint_size() -> tuple[float, float]:
    segments = read_structural_wall_segments()
    min_x, max_x = structural_extent(segments, horizontal=True)
    min_y, max_y = structural_extent(segments, horizontal=False)
    return max_x - min_x, max_y - min_y


def normalized_shape(vertices: list[tuple[float, float]]) -> list[tuple[float, float]]:
    xs = [vertex[0] for vertex in vertices]
    ys = [vertex[1] for vertex in vertices]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    width = max(max_x - min_x, 1e-9)
    height = max(max_y - min_y, 1e-9)
    return [
        ((vertex[0] - min_x) / width, (vertex[1] - min_y) / height)
        for vertex in vertices
    ]


def property_boundary_matches_setback_shape(path: Path) -> tuple[bool, str]:
    property_vertices = line_vertices_on_layer(path, PROPERTY_LAYER)
    setback_vertices = line_vertices_on_layer(path, "SETBACKS")

    if not property_vertices:
        return False, "missing LINE vertices on property boundary layer"
    if not setback_vertices:
        return False, "missing LINE vertices on SETBACKS layer"
    if len(property_vertices) != len(setback_vertices):
        return (
            False,
            f"property boundary has {len(property_vertices)} segment(s) but SETBACKS has {len(setback_vertices)}",
        )

    property_shape = normalized_shape(property_vertices)
    setback_shape = normalized_shape(setback_vertices)
    tolerance = 1e-5
    for index, (property_vertex, setback_vertex) in enumerate(zip(property_shape, setback_shape)):
        if (
            abs(property_vertex[0] - setback_vertex[0]) > tolerance
            or abs(property_vertex[1] - setback_vertex[1]) > tolerance
        ):
            return (
                False,
                f"normalized vertex {index} differs: property={property_vertex}, setback={setback_vertex}",
            )

    return True, "property boundary matches SETBACKS shape"


def run_autocad_audit(path: Path) -> tuple[bool, str]:
    if not ACCORECONSOLE.exists():
        return True, "AutoCAD Core Console not installed; skipped"

    ACCORE_SCRIPT.parent.mkdir(parents=True, exist_ok=True)
    ACCORE_SCRIPT.write_text("_AUDIT\nY\n_ZOOM\n_E\n_QSAVE\n_QUIT\n", encoding="ascii")

    completed = subprocess.run(
        [
            str(ACCORECONSOLE),
            "/i",
            str(path),
            "/s",
            str(ACCORE_SCRIPT),
            "/l",
            "en-US",
        ],
        cwd=Path.cwd(),
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=60,
        check=False,
    )
    output = completed.stdout.decode("utf-16-le", errors="ignore")
    if completed.returncode == 0:
        return True, "AutoCAD Core Console audit passed"

    interesting_lines = [
        line.strip()
        for line in output.splitlines()
        if "Error" in line or "Invalid" in line or "discarded" in line or "DXF" in line
    ]
    summary = " | ".join(interesting_lines[-6:]) if interesting_lines else f"exit={completed.returncode}"
    return False, summary


def main() -> int:
    if not REFERENCE_DXF.exists():
        print(f"FAIL: reference DXF not found: {REFERENCE_DXF}")
        return 1

    reference_layers = read_layer_records(REFERENCE_DXF)
    expected = {
        layer_name: layer_signature(reference_layers[layer_name])
        for layer_name in REQUIRED_LAYER_NAMES
    }

    synth_files = [SITE_PLAN_DIR / file_name for file_name in EXPECTED_SYNTH_CASES]
    missing_files = [synth_file.name for synth_file in synth_files if not synth_file.exists()]
    if missing_files:
        print(f"FAIL: missing expected descriptive synth file(s): {', '.join(missing_files)}")
        return 1

    structural_width, structural_height = structural_footprint_size()

    failures: list[str] = []
    for synth_file in synth_files:
        expected_case = EXPECTED_SYNTH_CASES[synth_file.name]
        table_names = read_table_names(synth_file)
        missing_tables = [table_name for table_name in REQUIRED_TABLE_NAMES if table_name not in table_names]
        if missing_tables:
            failures.append(f"{synth_file.name}: missing required Pointe table(s): {', '.join(missing_tables)}")

        layers = read_layer_records(synth_file)
        for layer_name, expected_signature in expected.items():
            if layer_name not in layers:
                failures.append(f"{synth_file.name}: missing layer {layer_name}")
                continue

            actual_signature = layer_signature(layers[layer_name])
            if actual_signature != expected_signature:
                failures.append(
                    f"{synth_file.name}: layer {layer_name} expected {expected_signature}, got {actual_signature}"
                )

        if count_entities_on_layer(synth_file, "SETBACKS") == 0:
            failures.append(f"{synth_file.name}: no entities on SETBACKS layer")

        shape_ok, shape_summary = property_boundary_matches_setback_shape(synth_file)
        if not shape_ok:
            failures.append(f"{synth_file.name}: property boundary must trace SETBACKS shape: {shape_summary}")

        buildable_width, buildable_height = setback_size(synth_file)
        width_deficit = max(0.0, structural_width - buildable_width)
        height_deficit = max(0.0, structural_height - buildable_height)
        largest_deficit = max(width_deficit, height_deficit)
        tolerance = 0.01
        if largest_deficit > MAX_NORMAL_ADAPTATION_INCHES + tolerance:
            failures.append(
                f"{synth_file.name}: normal synth deficit exceeds {MAX_NORMAL_ADAPTATION_INCHES:g}\" "
                f"(width={width_deficit:.3f}\", height={height_deficit:.3f}\")"
            )

        expected_deficit = float(expected_case["deficit"])
        expected_axis = str(expected_case["axis"])
        if expected_axis == "ancho":
            if abs(width_deficit - expected_deficit) > tolerance or height_deficit > tolerance:
                failures.append(
                    f"{synth_file.name}: expected {expected_deficit:g}\" width deficit only, "
                    f"got width={width_deficit:.3f}\" height={height_deficit:.3f}\""
                )
        elif expected_axis == "alto":
            if abs(height_deficit - expected_deficit) > tolerance or width_deficit > tolerance:
                failures.append(
                    f"{synth_file.name}: expected {expected_deficit:g}\" height deficit only, "
                    f"got width={width_deficit:.3f}\" height={height_deficit:.3f}\""
                )
        else:
            failures.append(f"{synth_file.name}: unknown expected axis {expected_axis!r}")

        text_records = read_text_records(synth_file)
        house_number = str(expected_case["house"])
        street_name = str(expected_case["street"])
        legal_line = str(expected_case["legal"])
        file_required_texts = [
            (house_number, "E", "HOUSE", title_height(5.203124999999999)),
            (street_name, "E", "HOUSE", title_height(5.203124999999999)),
            (legal_line, "E", "L80", title_height(1.6)),
            *REQUIRED_TITLE_TEXTS,
        ]
        for text_value, layer, style, height in file_required_texts:
            if not has_text(text_records, text_value, layer, style, height):
                failures.append(
                    f"{synth_file.name}: missing title text {text_value!r} "
                    f"on layer={layer!r} style={style!r} height={height}"
                )

        visible_text = "\n".join(record["text"].upper() for record in text_records)
        for forbidden_snippet in FORBIDDEN_VISIBLE_TEXT_SNIPPETS:
            if forbidden_snippet in visible_text:
                failures.append(f"{synth_file.name}: contains diagnostic text {forbidden_snippet!r}")

        if not has_any_survey_bearing_label(text_records):
            failures.append(f"{synth_file.name}: missing ARCHITECTURAL survey bearing labels")

        audit_ok, audit_summary = run_autocad_audit(synth_file)
        if not audit_ok:
            failures.append(f"{synth_file.name}: AutoCAD Core Console rejected DXF: {audit_summary}")

    if failures:
        print("FAIL: synthetic site-plan layer verification failed")
        for failure in failures:
            print(f" - {failure}")
        return 1

    print(
        f"PASS: {len(synth_files)} synthetic site plan(s) match Pointe layer/title appearance "
        f"and expected <= {MAX_NORMAL_ADAPTATION_INCHES:g}\" deficits"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
