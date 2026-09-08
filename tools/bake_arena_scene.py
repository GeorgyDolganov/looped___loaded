import json
import math
import uuid
from pathlib import Path

NS = uuid.UUID("9f2c4d10-3a51-4e88-9b7c-51d3a7e10001")
BOX = 50.0
TRACK_RADIUS = 1000.0
TRACK_WIDTH = 190.0
BOUNDARY_RADIUS = 1170.0
CORE_RADIUS = 230.0
START_ANGLE = math.pi * 0.5
TRACK_INNER = TRACK_RADIUS - TRACK_WIDTH * 0.5
TRACK_OUTER = TRACK_RADIUS + TRACK_WIDTH * 0.5
CITY_SIZE = 5
CITY_CELL = 180.0
CITY_SPAN = CITY_SIZE * CITY_CELL

FLOOR = (0.035, 0.042, 0.055, 1)
TRACK = (0.13, 0.15, 0.19, 1)
EDGE = (0.34, 0.42, 0.52, 1)
BOUNDARY = (0.62, 0.70, 0.82, 1)
CORE = (0.42, 0.24, 0.30, 1)
FINISH_GOLD = (1.0, 0.84, 0.38, 1)
FINISH_INK = (0.05, 0.06, 0.08, 1)


def gid(*parts):
    return str(uuid.uuid5(NS, "/".join(parts)))


def fmt(value):
    if isinstance(value, float):
        text = f"{value:.6f}".rstrip("0").rstrip(".")
        return "0" if text in {"-0", ""} else text
    return str(value)


def vec(*values):
    return ",".join(fmt(v) for v in values)


def from_angle(radians):
    return math.cos(radians), math.sin(radians)


def from_yaw(degrees):
    half = math.radians(degrees) * 0.5
    return 0.0, 0.0, math.sin(half), math.cos(half)


def flat_facing(x, y):
    return from_yaw(math.degrees(math.atan2(y, x)))


def scale_from_size(size):
    return tuple(s / BOX for s in size)


def cref(comp_id, go_id, typ):
    return {
        "_type": "component",
        "component_id": comp_id,
        "go": go_id,
        "component_type": typ,
    }


def go(name, guid=None, position=None, rotation=None, scale=None, components=None, children=None):
    node = {
        "__guid": guid or gid("go", name, str(position), str(rotation), str(scale)),
        "Flags": 0,
        "Name": name,
        "Enabled": True,
    }
    if position and position != (0, 0, 0):
        node["Position"] = vec(*position)
    if rotation and rotation != (0, 0, 0, 1):
        node["Rotation"] = vec(*rotation)
    if scale and scale != (1, 1, 1):
        node["Scale"] = vec(*scale)
    if components:
        node["Components"] = components
    if children:
        node["Children"] = children
    return node


def box(name, position, rotation, size, tint, shadows=True, extra=None, guid=None):
    renderer = {
        "__type": "Sandbox.ModelRenderer",
        "__guid": gid("mr", name, str(position)),
        "BodyGroups": 18446744073709551615,
        "MaterialOverride": "materials/default.vmat",
        "Model": "models/dev/box.vmdl",
        "RenderType": "On" if shadows else "Off",
        "Tint": vec(*tint),
    }
    components = [renderer]
    if extra:
        components.extend(extra)
    return go(name, guid=guid, position=position, rotation=rotation, scale=scale_from_size(size), components=components)


def wall_box(kind, index, a, b):
    if kind == "Boundary":
        thickness, height, tint = 32.0, 140.0, BOUNDARY
    else:
        thickness, height, tint = 36.0, 170.0, CORE
    center = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5)
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dy)
    return box(
        f"Wall {kind} {index}",
        (center[0], center[1], height * 0.5),
        flat_facing(dx, dy),
        (length + thickness, thickness, height),
        tint,
        extra=[{
            "__type": "LoopedLoaded.ArenaWall",
            "__guid": gid("wall", kind, str(index)),
            "Kind": kind,
            "FlipFacing": False,
        }],
    )


def ring(radius, sides, offset, kind):
    step = math.tau / sides
    walls = []
    for i in range(sides):
        ax, ay = from_angle(offset + step * i)
        bx, by = from_angle(offset + step * (i + 1))
        walls.append(wall_box(kind, i, (ax * radius, ay * radius), (bx * radius, by * radius)))
    return walls


def finish_mark(name, position, rotation, size, tint, pulse):
    return box(
        name,
        position,
        rotation,
        size,
        tint,
        shadows=False,
        extra=[{
            "__type": "LoopedLoaded.FinishPulse",
            "__guid": gid("pulse", name),
            "Pulse": pulse,
        }],
    )


def build_finish():
    outward = from_angle(START_ANGLE)
    along = (-outward[1], outward[0])
    rot = flat_facing(*along)
    mid = (outward[0] * TRACK_RADIUS, outward[1] * TRACK_RADIUS, 0.0)
    cols, rows = 8, 3
    cell_along = 18.0
    cell_across = (TRACK_WIDTH + 12.0) / cols
    children = []
    for row in range(rows):
        shift = (row - (rows - 1) * 0.5) * cell_along
        for col in range(cols):
            across = (col + 0.5) * cell_across - (TRACK_WIDTH + 12.0) * 0.5
            gold = (row + col) % 2 == 0
            pos = (
                mid[0] + along[0] * shift + outward[0] * across,
                mid[1] + along[1] * shift + outward[1] * across,
                10.0,
            )
            children.append(finish_mark(
                f"Cell {row},{col}",
                pos,
                rot,
                (cell_along * 1.05, cell_across * 1.05, 8.0),
                FINISH_GOLD if gold else FINISH_INK,
                0.7 if gold else 0.12,
            ))

    children.append(finish_mark("Post Inner", (outward[0] * TRACK_INNER, outward[1] * TRACK_INNER, 110.0), (0, 0, 0, 1), (26, 26, 220), FINISH_GOLD, 0.65))
    children.append(finish_mark("Post Outer", (outward[0] * TRACK_OUTER, outward[1] * TRACK_OUTER, 110.0), (0, 0, 0, 1), (26, 26, 220), FINISH_GOLD, 0.65))
    children.append(finish_mark("Bar", (mid[0], mid[1], 214.0), rot, (22, TRACK_WIDTH + 36, 16), FINISH_GOLD, 0.8))
    children.append(finish_mark("Flag Inner", (outward[0] * TRACK_INNER, outward[1] * TRACK_INNER, 232.0), rot, (8, 52, 36), FINISH_GOLD, 0.9))
    children.append(finish_mark("Flag Outer", (outward[0] * TRACK_OUTER, outward[1] * TRACK_OUTER, 232.0), rot, (8, 52, 36), FINISH_GOLD, 0.9))

    for i in range(3):
        width = 72.0 + i * 38.0
        at = START_ANGLE + 0.30 - i * 0.09
        o = from_angle(at)
        t = (-o[1], o[0])
        children.append(finish_mark(
            f"Approach {i}",
            (o[0] * TRACK_RADIUS, o[1] * TRACK_RADIUS, 10.0),
            flat_facing(*t),
            (14.0, width, 8.0),
            FINISH_GOLD,
            0.55 + i * 0.12,
        ))

    inner = TRACK_INNER - 8.0
    outer = TRACK_OUTER + 8.0
    children.append(go("Beam Inner", position=(outward[0] * inner, outward[1] * inner, 28.0)))
    children.append(go("Beam Outer", position=(outward[0] * outer, outward[1] * outer, 28.0)))
    children.append(go(
        "Finish Beam",
        components=[{
            "__type": "LoopedLoaded.PolyLine",
            "__guid": gid("comp", "finish-beam"),
            "HeadTint": vec(*FINISH_GOLD),
            "TailTint": vec(*FINISH_GOLD),
            "HeadWidth": 16,
            "TailWidth": 16,
        }],
    ))
    gold_light = (FINISH_GOLD[0] * 3.5, FINISH_GOLD[1] * 3.5, FINISH_GOLD[2] * 3.5, 1)
    children.append(go(
        "Finish Light",
        position=(mid[0], mid[1], 80.0),
        components=[{
            "__type": "Sandbox.PointLight",
            "__guid": gid("comp", "finish-light"),
            "LightColor": vec(*gold_light),
            "Radius": 720,
            "Shadows": False,
        }],
    ))
    return go("Finish", guid=gid("go", "Finish"), children=children)


def build_yard():
    floor_span = BOUNDARY_RADIUS * 2.8
    floor = go("Floor", children=[
        box("Slab", (0, 0, -30), (0, 0, 0, 1), (floor_span, floor_span, 60), FLOOR)
    ])

    track_children = []
    segments = 64
    step = math.tau / segments
    arc = math.tau * TRACK_RADIUS / segments
    for i in range(segments):
        angle = step * i
        outward = from_angle(angle)
        tangent = (-outward[1], outward[0])
        shade = 1.0 if i % 2 == 0 else 0.82
        tint = (TRACK[0] * shade, TRACK[1] * shade, TRACK[2] * shade, 1)
        track_children.append(box(
            f"Track {i}",
            (outward[0] * TRACK_RADIUS, outward[1] * TRACK_RADIUS, 3.0),
            flat_facing(*tangent),
            (arc * 1.02, TRACK_WIDTH, 6.0),
            tint,
            shadows=False,
        ))
    track = go("Track", children=track_children)

    edge_children = []
    for label, radius in (("Inner", TRACK_INNER), ("Outer", TRACK_OUTER)):
        segments = 72
        step = math.tau / segments
        arc = math.tau * radius / segments
        for i in range(segments):
            angle = step * i
            outward = from_angle(angle)
            tangent = (-outward[1], outward[0])
            edge_children.append(box(
                f"Edge {label} {i}",
                (outward[0] * radius, outward[1] * radius, 8.0),
                flat_facing(*tangent),
                (arc * 1.02, 9.0, 10.0),
                EDGE,
                shadows=False,
            ))
    edges = go("Edges", children=edge_children)
    walls = go("Walls", children=ring(BOUNDARY_RADIUS, 24, math.pi / 24.0, "Boundary") + ring(CORE_RADIUS, 8, 0.0, "Core"))
    panels = go("Panels", guid=gid("go", "Panels"))

    yard_id = gid("go", "Yard")
    arena_id = gid("comp", "ArenaBuilder")
    return go(
        "Yard",
        guid=yard_id,
        components=[{
            "__type": "LoopedLoaded.ArenaBuilder",
            "__guid": arena_id,
            "Code": "YARD",
            "TrackRadius": TRACK_RADIUS,
            "TrackWidth": TRACK_WIDTH,
            "BoundaryRadius": BOUNDARY_RADIUS,
            "CoreRadius": CORE_RADIUS,
            "StartAngle": START_ANGLE,
        }],
        children=[floor, track, edges, walls, build_finish(), panels],
    ), yard_id, arena_id


def build_city():
    city_id = gid("go", "City")
    board_id = gid("comp", "CityBoard")
    stage_id = gid("go", "CityStage")
    children = [
        box("Ground", (0, -180, -18), (0, 0, 0, 1), (CITY_SPAN * 1.45, CITY_SPAN * 1.9, 20), (0.07, 0.09, 0.12, 1), shadows=False)
    ]
    origin = (-CITY_SPAN * 0.5, -CITY_SPAN * 0.5)
    for y in range(CITY_SIZE):
        for x in range(CITY_SIZE):
            pos = (origin[0] + (x + 0.5) * CITY_CELL, origin[1] + (y + 0.5) * CITY_CELL, 2.0)
            shade = 1.0 if (x + y) % 2 == 0 else 0.78
            tint = (0.12 * shade, 0.15 * shade, 0.2 * shade, 1)
            children.append(box(
                f"Tile {x},{y}",
                pos,
                (0, 0, 0, 1),
                (CITY_CELL * 0.92, CITY_CELL * 0.92, 6.0),
                tint,
                shadows=False,
                extra=[{
                    "__type": "LoopedLoaded.CityCell",
                    "__guid": gid("cell", str(x), str(y)),
                    "X": x,
                    "Y": y,
                }],
            ))

    stand = (0.0, -CITY_SPAN * 0.5 - 260.0, 0.0)
    children.append(box("Pad", (stand[0], stand[1], -8.0), (0, 0, 0, 1), (220, 140, 12), (0.18, 0.22, 0.28, 1), shadows=False))
    shooter = go(
        "City Shooter",
        position=stand,
        children=[
            box("Torso", (0, 0, 40), (0, 0, 0, 1), (46, 46, 80), (0.82, 0.94, 1, 1)),
            box("Barrel", (54, 0, 46), (0, 0, 0, 1), (76, 16, 16), (0.22, 0.3, 0.4, 1)),
        ],
    )
    children.append(shooter)
    children.append(go("Runtime", guid=gid("go", "CityRuntime")))
    stage = go("Stage", guid=stage_id, children=children)
    city = go(
        "City",
        guid=city_id,
        position=(5000.0, 0.0, 0.0),
        components=[{
            "__type": "LoopedLoaded.CityBoard",
            "__guid": board_id,
            "Size": CITY_SIZE,
            "CellSize": CITY_CELL,
            "PlayHeight": 40,
        }],
        children=[stage],
    )
    return city, city_id, board_id


def main():
    yard, yard_id, arena_id = build_yard()
    city, city_id, board_id = build_city()

    game_id = "9f2c4d10-3a51-4e88-9b7c-51d3a7e10008"
    bootstrap_id = "9f2c4d10-3a51-4e88-9b7c-51d3a7e10009"
    loop_id = gid("comp", "GameLoop")
    player_id = gid("go", "Player")
    runner_id = gid("comp", "RingRunner")
    aim_id = gid("comp", "PlayerAim")
    inv_id = gid("comp", "RoundInventory")
    hud_id = gid("go", "HUD")
    hud_comp = gid("comp", "ArenaHud")
    camera_id = "9f2c4d10-3a51-4e88-9b7c-51d3a7e10004"
    camera_comp = "9f2c4d10-3a51-4e88-9b7c-51d3a7e10005"
    rig_id = gid("comp", "ArenaCamera")

    scene = {
        "__guid": "9f2c4d10-3a51-4e88-9b7c-51d3a7e10001",
        "GameObjects": [
            {
                "__guid": "9f2c4d10-3a51-4e88-9b7c-51d3a7e10002",
                "Flags": 0,
                "Name": "Sun",
                "Rotation": "-0.1830127,0.6830127,0.1830127,0.6830127",
                "Tags": "light_directional,light",
                "Enabled": True,
                "Components": [
                    {
                        "__type": "Sandbox.DirectionalLight",
                        "__guid": "9f2c4d10-3a51-4e88-9b7c-51d3a7e10003",
                        "FogMode": "Disabled",
                        "FogStrength": 1,
                        "LightColor": "0.55,0.62,0.78,1",
                        "Shadows": True,
                        "SkyColor": "0.04,0.05,0.08,1",
                    }
                ],
            },
            go(
                "Ambient",
                guid=gid("go", "Ambient"),
                components=[{
                    "__type": "Sandbox.AmbientLight",
                    "__guid": gid("comp", "AmbientLight"),
                    "Color": "0.06,0.08,0.12,1",
                }],
            ),
            {
                "__guid": camera_id,
                "Flags": 0,
                "Name": "Camera",
                "Position": "0,-1498.4,3707.8",
                "Rotation": "-0.395409,0.395409,0.586218,0.586218",
                "Enabled": True,
                "Components": [
                    {
                        "__type": "Sandbox.CameraComponent",
                        "__guid": camera_comp,
                        "BackgroundColor": "0.008,0.011,0.018,1",
                        "ClearFlags": "All",
                        "FieldOfView": 60,
                        "IsMainCamera": True,
                        "Orthographic": True,
                        "OrthographicHeight": 2900,
                        "Priority": 1,
                        "RenderExcludeTags": "",
                        "RenderTags": "",
                        "TargetEye": "None",
                        "Viewport": "0,0,1,1",
                        "ZFar": 20000,
                        "ZNear": 10,
                    },
                    {
                        "__type": "Sandbox.Bloom",
                        "__guid": "9f2c4d10-3a51-4e88-9b7c-51d3a7e10006",
                        "BloomColor": {"color": [{"c": "1,1,1,1"}, {"t": 1, "c": "1,1,1,1"}], "alpha": []},
                        "BloomCurve": [{"y": 0.5}, {"x": 1, "y": 1}],
                        "Mode": "Additive",
                        "Strength": 0.85,
                        "Threshold": 0.35,
                        "ThresholdWidth": 0.6,
                    },
                    {
                        "__type": "Sandbox.Tonemapping",
                        "__guid": "9f2c4d10-3a51-4e88-9b7c-51d3a7e10007",
                        "__version": 1,
                        "ExposureBias": 2,
                        "ExposureCompensation": 0,
                        "ExposureMethod": "RGB",
                        "MaximumExposure": 2,
                        "MinimumExposure": 1,
                        "Mode": "Legacy",
                        "Rate": 1,
                    },
                    {
                        "__type": "LoopedLoaded.ArenaCamera",
                        "__guid": rig_id,
                        "Pitch": 68,
                        "Yaw": 90,
                        "Distance": 4000,
                        "FrameMargin": 1.24,
                        "FollowBias": 0.14,
                        "FollowSmoothing": 6,
                    },
                ],
            },
            {
                "__guid": game_id,
                "Flags": 0,
                "Name": "Game",
                "Enabled": True,
                "Components": [
                    {
                        "__type": "LoopedLoaded.GameBootstrap",
                        "__guid": bootstrap_id,
                    },
                    {
                        "__type": "LoopedLoaded.GameLoop",
                        "__guid": loop_id,
                        "LostRoundMinArc": 460,
                        "NoticeDuration": 1.6,
                        "MaxHealth": 3,
                    },
                ],
            },
            go(
                "Player",
                guid=player_id,
                components=[
                    {"__type": "LoopedLoaded.RingRunner", "__guid": runner_id, "Speed": 330, "DashDistance": 430, "DashCooldown": 1.1, "DashDuration": 0.17, "SlowSpeedScale": 0.38, "SlowDrain": 0.55, "SlowRegen": 0.28, "PlayerRadius": 48},
                    {"__type": "LoopedLoaded.PlayerAim", "__guid": aim_id, "MuzzleOffset": 82, "PreviewLength": 1500, "PreviewBounceLength": 340, "RoundRadius": 13},
                    {"__type": "LoopedLoaded.RoundInventory", "__guid": inv_id},
                ],
            ),
            go(
                "HUD",
                guid=hud_id,
                components=[
                    {"__type": "Sandbox.ScreenPanel", "__guid": gid("comp", "ScreenPanel")},
                    {"__type": "LoopedLoaded.ArenaHud", "__guid": hud_comp},
                ],
            ),
            yard,
            city,
        ],
        "SceneProperties": {
            "FixedUpdateFrequency": 60,
            "MaxFixedUpdates": 5,
            "NetworkFrequency": 60,
            "NetworkInterpolation": True,
            "ThreadedAnimation": True,
            "TimeScale": 1,
            "UseFixedUpdate": True,
            "NavMesh": {
                "Enabled": False,
                "IncludeStaticBodies": True,
                "IncludeKeyframedBodies": True,
                "EditorAutoUpdate": True,
                "AgentHeight": 64,
                "AgentRadius": 16,
                "AgentStepSize": 18,
                "AgentMaxSlope": 40,
                "ExcludedBodies": "",
                "IncludedBodies": "",
            },
        },
        "Title": "arena",
        "Description": "One Round Trip",
        "ResourceVersion": 1,
        "__references": [],
        "__version": 1,
    }

    refs = {
        (game_id, bootstrap_id): {
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "City": cref(board_id, city_id, "LoopedLoaded.CityBoard"),
            "Runner": cref(runner_id, player_id, "LoopedLoaded.RingRunner"),
            "Camera": cref(camera_comp, camera_id, "Sandbox.CameraComponent"),
        },
        (game_id, loop_id): {
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "Runner": cref(runner_id, player_id, "LoopedLoaded.RingRunner"),
            "Aim": cref(aim_id, player_id, "LoopedLoaded.PlayerAim"),
            "Inventory": cref(inv_id, player_id, "LoopedLoaded.RoundInventory"),
            "City": cref(board_id, city_id, "LoopedLoaded.CityBoard"),
        },
        (player_id, runner_id): {
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
        },
        (player_id, aim_id): {
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "Runner": cref(runner_id, player_id, "LoopedLoaded.RingRunner"),
            "Inventory": cref(inv_id, player_id, "LoopedLoaded.RoundInventory"),
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
        },
        (player_id, inv_id): {
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "Runner": cref(runner_id, player_id, "LoopedLoaded.RingRunner"),
            "Aim": cref(aim_id, player_id, "LoopedLoaded.PlayerAim"),
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
        },
        (hud_id, hud_comp): {
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
        },
        (city_id, board_id): {
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
        },
        (camera_id, rig_id): {
            "Loop": cref(loop_id, game_id, "LoopedLoaded.GameLoop"),
            "City": cref(board_id, city_id, "LoopedLoaded.CityBoard"),
            "Arena": cref(arena_id, yard_id, "LoopedLoaded.ArenaBuilder"),
            "Runner": cref(runner_id, player_id, "LoopedLoaded.RingRunner"),
        },
    }

    def walk(node):
        for comp in node.get("Components", []):
            extra = refs.get((node["__guid"], comp["__guid"]))
            if extra:
                comp.update(extra)
        for child in node.get("Children", []):
            walk(child)

    for root in scene["GameObjects"]:
        walk(root)

    path = Path(__file__).resolve().parents[1] / "Assets" / "scenes" / "arena.scene"
    path.write_text(json.dumps(scene, indent=2) + "\n", encoding="utf-8")
    print(path, "objects", count_objects(scene["GameObjects"]))


def count_objects(nodes):
    total = 0
    for node in nodes:
        total += 1
        total += count_objects(node.get("Children", []))
    return total


if __name__ == "__main__":
    main()
