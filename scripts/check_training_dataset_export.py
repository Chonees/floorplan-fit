"""Smoke-check the floor-plan training dataset export.

Runs the exporter against the local dev SQLite workspace and verifies that the
JSONL/audit contract is usable without needing a .NET build.
"""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "scripts" / "export_training_dataset.py"
DB = ROOT / "src" / "FloorplanFit.Desktop" / "bin" / "Debug" / "net10.0" / "workspace" / "app.db"


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="floorplan-training-export-") as tmp:
        out_dir = Path(tmp)
        subprocess.run(
            [
                sys.executable,
                str(EXPORTER),
                "--db",
                str(DB),
                "--out",
                str(out_dir),
            ],
            cwd=ROOT,
            check=True,
        )

        manifest = read_json(out_dir / "manifest.json")
        audit = read_json(out_dir / "audit.json")
        jsonl_path = out_dir / "floorplan_dimension_bindings.v1.jsonl"
        records = [json.loads(line) for line in jsonl_path.read_text(encoding="utf-8").splitlines() if line]

    assert manifest["schema_version"] == "floorplan_dimension_binding.v1"
    assert manifest["primary_document_kind"] == "floor_plan"
    assert "electrical_plan" in manifest["future_document_kinds"]
    assert "facade" in manifest["future_document_kinds"]
    assert audit["hard_error_count"] == 0
    assert audit["exported_records"] == len(records)
    assert audit["accepted_binding_records"] > 0
    assert audit["unbound_dimension_records"] > 0

    first = records[0]
    assert first["schema_version"] == "floorplan_dimension_binding.v1"
    assert first["document_kind"] == "floor_plan"
    assert "plan" in first
    assert "dimension" in first
    assert "dimension_primitives" in first
    assert "quality" in first
    assert any(record["example_kind"] == "accepted_binding" and record.get("label") for record in records)
    assert any(record["example_kind"] == "unbound_dimension" for record in records)

    print("training dataset export contract OK")
    return 0


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


if __name__ == "__main__":
    raise SystemExit(main())
