"""Export training-ready floor-plan dimension binding examples.

V1 is intentionally boring: read the local SQLite app workspace, write JSONL
examples plus an audit file. No app build, no ML dependency, no magic.
"""

from __future__ import annotations

import argparse
import json
import sqlite3
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCHEMA_VERSION = "floorplan_dimension_binding.v1"
DOCUMENT_KIND = "floor_plan"
FUTURE_DOCUMENT_KINDS = ["electrical_plan", "facade"]
ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DB = ROOT / "src" / "FloorplanFit.Desktop" / "bin" / "Debug" / "net10.0" / "workspace" / "app.db"
DEFAULT_OUT = ROOT / "artifacts" / "training-datasets" / "floorplan-bindings-v1"

CURATION_STATUS = {1: "Draft", 2: "Published", 3: "Superseded"}
AXIS_TAG = {1: "Width", 2: "Height"}
SOURCE_UNIT = {0: "unknown", 1: "millimeter", 2: "centimeter", 3: "meter", 4: "inch", 5: "foot"}


def main() -> int:
    args = parse_args()
    db_path = args.db.resolve()
    out_dir = args.out.resolve()

    if not db_path.exists():
        raise SystemExit(f"Database not found: {db_path}")

    out_dir.mkdir(parents=True, exist_ok=True)

    generated_at = datetime.now(timezone.utc).isoformat()
    with sqlite3.connect(db_path) as connection:
        connection.row_factory = sqlite3.Row
        bundle = load_bundle(connection, published_only=args.published_only)
        records, audit = build_records(bundle, db_path, generated_at)

    jsonl_path = out_dir / "floorplan_dimension_bindings.v1.jsonl"
    write_jsonl(jsonl_path, records)

    audit_path = out_dir / "audit.json"
    audit["files"] = {
        "jsonl": jsonl_path.name,
        "audit": audit_path.name,
        "manifest": "manifest.json",
    }
    write_json(audit_path, audit)

    manifest = {
        "schema_version": SCHEMA_VERSION,
        "dataset_role": "dimension_node_binding",
        "primary_document_kind": DOCUMENT_KIND,
        "future_document_kinds": FUTURE_DOCUMENT_KINDS,
        "generated_at_utc": generated_at,
        "source_database": str(db_path),
        "published_only": args.published_only,
        "files": audit["files"],
        "counts": {
            "exported_records": audit["exported_records"],
            "accepted_binding_records": audit["accepted_binding_records"],
            "unbound_dimension_records": audit["unbound_dimension_records"],
            "hard_error_count": audit["hard_error_count"],
        },
        "notes": [
            "V1 exports floor-plan dimension-to-measurement-node supervision.",
            "Electrical plans and facades will use document_kind, not a breaking schema fork.",
            "Published curations are training-approved; draft curations are exported with quality warnings.",
        ],
    }
    write_json(out_dir / "manifest.json", manifest)

    print(
        f"Exported {audit['exported_records']} records "
        f"({audit['accepted_binding_records']} accepted, {audit['unbound_dimension_records']} unbound) "
        f"to {jsonl_path}"
    )
    print(f"Audit hard errors: {audit['hard_error_count']}")
    return 0 if audit["hard_error_count"] == 0 else 1


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export Floorplan Fit training JSONL.")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB, help="Path to app.db.")
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT, help="Output directory.")
    parser.add_argument(
        "--published-only",
        action="store_true",
        help="Export only published curations. Default exports drafts too, flagged in quality.",
    )
    return parser.parse_args()


def load_bundle(connection: sqlite3.Connection, *, published_only: bool) -> dict[str, Any]:
    curations = fetch_curations(connection, published_only=published_only)
    latest_runs = fetch_latest_completed_runs(connection)
    dimensions_by_run = fetch_dimensions_by_run(connection)
    primitives_by_dimension = fetch_primitives_by_dimension(connection)
    corridors_by_curation = fetch_by(connection, "measurement_corridors", "floorplan_curation_id")
    nodes_by_curation = fetch_by(connection, "measurement_nodes", "floorplan_curation_id")
    bindings_by_curation = fetch_by(connection, "floorplan_dimension_interval_bindings", "floorplan_curation_id")

    return {
        "curations": curations,
        "latest_runs": latest_runs,
        "dimensions_by_run": dimensions_by_run,
        "primitives_by_dimension": primitives_by_dimension,
        "corridors_by_curation": corridors_by_curation,
        "nodes_by_curation": nodes_by_curation,
        "bindings_by_curation": bindings_by_curation,
    }


def fetch_curations(connection: sqlite3.Connection, *, published_only: bool) -> list[dict[str, Any]]:
    where = "WHERE c.status = 2" if published_only else ""
    return rows(
        connection,
        f"""
        SELECT
            c.id AS curation_id,
            c.curation_version,
            c.status AS curation_status,
            c.based_on_curation_id,
            c.notes AS curation_notes,
            c.created_at_utc AS curation_created_at_utc,
            c.published_at_utc,
            v.id AS floorplan_version_id,
            v.version_number,
            v.geometry_fingerprint,
            v.created_at_utc AS version_created_at_utc,
            t.id AS template_id,
            t.code AS template_code,
            t.name AS template_name,
            t.active_published_curation_id,
            d.id AS imported_document_id,
            d.document_type,
            d.original_file_name,
            d.storage_path,
            d.sha256,
            d.dxf_version,
            d.imported_at_utc,
            m.id AS measurement_context_id,
            m.source_unit,
            m.to_millimeters_factor,
            m.linear_tolerance_mm,
            m.angular_tolerance_deg,
            m.created_at_utc AS measurement_context_created_at_utc
        FROM floorplan_curations c
        JOIN floorplan_versions v ON v.id = c.floorplan_version_id
        JOIN floorplan_templates t ON t.id = v.floorplan_template_id
        JOIN imported_documents d ON d.id = v.imported_document_id
        JOIN measurement_contexts m ON m.id = d.measurement_context_id
        {where}
        ORDER BY t.code, v.version_number, c.curation_version
        """,
    )


def fetch_latest_completed_runs(connection: sqlite3.Connection) -> dict[str, dict[str, Any]]:
    latest: dict[str, dict[str, Any]] = {}
    for run in rows(
        connection,
        """
        SELECT *
        FROM wall_extraction_runs
        WHERE status = 'Completed'
        ORDER BY floorplan_version_id, started_at_utc DESC, id DESC
        """,
    ):
        latest.setdefault(run["floorplan_version_id"], run)
    return latest


def fetch_dimensions_by_run(connection: sqlite3.Connection) -> dict[str, list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in rows(connection, "SELECT * FROM extracted_dimensions ORDER BY wall_extraction_run_id, sort_order, id"):
        grouped[item["wall_extraction_run_id"]].append(item)
    return grouped


def fetch_primitives_by_dimension(connection: sqlite3.Connection) -> dict[str, list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in rows(
        connection,
        "SELECT * FROM extracted_dimension_primitives ORDER BY dimension_id, sort_order, primitive_key",
    ):
        grouped[item["dimension_id"]].append(item)
    return grouped


def fetch_by(connection: sqlite3.Connection, table: str, key: str) -> dict[str, list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in rows(connection, f"SELECT * FROM {table} ORDER BY {key}, rowid"):
        grouped[item[key]].append(item)
    return grouped


def rows(connection: sqlite3.Connection, sql: str) -> list[dict[str, Any]]:
    return [dict(row) for row in connection.execute(sql)]


def build_records(
    bundle: dict[str, Any],
    db_path: Path,
    generated_at: str,
) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    records: list[dict[str, Any]] = []
    issue_counts: Counter[str] = Counter()
    status_counts: Counter[str] = Counter()
    document_kind_counts: Counter[str] = Counter()

    for curation in bundle["curations"]:
        status_name = curation_status_name(curation["curation_status"])
        status_counts[status_name] += 1
        document_kind_counts[DOCUMENT_KIND] += 1

        run = bundle["latest_runs"].get(curation["floorplan_version_id"])
        if run is None:
            issue_counts["missing_extraction_run"] += 1
            continue

        dimensions = bundle["dimensions_by_run"].get(run["id"], [])
        dimensions_by_id = {item["id"]: item for item in dimensions}
        corridors = bundle["corridors_by_curation"].get(curation["curation_id"], [])
        nodes = bundle["nodes_by_curation"].get(curation["curation_id"], [])
        bindings = bundle["bindings_by_curation"].get(curation["curation_id"], [])
        corridors_by_id = {item["id"]: item for item in corridors}
        nodes_by_id = {item["id"]: item for item in nodes}
        bindings_by_dimension = {item["dimension_id"]: item for item in bindings}

        for binding in bindings:
            dimension = dimensions_by_id.get(binding["dimension_id"])
            corridor = corridors_by_id.get(binding["corridor_id"])
            start_node = nodes_by_id.get(binding["start_node_id"])
            end_node = nodes_by_id.get(binding["end_node_id"])
            quality = build_quality(
                status_name,
                binding=binding,
                dimension=dimension,
                corridor=corridor,
                start_node=start_node,
                end_node=end_node,
            )
            for issue in quality["hard_errors"]:
                issue_counts[issue] += 1

            if dimension is None:
                continue

            records.append(
                build_record(
                    generated_at=generated_at,
                    example_kind="accepted_binding",
                    curation=curation,
                    run=run,
                    dimension=dimension,
                    primitives=bundle["primitives_by_dimension"].get(dimension["id"], []),
                    label=build_label(binding, corridor, start_node, end_node, nodes),
                    quality=quality,
                )
            )

        for dimension in dimensions:
            if dimension["id"] in bindings_by_dimension:
                continue

            records.append(
                build_record(
                    generated_at=generated_at,
                    example_kind="unbound_dimension",
                    curation=curation,
                    run=run,
                    dimension=dimension,
                    primitives=bundle["primitives_by_dimension"].get(dimension["id"], []),
                    label=None,
                    quality={
                        "is_training_approved": status_name == "Published",
                        "curation_status": status_name,
                        "hard_errors": [],
                        "warnings": draft_warnings(status_name),
                        "negative_reason": "no_manual_verified_dimension_interval_binding_in_curation",
                    },
                )
            )

    accepted = sum(1 for record in records if record["example_kind"] == "accepted_binding")
    unbound = sum(1 for record in records if record["example_kind"] == "unbound_dimension")
    audit = {
        "schema_version": SCHEMA_VERSION,
        "dataset_role": "dimension_node_binding",
        "primary_document_kind": DOCUMENT_KIND,
        "future_document_kinds": FUTURE_DOCUMENT_KINDS,
        "generated_at_utc": generated_at,
        "source_database": str(db_path),
        "curation_count": len(bundle["curations"]),
        "curation_status_counts": dict(sorted(status_counts.items())),
        "document_kind_counts": dict(sorted(document_kind_counts.items())),
        "exported_records": len(records),
        "accepted_binding_records": accepted,
        "unbound_dimension_records": unbound,
        "issue_counts": dict(sorted(issue_counts.items())),
        "hard_error_count": sum(issue_counts.values()),
        "warnings": build_audit_warnings(status_counts),
    }
    return records, audit


def build_record(
    *,
    generated_at: str,
    example_kind: str,
    curation: dict[str, Any],
    run: dict[str, Any],
    dimension: dict[str, Any],
    primitives: list[dict[str, Any]],
    label: dict[str, Any] | None,
    quality: dict[str, Any],
) -> dict[str, Any]:
    return {
        "schema_version": SCHEMA_VERSION,
        "document_kind": DOCUMENT_KIND,
        "example_kind": example_kind,
        "generated_at_utc": generated_at,
        "plan": build_plan(curation, run),
        "dimension": build_dimension(dimension),
        "dimension_primitives": [strip_none(item) for item in primitives],
        "label": label,
        "quality": quality,
    }


def build_plan(curation: dict[str, Any], run: dict[str, Any]) -> dict[str, Any]:
    return {
        "template": {
            "id": curation["template_id"],
            "code": curation["template_code"],
            "name": curation["template_name"],
        },
        "version": {
            "id": curation["floorplan_version_id"],
            "version_number": curation["version_number"],
            "geometry_fingerprint": curation["geometry_fingerprint"],
            "created_at_utc": curation["version_created_at_utc"],
        },
        "curation": {
            "id": curation["curation_id"],
            "version": curation["curation_version"],
            "status": curation_status_name(curation["curation_status"]),
            "based_on_curation_id": curation["based_on_curation_id"],
            "created_at_utc": curation["curation_created_at_utc"],
            "published_at_utc": curation["published_at_utc"],
        },
        "source_document": {
            "id": curation["imported_document_id"],
            "document_kind": DOCUMENT_KIND,
            "original_file_name": curation["original_file_name"],
            "sha256": curation["sha256"],
            "dxf_version": curation["dxf_version"],
            "imported_at_utc": curation["imported_at_utc"],
        },
        "measurement_context": {
            "id": curation["measurement_context_id"],
            "source_unit": source_unit_name(curation["source_unit"]),
            "source_unit_value": curation["source_unit"],
            "to_millimeters_factor": curation["to_millimeters_factor"],
            "linear_tolerance_mm": curation["linear_tolerance_mm"],
            "angular_tolerance_deg": curation["angular_tolerance_deg"],
        },
        "extraction_run": {
            "id": run["id"],
            "status": run["status"],
            "extractor_version": run["extractor_version"],
            "started_at_utc": run["started_at_utc"],
            "finished_at_utc": run["finished_at_utc"],
        },
    }


def build_dimension(dimension: dict[str, Any]) -> dict[str, Any]:
    keys = [
        "id",
        "source_entity_ref",
        "source_handle",
        "source_layer",
        "source_entity_kind",
        "geometry_block_name",
        "display_text",
        "display_text_source",
        "raw_text_override",
        "measurement_source_units",
        "measurement_millimeters",
        "source_unit",
        "dim_type",
        "angle",
        "oblique_angle",
        "def_point_x",
        "def_point_y",
        "def_point_z",
        "def_point2_x",
        "def_point2_y",
        "def_point2_z",
        "def_point3_x",
        "def_point3_y",
        "def_point3_z",
        "confidence",
        "detection_notes",
        "render_text_x",
        "render_text_y",
        "render_text_height",
        "render_text_rotation_degrees",
        "render_text_style_name",
        "render_text_horizontal_alignment",
        "render_text_vertical_alignment",
        "render_text_attachment_point",
    ]
    return strip_none({key: dimension.get(key) for key in keys})


def build_label(
    binding: dict[str, Any],
    corridor: dict[str, Any] | None,
    start_node: dict[str, Any] | None,
    end_node: dict[str, Any] | None,
    all_nodes: list[dict[str, Any]],
) -> dict[str, Any]:
    candidate_nodes = [
        build_node(node)
        for node in all_nodes
        if corridor is not None and node["corridor_id"] == corridor["id"]
    ]
    return {
        "binding_status": binding["binding_status"],
        "interval_start_coordinate": binding["interval_start_coordinate"],
        "interval_end_coordinate": binding["interval_end_coordinate"],
        "updated_at_utc": binding["updated_at_utc"],
        "corridor": build_corridor(corridor),
        "start_node": build_node(start_node),
        "end_node": build_node(end_node),
        "candidate_nodes_in_corridor": candidate_nodes,
    }


def build_corridor(corridor: dict[str, Any] | None) -> dict[str, Any] | None:
    if corridor is None:
        return None
    return {
        "id": corridor["id"],
        "name": corridor["name"],
        "axis_tag": axis_name(corridor["axis_tag"]),
        "axis_tag_value": corridor["axis_tag"],
        "guide_geometry_path_id": corridor["guide_geometry_path_id"],
        "band_min_coordinate": corridor["band_min_coordinate"],
        "band_max_coordinate": corridor["band_max_coordinate"],
        "status": corridor["status"],
        "sort_order": corridor["sort_order"],
    }


def build_node(node: dict[str, Any] | None) -> dict[str, Any] | None:
    if node is None:
        return None
    keys = [
        "id",
        "corridor_id",
        "sort_order",
        "reference_kind",
        "source_artifact_kind",
        "source_artifact_id",
        "geometry_path_id",
        "snap_kind",
        "anchor_x",
        "anchor_y",
        "axis_coordinate",
        "offset_along_axis",
        "offset_normal",
        "position_ratio",
    ]
    return {key: node.get(key) for key in keys}


def build_quality(
    status_name: str,
    *,
    binding: dict[str, Any],
    dimension: dict[str, Any] | None,
    corridor: dict[str, Any] | None,
    start_node: dict[str, Any] | None,
    end_node: dict[str, Any] | None,
) -> dict[str, Any]:
    hard_errors: list[str] = []
    if dimension is None:
        hard_errors.append("missing_dimension")
    if corridor is None:
        hard_errors.append("missing_corridor")
    if start_node is None:
        hard_errors.append("missing_start_node")
    if end_node is None:
        hard_errors.append("missing_end_node")
    if corridor is not None and start_node is not None and start_node["corridor_id"] != corridor["id"]:
        hard_errors.append("start_node_corridor_mismatch")
    if corridor is not None and end_node is not None and end_node["corridor_id"] != corridor["id"]:
        hard_errors.append("end_node_corridor_mismatch")
    if binding["start_node_id"] == binding["end_node_id"]:
        hard_errors.append("same_start_and_end_node")

    return {
        "is_training_approved": status_name == "Published",
        "curation_status": status_name,
        "hard_errors": hard_errors,
        "warnings": draft_warnings(status_name),
    }


def draft_warnings(status_name: str) -> list[str]:
    return [] if status_name == "Published" else ["curation_not_published"]


def build_audit_warnings(status_counts: Counter[str]) -> list[str]:
    warnings: list[str] = []
    if status_counts.get("Draft", 0):
        warnings.append("draft_curations_exported_with_is_training_approved_false")
    if status_counts.get("Superseded", 0):
        warnings.append("superseded_curations_exported_with_is_training_approved_false")
    return warnings


def curation_status_name(value: int) -> str:
    return CURATION_STATUS.get(value, f"Unknown:{value}")


def axis_name(value: int) -> str:
    return AXIS_TAG.get(value, f"Unknown:{value}")


def source_unit_name(value: int) -> str:
    return SOURCE_UNIT.get(value, f"unknown:{value}")


def strip_none(value: dict[str, Any]) -> dict[str, Any]:
    return {key: item for key, item in value.items() if item is not None}


def write_jsonl(path: Path, records: list[dict[str, Any]]) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        for record in records:
            handle.write(json.dumps(record, ensure_ascii=False, sort_keys=True))
            handle.write("\n")


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    raise SystemExit(main())
