#!/usr/bin/env python3
"""scene_layout_report.py – read-only Unity scene layout analyser for Las Detox.

Purpose
-------
Parse a Unity .unity (YAML-like) scene file and produce a human-readable or
machine-readable report of the **Environment/Rooms** hierarchy: room containers,
floors, walls, shared walls, and their local/world transforms.

Read-only guarantee
-------------------
This tool **never** modifies the scene file, any YAML, any project asset, the
README, or the git repository.  It does not launch Unity.  All file-system
writes are limited to the explicit ``--output`` path provided by the user.

Limitations
-----------
* Supports only the subset of Unity YAML needed for GameObjects (``!u!1``)
  and Transforms (``!u!4``).  Other component types are silently ignored.
* Quaternion → matrix conversion assumes unit quaternions (Unity convention).
* Does not resolve Prefab overrides – operates on the serialised values only.
"""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Optional

# ---------------------------------------------------------------------------
# 3-D math helpers (pure stdlib – no numpy required)
# ---------------------------------------------------------------------------

Vec3 = tuple[float, float, float]
Quat = tuple[float, float, float, float]  # (x, y, z, w)
# 4×4 matrix stored as a flat list of 16 floats, row-major.
Mat4 = list[float]


def mat4_identity() -> Mat4:
    return [
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1,
    ]


def mat4_mul(a: Mat4, b: Mat4) -> Mat4:
    """Multiply two 4×4 row-major matrices."""
    r: Mat4 = [0.0] * 16
    for row in range(4):
        for col in range(4):
            s = 0.0
            for k in range(4):
                s += a[row * 4 + k] * b[k * 4 + col]
            r[row * 4 + col] = s
    return r


def mat4_from_trs(pos: Vec3, rot: Quat, scl: Vec3) -> Mat4:
    """Build a TRS matrix from position, quaternion rotation, and scale."""
    x, y, z, w = rot
    # Rotation matrix elements (from unit quaternion)
    xx, yy, zz = x * x, y * y, z * z
    xy, xz, yz = x * y, x * z, y * z
    wx, wy, wz = w * x, w * y, w * z

    r00 = 1 - 2 * (yy + zz)
    r01 = 2 * (xy - wz)
    r02 = 2 * (xz + wy)
    r10 = 2 * (xy + wz)
    r11 = 1 - 2 * (xx + zz)
    r12 = 2 * (yz - wx)
    r20 = 2 * (xz - wy)
    r21 = 2 * (yz + wx)
    r22 = 1 - 2 * (xx + yy)

    sx, sy, sz = scl
    return [
        r00 * sx, r01 * sy, r02 * sz, pos[0],
        r10 * sx, r11 * sy, r12 * sz, pos[1],
        r20 * sx, r21 * sy, r22 * sz, pos[2],
        0,        0,        0,        1,
    ]


def mat4_extract_position(m: Mat4) -> Vec3:
    return (m[3], m[7], m[11])


def mat4_extract_scale(m: Mat4) -> Vec3:
    sx = math.sqrt(m[0] ** 2 + m[4] ** 2 + m[8] ** 2)
    sy = math.sqrt(m[1] ** 2 + m[5] ** 2 + m[9] ** 2)
    sz = math.sqrt(m[2] ** 2 + m[6] ** 2 + m[10] ** 2)
    return (sx, sy, sz)


def quat_multiply(a: Quat, b: Quat) -> Quat:
    """Hamilton product  a * b  with layout (x, y, z, w)."""
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def mat4_extract_rotation(m: Mat4) -> Quat:
    """Extract a quaternion (x, y, z, w) from the rotation part of a TRS matrix."""
    sx, sy, sz = mat4_extract_scale(m)
    if sx == 0 or sy == 0 or sz == 0:
        return (0.0, 0.0, 0.0, 1.0)

    r00 = m[0] / sx;  r01 = m[1] / sy;  r02 = m[2] / sz
    r10 = m[4] / sx;  r11 = m[5] / sy;  r12 = m[6] / sz
    r20 = m[8] / sx;  r21 = m[9] / sy;  r22 = m[10] / sz

    trace = r00 + r11 + r22
    if trace > 0:
        s = 0.5 / math.sqrt(trace + 1.0)
        w = 0.25 / s
        x = (r21 - r12) * s
        y = (r02 - r20) * s
        z = (r10 - r01) * s
    elif r00 > r11 and r00 > r22:
        s = 2.0 * math.sqrt(1.0 + r00 - r11 - r22)
        w = (r21 - r12) / s
        x = 0.25 * s
        y = (r01 + r10) / s
        z = (r02 + r20) / s
    elif r11 > r22:
        s = 2.0 * math.sqrt(1.0 + r11 - r00 - r22)
        w = (r02 - r20) / s
        x = (r01 + r10) / s
        y = 0.25 * s
        z = (r12 + r21) / s
    else:
        s = 2.0 * math.sqrt(1.0 + r22 - r00 - r11)
        w = (r10 - r01) / s
        x = (r02 + r20) / s
        y = (r12 + r21) / s
        z = 0.25 * s
    return (x, y, z, w)


def vec3_str(v: Vec3, decimals: int = 3) -> str:
    return f"({v[0]:.{decimals}f}, {v[1]:.{decimals}f}, {v[2]:.{decimals}f})"


def quat_str(q: Quat, decimals: int = 3) -> str:
    return f"({q[0]:.{decimals}f}, {q[1]:.{decimals}f}, {q[2]:.{decimals}f}, {q[3]:.{decimals}f})"


# ---------------------------------------------------------------------------
# Data model
# ---------------------------------------------------------------------------

@dataclass
class TransformData:
    """Parsed Unity Transform component."""
    file_id: str
    game_object_id: str
    father_id: str  # "0" means scene root
    local_position: Vec3 = (0.0, 0.0, 0.0)
    local_rotation: Quat = (0.0, 0.0, 0.0, 1.0)
    local_scale: Vec3 = (1.0, 1.0, 1.0)
    child_transform_ids: list[str] = field(default_factory=list)

    # Computed after hierarchy is built
    world_matrix: Mat4 = field(default_factory=mat4_identity)
    world_position: Vec3 = (0.0, 0.0, 0.0)
    world_rotation: Quat = (0.0, 0.0, 0.0, 1.0)
    world_scale: Vec3 = (1.0, 1.0, 1.0)


@dataclass
class GameObjectData:
    """Parsed Unity GameObject."""
    file_id: str
    name: str = ""
    is_active: bool = True
    transform_id: Optional[str] = None
    children_ids: list[str] = field(default_factory=list)  # GO ids
    hierarchy_path: str = ""


@dataclass
class RoomInfo:
    """High-level description of a detected room / corridor."""
    name: str
    hierarchy_path: str
    go_id: str
    container_local_pos: Vec3
    container_local_rot: Quat
    container_local_scale: Vec3
    container_world_pos: Vec3
    floor_local_pos: Vec3 = (0.0, 0.0, 0.0)
    floor_world_pos: Vec3 = (0.0, 0.0, 0.0)
    floor_scale: Vec3 = (1.0, 1.0, 1.0)
    floor_world_scale: Vec3 = (1.0, 1.0, 1.0)
    approx_size_x: float = 0.0
    approx_size_z: float = 0.0
    approx_area: float = 0.0
    wall_count: int = 0
    wall_names: list[str] = field(default_factory=list)
    has_floor: bool = False
    has_walls: bool = False
    has_doors: bool = False
    has_windows: bool = False
    has_furniture: bool = False


@dataclass
class SharedWallsInfo:
    """Info about the Shared Walls container."""
    hierarchy_path: str
    go_id: str
    wall_names: list[str] = field(default_factory=list)
    wall_count: int = 0


# ---------------------------------------------------------------------------
# Diagnostic messages
# ---------------------------------------------------------------------------

class Diagnostic:
    """Collects errors, warnings, and info messages."""

    def __init__(self) -> None:
        self.messages: list[tuple[str, str]] = []  # (level, text)

    def error(self, msg: str) -> None:
        self.messages.append(("ERROR", msg))

    def warning(self, msg: str) -> None:
        self.messages.append(("WARNING", msg))

    def info(self, msg: str) -> None:
        self.messages.append(("INFO", msg))

    def has_errors(self) -> bool:
        return any(lvl == "ERROR" for lvl, _ in self.messages)

    def print_all(self, file=sys.stderr) -> None:
        for lvl, msg in self.messages:
            print(f"[{lvl}] {msg}", file=file)


# ---------------------------------------------------------------------------
# Unity YAML parser (safe, line-based – no yaml module needed)
# ---------------------------------------------------------------------------

_RE_HEADER = re.compile(r'^--- !u!(\d+) &(\d+)')
_RE_FILEID = re.compile(r'fileID:\s*(-?\d+)')
_RE_VEC = re.compile(
    r'\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}'
)
_RE_QUAT = re.compile(
    r'\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)\}'
)


def _parse_vec3(line: str) -> Optional[Vec3]:
    m = _RE_VEC.search(line)
    if m:
        return (float(m.group(1)), float(m.group(2)), float(m.group(3)))
    return None


def _parse_quat(line: str) -> Optional[Quat]:
    m = _RE_QUAT.search(line)
    if m:
        return (float(m.group(1)), float(m.group(2)),
                float(m.group(3)), float(m.group(4)))
    return None


def _parse_file_id(line: str) -> Optional[str]:
    m = _RE_FILEID.search(line)
    return m.group(1) if m else None


def parse_scene(path: Path, diag: Diagnostic) -> tuple[
    dict[str, GameObjectData], dict[str, TransformData]
]:
    """Parse a Unity .unity scene file and return GameObjects and Transforms."""
    if not path.exists():
        diag.error(f"Scene file not found: {path}")
        return {}, {}

    game_objects: dict[str, GameObjectData] = {}
    transforms: dict[str, TransformData] = {}

    # ---- Phase 1: split file into blocks by --- !u! headers ----
    blocks: list[tuple[str, str, list[str]]] = []  # (type_id, file_id, lines)
    current_type: Optional[str] = None
    current_fid: Optional[str] = None
    current_lines: list[str] = []

    with path.open("r", encoding="utf-8") as fh:
        for raw_line in fh:
            hdr = _RE_HEADER.match(raw_line)
            if hdr:
                if current_type is not None and current_fid is not None:
                    blocks.append((current_type, current_fid, current_lines))
                current_type = hdr.group(1)
                current_fid = hdr.group(2)
                current_lines = []
            else:
                current_lines.append(raw_line)
        if current_type is not None and current_fid is not None:
            blocks.append((current_type, current_fid, current_lines))

    # ---- Phase 2: interpret blocks ----
    for type_id, file_id, lines in blocks:
        if type_id == "1":  # GameObject
            go = GameObjectData(file_id=file_id)
            for line in lines:
                stripped = line.strip()
                if stripped.startswith("m_Name:"):
                    go.name = stripped.split("m_Name:", 1)[1].strip()
                elif stripped.startswith("m_IsActive:"):
                    go.is_active = stripped.split(":", 1)[1].strip() == "1"
            game_objects[file_id] = go

        elif type_id == "4":  # Transform
            td = TransformData(file_id=file_id, game_object_id="0", father_id="0")
            children_ids: list[str] = []
            in_children = False
            for line in lines:
                stripped = line.strip()
                if stripped.startswith("m_GameObject:"):
                    fid = _parse_file_id(line)
                    if fid:
                        td.game_object_id = fid
                elif stripped.startswith("m_Father:"):
                    fid = _parse_file_id(line)
                    if fid:
                        td.father_id = fid
                elif stripped.startswith("m_LocalPosition:"):
                    v = _parse_vec3(line)
                    if v:
                        td.local_position = v
                elif stripped.startswith("m_LocalRotation:"):
                    q = _parse_quat(line)
                    if q:
                        td.local_rotation = q
                elif stripped.startswith("m_LocalScale:"):
                    v = _parse_vec3(line)
                    if v:
                        td.local_scale = v
                elif stripped.startswith("m_Children:"):
                    if "[]" in stripped:
                        in_children = False
                    else:
                        in_children = True
                elif in_children:
                    if stripped.startswith("- {"):
                        fid = _parse_file_id(stripped)
                        if fid:
                            children_ids.append(fid)
                    elif not stripped.startswith("-"):
                        in_children = False
            td.child_transform_ids = children_ids
            transforms[file_id] = td

    return game_objects, transforms


# ---------------------------------------------------------------------------
# Hierarchy building
# ---------------------------------------------------------------------------

def link_hierarchy(
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    diag: Diagnostic,
) -> None:
    """Cross-link GOs ↔ Transforms, build children lists, compute paths."""

    # Map: transform_id → GO
    transform_to_go: dict[str, str] = {}
    go_to_transform: dict[str, str] = {}

    for tid, td in transforms.items():
        go_id = td.game_object_id
        if go_id not in game_objects:
            if go_id != "0":
                diag.warning(f"Transform {tid} references missing GameObject {go_id}")
            continue
        transform_to_go[tid] = go_id
        go_to_transform[go_id] = tid
        game_objects[go_id].transform_id = tid

    # Check every GO has a transform
    for go_id, go in game_objects.items():
        if go.transform_id is None:
            diag.warning(f"GameObject '{go.name}' ({go_id}) has no Transform")

    # Build children list on GameObjectData (preserving order from Transform.m_Children)
    for go_id, go in game_objects.items():
        go.children_ids = []

    for tid, td in transforms.items():
        go_id = transform_to_go.get(tid)
        if go_id is None:
            continue
        for child_tid in td.child_transform_ids:
            child_go_id = transform_to_go.get(child_tid)
            if child_go_id:
                game_objects[go_id].children_ids.append(child_go_id)

    # Validate father references
    for tid, td in transforms.items():
        if td.father_id not in ("0", "-1", ""):
            if td.father_id not in transforms:
                diag.error(
                    f"Transform {tid} (GO '{transform_to_go.get(tid, '?')}') "
                    f"references non-existent father Transform {td.father_id}"
                )

    # Build hierarchy paths (iterative to avoid deep recursion)
    def build_path(go_id: str) -> str:
        parts: list[str] = []
        visited: set[str] = set()
        cur = go_id
        while cur:
            if cur in visited:
                diag.error(f"Cycle detected in hierarchy involving GO {cur}")
                break
            visited.add(cur)
            go = game_objects.get(cur)
            if go is None:
                break
            parts.append(go.name)
            tid = go.transform_id
            if tid is None:
                break
            father_tid = transforms[tid].father_id
            if father_tid in ("0", "-1", ""):
                break
            father_go = transform_to_go.get(father_tid)
            cur = father_go
        parts.reverse()
        return "/".join(parts)

    for go_id in game_objects:
        game_objects[go_id].hierarchy_path = build_path(go_id)

    # Validate: zero scales
    for tid, td in transforms.items():
        sx, sy, sz = td.local_scale
        if sx == 0 or sy == 0 or sz == 0:
            go_name = game_objects.get(transform_to_go.get(tid, ""), GameObjectData(file_id="")).name
            diag.warning(f"Transform {tid} (GO '{go_name}') has zero scale component: {td.local_scale}")

    # Check for duplicate paths
    paths_seen: dict[str, list[str]] = {}
    for go_id, go in game_objects.items():
        paths_seen.setdefault(go.hierarchy_path, []).append(go_id)
    for p, ids in paths_seen.items():
        if len(ids) > 1 and p:
            diag.warning(f"Duplicate hierarchy path '{p}' shared by {len(ids)} GameObjects")


def compute_world_transforms(
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    diag: Diagnostic,
) -> None:
    """Compute world matrices by walking the parent chain for every transform."""
    transform_to_go: dict[str, str] = {}
    for tid, td in transforms.items():
        if td.game_object_id in game_objects:
            transform_to_go[tid] = td.game_object_id

    cache: dict[str, Mat4] = {}

    def get_world_matrix(tid: str, visited: set[str] | None = None) -> Mat4:
        if tid in cache:
            return cache[tid]
        if visited is None:
            visited = set()
        if tid in visited:
            diag.error(f"Cycle in transform chain at {tid}")
            return mat4_identity()
        visited.add(tid)

        td = transforms[tid]
        local = mat4_from_trs(td.local_position, td.local_rotation, td.local_scale)

        if td.father_id in ("0", "-1", "") or td.father_id not in transforms:
            cache[tid] = local
        else:
            parent_world = get_world_matrix(td.father_id, visited)
            cache[tid] = mat4_mul(parent_world, local)

        return cache[tid]

    for tid, td in transforms.items():
        td.world_matrix = get_world_matrix(tid)
        td.world_position = mat4_extract_position(td.world_matrix)
        td.world_scale = mat4_extract_scale(td.world_matrix)
        td.world_rotation = mat4_extract_rotation(td.world_matrix)


# ---------------------------------------------------------------------------
# Room detection
# ---------------------------------------------------------------------------

KNOWN_SUB_CONTAINERS = {"Floor", "Walls", "Doors", "Windows", "Furniture"}


def find_rooms_root(
    game_objects: dict[str, GameObjectData],
    diag: Diagnostic,
) -> Optional[str]:
    """Return the GO id of ``Environment/Rooms`` or None."""
    for go_id, go in game_objects.items():
        if go.hierarchy_path == "Environment/Rooms":
            return go_id
    diag.error("Could not find 'Environment/Rooms' in scene hierarchy.")
    return None


def detect_rooms(
    rooms_go_id: str,
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    diag: Diagnostic,
) -> tuple[list[RoomInfo], Optional[SharedWallsInfo]]:
    """Detect rooms as direct children of Rooms that have a Floor child."""
    rooms_go = game_objects[rooms_go_id]
    rooms: list[RoomInfo] = []
    shared_walls: Optional[SharedWallsInfo] = None

    for child_id in rooms_go.children_ids:
        child = game_objects[child_id]

        # Shared Walls – special container
        if child.name == "Shared Walls":
            sw = SharedWallsInfo(
                hierarchy_path=child.hierarchy_path,
                go_id=child_id,
            )
            for sw_child_id in child.children_ids:
                sw.wall_names.append(game_objects[sw_child_id].name)
            sw.wall_count = len(sw.wall_names)
            shared_walls = sw
            continue

        # Look for Floor among children
        floor_go: Optional[GameObjectData] = None
        walls_go: Optional[GameObjectData] = None
        has_doors = False
        has_windows = False
        has_furniture = False
        floor_count = 0

        for sub_id in child.children_ids:
            sub = game_objects[sub_id]
            if sub.name == "Floor":
                floor_go = sub
                floor_count += 1
            elif sub.name == "Walls":
                walls_go = sub
            elif sub.name == "Doors":
                has_doors = True
            elif sub.name == "Windows":
                has_windows = True
            elif sub.name == "Furniture":
                has_furniture = True

        if floor_count == 0:
            diag.warning(f"Child '{child.name}' under Rooms has no Floor – not treated as a room.")
            continue
        if floor_count > 1:
            diag.warning(f"Room '{child.name}' has {floor_count} Floor objects (expected 1).")

        # Container transform
        c_tid = child.transform_id
        c_td = transforms[c_tid] if c_tid else None

        # Floor transform
        f_tid = floor_go.transform_id if floor_go else None
        f_td = transforms[f_tid] if f_tid else None

        wall_count = 0
        wall_names: list[str] = []
        if walls_go:
            for wid in walls_go.children_ids:
                wall_names.append(game_objects[wid].name)
            wall_count = len(wall_names)
            if wall_count == 0:
                diag.warning(f"Room '{child.name}' has a Walls container with no children.")

        room = RoomInfo(
            name=child.name,
            hierarchy_path=child.hierarchy_path,
            go_id=child_id,
            container_local_pos=c_td.local_position if c_td else (0, 0, 0),
            container_local_rot=c_td.local_rotation if c_td else (0, 0, 0, 1),
            container_local_scale=c_td.local_scale if c_td else (1, 1, 1),
            container_world_pos=c_td.world_position if c_td else (0, 0, 0),
            has_floor=floor_go is not None,
            has_walls=walls_go is not None,
            has_doors=has_doors,
            has_windows=has_windows,
            has_furniture=has_furniture,
            wall_count=wall_count,
            wall_names=wall_names,
        )

        if f_td:
            room.floor_local_pos = f_td.local_position
            room.floor_world_pos = f_td.world_position
            room.floor_scale = f_td.local_scale
            room.floor_world_scale = f_td.world_scale
            room.approx_size_x = abs(f_td.world_scale[0])
            room.approx_size_z = abs(f_td.world_scale[2])
            room.approx_area = room.approx_size_x * room.approx_size_z

        rooms.append(room)

    # Validate: Shared Walls should NOT be inside a single room
    if shared_walls:
        for room in rooms:
            for sub_id in game_objects[room.go_id].children_ids:
                if game_objects[sub_id].name == "Shared Walls":
                    diag.warning(
                        f"'Shared Walls' found inside room '{room.name}' – "
                        "it should be a sibling, not a child."
                    )

    return rooms, shared_walls


# ---------------------------------------------------------------------------
# Validation (--check mode)
# ---------------------------------------------------------------------------

def run_validations(
    rooms: list[RoomInfo],
    shared_walls: Optional[SharedWallsInfo],
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    diag: Diagnostic,
) -> None:
    """Additional structural validations."""
    # Already done in earlier phases; add any extra checks here.
    diag.info(f"Detected {len(rooms)} room(s) with Floor.")
    if shared_walls:
        diag.info(f"Shared Walls container has {shared_walls.wall_count} wall(s).")


# ---------------------------------------------------------------------------
# Output formatters
# ---------------------------------------------------------------------------

def _build_hierarchy_tree(
    go_id: str,
    game_objects: dict[str, GameObjectData],
    indent: int = 0,
) -> list[str]:
    """Return lines for a text tree starting at go_id."""
    go = game_objects[go_id]
    lines = [f"{'  ' * indent}- {go.name}"]
    for child_id in go.children_ids:
        lines.extend(_build_hierarchy_tree(child_id, game_objects, indent + 1))
    return lines


def format_text(
    rooms: list[RoomInfo],
    shared_walls: Optional[SharedWallsInfo],
    rooms_go_id: str,
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    scene_path: Path,
    diag: Diagnostic,
) -> str:
    """Produce the default human-readable terminal report."""
    lines: list[str] = []
    lines.append("=" * 72)
    lines.append("  SCENE LAYOUT REPORT – Las Detox")
    lines.append("=" * 72)
    lines.append(f"Scene : {scene_path}")
    lines.append(f"Date  : {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    lines.append(f"Scale : 1 Unity unit = 1 metre")
    lines.append("")

    # Hierarchy tree
    lines.append("--- Hierarchy: Environment/Rooms ---")
    lines.extend(_build_hierarchy_tree(rooms_go_id, game_objects))
    lines.append("")

    # Rooms table
    lines.append("--- Rooms ---")
    for r in rooms:
        lines.append(f"  [{r.name}]")
        lines.append(f"    Path               : {r.hierarchy_path}")
        lines.append(f"    Container localPos  : {vec3_str(r.container_local_pos)}")
        lines.append(f"    Container localRot  : {quat_str(r.container_local_rot)}")
        lines.append(f"    Container localScale: {vec3_str(r.container_local_scale)}")
        lines.append(f"    Container worldPos  : {vec3_str(r.container_world_pos)}")
        if r.has_floor:
            lines.append(f"    Floor localPos      : {vec3_str(r.floor_local_pos)}")
            lines.append(f"    Floor worldPos      : {vec3_str(r.floor_world_pos)}")
            lines.append(f"    Floor localScale    : {vec3_str(r.floor_scale)}")
            lines.append(f"    Floor worldScale    : {vec3_str(r.floor_world_scale)}")
            lines.append(f"    Approx size X×Z    : {r.approx_size_x:.2f} × {r.approx_size_z:.2f} m")
            lines.append(f"    Approx area        : ~{r.approx_area:.1f} m²")
        lines.append(f"    Walls              : {r.wall_count}")
        if r.wall_names:
            for wn in r.wall_names:
                lines.append(f"      • {wn}")
        lines.append("")

    # Shared Walls
    if shared_walls:
        lines.append("--- Shared Walls ---")
        lines.append(f"  Path  : {shared_walls.hierarchy_path}")
        lines.append(f"  Count : {shared_walls.wall_count}")
        for wn in shared_walls.wall_names:
            lines.append(f"    • {wn}")
        lines.append("")

    # Diagnostics
    if diag.messages:
        lines.append("--- Diagnostics ---")
        for lvl, msg in diag.messages:
            lines.append(f"  [{lvl}] {msg}")
        lines.append("")

    lines.append("=" * 72)
    return "\n".join(lines)


def _room_to_dict(r: RoomInfo) -> dict:
    return {
        "name": r.name,
        "hierarchy_path": r.hierarchy_path,
        "container": {
            "local_position": list(r.container_local_pos),
            "local_rotation": list(r.container_local_rot),
            "local_scale": list(r.container_local_scale),
            "world_position": list(r.container_world_pos),
        },
        "floor": {
            "local_position": list(r.floor_local_pos),
            "world_position": list(r.floor_world_pos),
            "local_scale": list(r.floor_scale),
            "world_scale": list(r.floor_world_scale),
            "approx_size_x_m": round(r.approx_size_x, 3),
            "approx_size_z_m": round(r.approx_size_z, 3),
            "approx_area_m2": round(r.approx_area, 2),
        } if r.has_floor else None,
        "wall_count": r.wall_count,
        "wall_names": r.wall_names,
        "sub_containers": {
            "has_floor": r.has_floor,
            "has_walls": r.has_walls,
            "has_doors": r.has_doors,
            "has_windows": r.has_windows,
            "has_furniture": r.has_furniture,
        },
    }


def format_json(
    rooms: list[RoomInfo],
    shared_walls: Optional[SharedWallsInfo],
    rooms_go_id: str,
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    scene_path: Path,
    diag: Diagnostic,
) -> str:
    data = {
        "scene": str(scene_path),
        "generated_at": datetime.now().isoformat(),
        "scale": "1 unit = 1 metre",
        "hierarchy": _build_hierarchy_tree(rooms_go_id, game_objects),
        "rooms": [_room_to_dict(r) for r in rooms],
        "shared_walls": {
            "hierarchy_path": shared_walls.hierarchy_path,
            "wall_count": shared_walls.wall_count,
            "wall_names": shared_walls.wall_names,
        } if shared_walls else None,
        "diagnostics": [{"level": lvl, "message": msg} for lvl, msg in diag.messages],
    }
    return json.dumps(data, indent=2, ensure_ascii=False)


def format_markdown(
    rooms: list[RoomInfo],
    shared_walls: Optional[SharedWallsInfo],
    rooms_go_id: str,
    game_objects: dict[str, GameObjectData],
    transforms: dict[str, TransformData],
    scene_path: Path,
    diag: Diagnostic,
) -> str:
    lines: list[str] = []
    lines.append("# Raport układu sceny – Las Detox (auto-generated)")
    lines.append("")
    lines.append(f"- **Scena:** `{scene_path}`")
    lines.append(f"- **Data:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    lines.append(f"- **Skala:** 1 jednostka Unity = 1 metr")
    lines.append("")

    # Hierarchy
    lines.append("## Drzewo Environment/Rooms")
    lines.append("")
    lines.extend(_build_hierarchy_tree(rooms_go_id, game_objects))
    lines.append("")

    # Room table
    lines.append("## Tabela pomieszczeń")
    lines.append("")
    hdr = (
        "| Nazwa | Ścieżka | Kontener localPos | Floor localPos | Floor worldPos "
        "| Floor scale (X×Z) | Wymiar (m) | Pow. (m²) | Ściany |"
    )
    sep = "| :--- " * 9 + "|"
    lines.append(hdr)
    lines.append(sep)
    for r in rooms:
        row = (
            f"| {r.name} "
            f"| `{r.hierarchy_path}` "
            f"| `{vec3_str(r.container_local_pos)}` "
            f"| `{vec3_str(r.floor_local_pos)}` "
            f"| `{vec3_str(r.floor_world_pos)}` "
            f"| `({r.floor_scale[0]:.2f}, {r.floor_scale[2]:.2f})` "
            f"| {r.approx_size_x:.2f} × {r.approx_size_z:.2f} "
            f"| ~{r.approx_area:.1f} "
            f"| {r.wall_count} |"
        )
        lines.append(row)
    lines.append("")

    # Shared Walls
    if shared_walls:
        lines.append("## Ściany wspólne (Shared Walls)")
        lines.append("")
        lines.append(f"- Ścieżka: `{shared_walls.hierarchy_path}`")
        for wn in shared_walls.wall_names:
            lines.append(f"  - `{wn}`")
        lines.append("")

    # Diagnostics
    if diag.messages:
        lines.append("## Ostrzeżenia walidacyjne")
        lines.append("")
        for lvl, msg in diag.messages:
            lines.append(f"- **{lvl}**: {msg}")
        lines.append("")

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

DEFAULT_SCENE = Path("Assets/_Project/Scenes/DetoxPrototype.unity")


def build_argparser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        description="Read-only Unity scene layout analyser for Las Detox.",
        epilog="This tool never modifies the scene file.",
    )
    p.add_argument(
        "--scene",
        type=Path,
        default=DEFAULT_SCENE,
        help=f"Path to the .unity scene file (default: {DEFAULT_SCENE})",
    )
    p.add_argument(
        "--format",
        choices=("text", "json", "markdown"),
        default="text",
        help="Output format (default: text).",
    )
    p.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Write output to a file instead of stdout.",
    )
    p.add_argument(
        "--check",
        action="store_true",
        help="Validation-only mode: print diagnostics, write nothing, "
             "exit 0 on success, non-zero on errors.",
    )
    return p


def main() -> int:
    args = build_argparser().parse_args()
    scene_path: Path = args.scene
    diag = Diagnostic()

    # ---- Parse ----
    game_objects, transforms = parse_scene(scene_path, diag)
    if diag.has_errors():
        diag.print_all()
        return 1

    # ---- Link & compute ----
    link_hierarchy(game_objects, transforms, diag)
    if diag.has_errors():
        diag.print_all()
        return 1

    compute_world_transforms(game_objects, transforms, diag)

    # ---- Find Rooms ----
    rooms_go_id = find_rooms_root(game_objects, diag)
    if rooms_go_id is None:
        diag.print_all()
        return 1

    rooms, shared_walls = detect_rooms(rooms_go_id, game_objects, transforms, diag)
    run_validations(rooms, shared_walls, game_objects, transforms, diag)

    # ---- --check mode ----
    if args.check:
        diag.print_all()
        return 1 if diag.has_errors() else 0

    # ---- Format ----
    fmt = args.format
    if fmt == "text":
        output = format_text(rooms, shared_walls, rooms_go_id,
                             game_objects, transforms, scene_path, diag)
    elif fmt == "json":
        output = format_json(rooms, shared_walls, rooms_go_id,
                             game_objects, transforms, scene_path, diag)
    elif fmt == "markdown":
        output = format_markdown(rooms, shared_walls, rooms_go_id,
                                 game_objects, transforms, scene_path, diag)
    else:
        diag.error(f"Unknown format: {fmt}")
        diag.print_all()
        return 1

    # ---- Write ----
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output, encoding="utf-8")
        print(f"Report written to {args.output}", file=sys.stderr)
    else:
        print(output)

    return 0


if __name__ == "__main__":
    sys.exit(main())
