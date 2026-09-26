import json
import random
import uuid
from pathlib import Path

root = Path(__file__).resolve().parents[1]
traits = json.loads((root / "Assets/settings/traits.omrtrait").read_text(encoding="utf-8"))
if "BuckPellets" not in traits:
    raise SystemExit("per-card numbers already moved out of traits.omrtrait")
text = json.loads((root / "Assets/settings/text.omrtext").read_text(encoding="utf-8"))
prog = json.loads((root / "Assets/settings/progression.omrprog").read_text(encoding="utf-8"))
ratio = float(prog["TraitRatio"])
copy = text["Traits"]

defaults = {
    "FanCone": 14, "PumpPellets": 1, "PumpReload": 0.25, "LoadPellets": 2, "LoadReload": 0.15,
    "ChokeCone": 10, "ChokeFloor": 6, "MeatRange": 140, "MeatBonus": 1, "MeatRangeCut": 1,
    "RicoBounces": 1, "GapeCone": 14, "DoubleGap": 0.12, "DoubleReload": 0.35,
    "KickForce": 110, "KickRange": 180, "StunTime": 0.45, "StunRange": 160,
    "HeapPellets": 3, "HeapReload": 0.40, "WasteRange": 80, "WasteBonus": 1, "WasteRangeCut": 1,
    "BreachPierce": 1, "SlugRadius": 22, "SlugDamage": 2, "SlugFalloffPad": 120,
    "LashHit": 1, "ProjectileRadius": 13,
}

def N(key):
    if key in traits:
        return traits[key]
    return defaults[key]

def tiers_of(key):
    return N(key)

def at(tiers, level):
    if tiers is None:
        return 0
    level4 = float(tiers.get("Level4", 0) or 0)
    if level >= 4 and level4 != 0:
        return level4
    a = float(tiers.get("Level1", 0) or 0)
    b = float(tiers.get("Level2", 0) or 0)
    c = float(tiers.get("Level3", 0) or 0)
    if level <= 0:
        return 0
    if level == 1:
        return a
    if level == 2:
        return b
    return c

def trait_mul(level):
    if level <= 0:
        return 1.0
    return ratio ** (level - 1)

def code(name):
    return copy.get(name, {}).get("Code") or name.upper()

def title(name):
    return copy.get(name, {}).get("Title") or code(name)

def blurb(name):
    return copy.get(name, {}).get("Blurb") or ""

def M(stat, op, order, value=0, growth="Flat", tiers=None, bias=0, hidden=False, passive=False, byhook=False, also=False, points=False, role=None, when=None, wmin=None):
    mod = {"Stat": stat, "Op": op, "Growth": growth, "Order": order}
    if growth == "Tiers":
        packed = {}
        for key in ("Level1", "Level2", "Level3", "Level4"):
            if tiers and key in tiers and tiers[key] != 0:
                packed[key] = tiers[key]
        mod["Tiers"] = packed
    else:
        mod["Value"] = value
    if bias:
        mod["Bias"] = bias
    if hidden:
        mod["Hidden"] = True
    if passive:
        mod["Passive"] = True
    if byhook:
        mod["ByHook"] = True
    if also:
        mod["AlsoFull"] = True
    if points:
        mod["Points"] = True
    if role:
        mod["Role"] = role
    if when is not None:
        mod["UseWhen"] = True
        mod["WhenStat"] = when
        mod["HasWhenMin"] = True
        mod["WhenMin"] = wmin
    return mod

icons = {
    "Split": "ui/traits/bounce.png", "Fan": "ui/traits/swipe.png", "Pump": "ui/traits/latemag.png",
    "Load": "ui/traits/cue.png", "Choke": "ui/traits/incurve.png", "Meat": "ui/traits/heavy.png",
    "Rico": "ui/traits/pinball.png", "Gape": "ui/traits/redirect.png", "Double": "ui/traits/echo.png",
    "Kick": "ui/traits/kick.png", "Stun": "ui/traits/freeze.png", "Heap": "ui/traits/shred.png",
    "Waste": "ui/traits/rim.png", "Breach": "ui/traits/hook.png", "Slug": "ui/traits/heavy.png",
    "Buck": "ui/traits/heavy.png",
    "Bore": "ui/traits/pierce.png", "Deep": "ui/traits/pierce.png", "Awl": "ui/traits/pierce.png",
    "Ram": "ui/traits/pierce.png", "Mass": "ui/traits/pierce.png", "Keel": "ui/traits/pierce.png",
    "Trace": "ui/traits/pierce.png",
    "Drum": "ui/traits/accel.png", "Belt": "ui/traits/accel.png", "Walk": "ui/traits/accel.png",
    "Spool": "ui/traits/accel.png", "Sight": "ui/traits/accel.png", "Bite": "ui/traits/accel.png",
    "Link": "ui/traits/accel.png",
    "Warhead": "ui/traits/explosive.png", "Mirv": "ui/traits/explosive.png", "Bloom": "ui/traits/explosive.png",
    "Scorch": "ui/traits/explosive.png", "Lance": "ui/traits/explosive.png", "Crater": "ui/traits/explosive.png",
    "Spot": "ui/traits/explosive.png",
    "Lash": "ui/traits/electric.png", "Sear": "ui/traits/electric.png", "Kiln": "ui/traits/electric.png",
    "Arc": "ui/traits/electric.png", "Fork": "ui/traits/electric.png", "Shunt": "ui/traits/electric.png",
    "Linger": "ui/traits/electric.png", "Cell": "ui/traits/electric.png",
    "Jack": "ui/traits/snap.png", "Slap": "ui/traits/snap.png", "Rack": "ui/traits/snap.png",
    "Draw": "ui/traits/snap.png", "Feed": "ui/traits/snap.png", "Eject": "ui/traits/snap.png",
    "Vent": "ui/traits/snap.png", "Cool": "ui/traits/snap.png", "Shuck": "ui/traits/snap.png",
    "Slam": "ui/traits/snap.png", "Snap": "ui/traits/snap.png",
    "Spin": "ui/traits/clockwise.png", "Rush": "ui/traits/step.png", "Dodge": "ui/traits/graze.png",
    "Pin": "ui/traits/stick.png",
}

cards = {}

def add(name, pack, sort, cap, pool=True, hook=None, flags=None, req=None, price=0, lap=0, weight=0, mods=None, notes=None):
    ident = code(name)
    cards[ident] = {
        "name": name,
        "Id": ident,
        "Title": title(name),
        "Blurb": blurb(name),
        "Pack": pack,
        "MaxLevel": cap,
        "InPool": pool,
        "Sort": sort,
        "Icon": icons[name],
        "UnlockLap": lap,
        "Price": price,
        "OwnedWeight": weight,
        "Hook": hook or "None",
        "Flags": flags or [],
        "Requires": [code(item) for item in (req or [])],
        "Excludes": set(),
        "Mods": mods or [],
        "Notes": notes or [],
    }

def note(text, sign=0):
    item = {"Text": text}
    if sign:
        item["Sign"] = sign
    return item

def cross(left, right):
    for a in left:
        cards[a]["Excludes"].update(b for b in right if b != a)
    for b in right:
        cards[b]["Excludes"].update(a for a in left if a != b)

no_bounce = [note("No bounces", -1)]
ticks_when = ("BeamTicks", 1)
splash_when = ("Splash", 1.001)
burst_when = ("Burst", 2)

add("Split", "Entry", 0, 4, mods=[
    M("Count", "Add", 100, growth="Tiers", tiers=tiers_of("SplitPellets")),
    M("Reload", "Add", 700, growth="Tiers", tiers=tiers_of("SplitReload")),
])
add("Fan", "Entry", 1, 1, mods=[M("Cone", "Add", 200, N("FanCone"))])
add("Pump", "Entry", 60, 1, pool=False, mods=[
    M("Count", "Add", 100, N("PumpPellets")),
    M("Reload", "Add", 700, N("PumpReload")),
])
add("Load", "Junior", 61, 1, pool=False, mods=[
    M("Count", "Add", 100, N("LoadPellets")),
    M("Reload", "Add", 700, N("LoadReload")),
])
add("Choke", "Junior", 2, 1, mods=[
    M("Cone", "Add", 210, -N("ChokeCone")),
    M("Cone", "Max", 211, N("ChokeFloor")),
])
add("Meat", "Junior", 3, 2, mods=[
    M("MeatRange", "Max", 900, N("MeatRange")),
    M("MeatBonus", "Add", 900, N("MeatBonus"), growth="Linear"),
    M("RangeCut", "Add", 0, N("MeatRangeCut"), growth="Linear"),
])
add("Rico", "Junior", 4, 1, mods=[M("Bounces", "Add", 500, N("RicoBounces"))])
add("Gape", "Warrior", 62, 1, pool=False, mods=[M("Cone", "Add", 220, N("GapeCone"))])
add("Double", "Warrior", 5, 1, flags=["DoublePump"], mods=[
    M("Reload", "Add", 700, N("DoubleReload")),
    M("Gap", "Set", 700, N("DoubleGap")),
])
add("Kick", "Warrior", 6, 1, mods=[
    M("KickForce", "Add", 900, N("KickForce")),
    M("KickRange", "Set", 0, N("KickRange"), passive=True),
])
add("Stun", "Warrior", 7, 1, mods=[
    M("StunTime", "Add", 900, N("StunTime")),
    M("StunRange", "Set", 0, N("StunRange"), passive=True),
])
add("Heap", "Abomination", 63, 1, pool=False, mods=[
    M("Count", "Add", 100, N("HeapPellets")),
    M("Reload", "Add", 700, N("HeapReload")),
])
add("Waste", "Abomination", 64, 1, pool=False, mods=[
    M("MeatRange", "Max", 900, N("WasteRange")),
    M("MeatBonus", "Add", 900, N("WasteBonus"), growth="Linear"),
    M("RangeCut", "Add", 0, N("WasteRangeCut"), growth="Linear"),
])
add("Breach", "Abomination", 65, 1, pool=False, mods=[M("Pierce", "Add", 600, N("BreachPierce"))])
add("Slug", "Abomination", 8, 1, hook="Slug", flags=["NoNail"], notes=[note("One shot")], mods=[
    M("Damage", "Add", 1000, N("SlugDamage"), growth="PerRemoved"),
    M("Radius", "Max", 1100, N("SlugRadius")),
    M("RangePad", "Set", 900, N("SlugFalloffPad")),
])
add("Buck", "Shotgun", 9, 3, mods=[
    M("Count", "Add", 100, growth="Tiers", tiers=tiers_of("BuckPellets"), bias=-1),
    M("Cone", "Add", 220, growth="Tiers", tiers=tiers_of("BuckCone")),
    M("RangeCut", "Add", 0, growth="Tiers", tiers=tiers_of("BuckRangeCut")),
])
add("Bore", "Rail", 10, 3, mods=[
    M("Pierce", "Set", 600, growth="Tiers", tiers=tiers_of("BorePierce")),
    M("Reload", "Add", 700, N("BoreReload"), growth="TraitRatio"),
    M("BoreWait", "Set", 700, N("BoreReload"), growth="TraitRatio", hidden=True),
])
add("Drum", "Rifle", 11, 3, flags=["Auto"], mods=[
    M("Burst", "Set", 1400, growth="Tiers", tiers=tiers_of("DrumBurst")),
    M("Reload", "Add", 700, N("DrumReload"), growth="TraitRatio"),
])
add("Warhead", "Rocket", 12, 3, flags=["FriendlySplash"], mods=[
    M("Splash", "Set", 1200, growth="Tiers", tiers=tiers_of("WarheadRadius")),
    M("Speed", "Mul", 800, growth="Tiers", tiers=tiers_of("WarheadSpeed")),
])
add("Lash", "Laser", 13, 3, flags=["Beam"], weight=N("LashRankWeight"), notes=no_bounce, mods=[
    M("Reload", "Set", 710, N("LashReload")),
    M("Bounces", "Set", 510, 0, hidden=True),
    M("BeamTick", "Set", 1500, growth="Tiers", tiers=tiers_of("LashTick")),
    M("BeamTicks", "Set", 1500, growth="Tiers", tiers=tiers_of("LashTicks")),
    M("BeamHit", "Set", 1500, max(1, N("LashHit"))),
    M("BeamRank", "Set", 705, 1, growth="Linear", hidden=True),
])
add("Pin", "Nailgun", 14, 3, hook="Pin", flags=["Nail"], mods=[
    M("Count", "Set", 110, growth="Tiers", tiers=tiers_of("PinNails"), byhook=True, role="nails"),
    M("Cone", "Set", 310, growth="Tiers", tiers=tiers_of("PinCone"), byhook=True, role="cone"),
    M("Bounces", "Add", 500, growth="Tiers", tiers=tiers_of("PinBounce")),
    M("Radius", "Set", 1050, N("PinRadius")),
    M("StickTime", "Set", 1050, N("PinStick")),
])
add("Rush", "Rifle", 15, 3, mods=[
    M("Speed", "Mul", 800, growth="Tiers", tiers=tiers_of("RushSpeed")),
    M("Reload", "Add", 700, N("RushReload"), growth="TraitRatio"),
])
add("Dodge", "Nailgun", 16, 3, mods=[M("Dodge", "Set", 900, growth="Tiers", tiers=tiers_of("DodgeChance"))])
add("Snap", "Nailgun", 17, 3, price=N("SnapPrice"), lap=N("SnapUnlockLap"), mods=[
    M("Reload", "Mul", 740, growth="Tiers", tiers=tiers_of("SnapReload"), points=True),
])
add("Mirv", "Rocket", 18, 1, flags=["PerPelletSplash"], req=["Warhead"], mods=[
    M("Splash", "Mul", 1201, N("MirvRadiusScale")),
    M("Speed", "Mul", 800, N("MirvSpeed")),
])
add("Bloom", "Rocket", 19, 1, req=["Warhead"], mods=[
    M("Splash", "Add", 1202, N("BloomRadius")),
    M("Reload", "Add", 700, N("BloomReload")),
])
add("Scorch", "Rocket", 20, 1, req=["Warhead"], mods=[
    M("Speed", "Mul", 800, N("ScorchSpeed")),
    M("Reload", "Add", 700, N("ScorchReload")),
    M("SplashDamage", "Max", 1260, N("ScorchDamage"), when="Splash", wmin=1.001),
])
add("Lance", "Rocket", 21, 1, flags=["NoFriendlySplash"], req=["Warhead"], notes=no_bounce, mods=[
    M("Damage", "Add", 900, N("LanceDamage")),
    M("Splash", "Mul", 1203, N("LanceRadiusScale")),
    M("Speed", "Mul", 800, N("LanceSpeed")),
    M("Reload", "Add", 700, N("LanceReload")),
    M("Bounces", "Set", 510, 0, hidden=True),
])
add("Crater", "Rocket", 22, 1, req=["Warhead"], notes=no_bounce, mods=[
    M("Radius", "Max", 1110, N("CraterBody")),
    M("Splash", "Add", 1204, N("CraterSplash")),
    M("Speed", "Mul", 800, N("CraterSpeed")),
    M("Bounces", "Set", 510, 0, hidden=True),
])
add("Spot", "Rocket", 23, 1, flags=["PointAim"], req=["Warhead"], notes=no_bounce, mods=[
    M("Speed", "Mul", 800, N("SpotSpeed")),
    M("Splash", "Add", 1205, N("SpotSplash")),
    M("SplashDamage", "Add", 1261, N("SpotSplashDamage"), when="Splash", wmin=1.001),
    M("Bounces", "Set", 510, 0, hidden=True),
])
add("Deep", "Rail", 24, 1, req=["Bore"], mods=[
    M("Pierce", "Add", 601, N("DeepPierce")),
    M("Reload", "Add", 700, N("DeepReload")),
])
add("Awl", "Rail", 25, 1, flags=["IgnoreArmor"], req=["Bore"], mods=[
    M("Speed", "Mul", 800, N("AwlSpeed")),
    M("Reload", "Add", 700, N("AwlReload")),
])
add("Ram", "Rail", 26, 1, flags=["RampPierce"], req=["Bore"], mods=[M("Speed", "Mul", 800, N("RamSpeed"))])
add("Mass", "Rail", 66, 3, pool=False, hook="Slug", flags=["NoNail"], notes=[note("One shot")], mods=[
    M("Damage", "Add", 1000, N("MassDamage"), growth="PerRemoved"),
    M("Speed", "Mul", 800, N("MassSpeed")),
    M("Reload", "Add", 700, N("MassReload")),
])
add("Keel", "Rail", 27, 1, req=["Bore"], notes=no_bounce, mods=[
    M("Damage", "Add", 900, N("KeelDamage")),
    M("Speed", "Mul", 800, N("KeelSpeed")),
    M("Bounces", "Set", 510, 0, hidden=True),
])
add("Trace", "Rail", 67, 3, pool=False, mods=[
    M("Speed", "Mul", 800, N("TraceSpeed")),
    M("Reload", "Add", 700, N("TraceReload")),
])
add("Belt", "Rifle", 28, 1, req=["Drum"], mods=[
    M("Burst", "Add", 1410, N("BeltBurst"), when="Burst", wmin=2),
    M("Reload", "Add", 700, N("BeltReload")),
])
add("Walk", "Rifle", 29, 1, req=["Drum"], mods=[
    M("WalkStep", "Add", 900, N("WalkCone")),
    M("Reload", "Add", 700, N("WalkReload")),
])
add("Spool", "Rifle", 30, 1, req=["Drum"], mods=[
    M("Cycle", "Mul", 1400, N("SpoolCycle")),
    M("Reload", "Add", 700, N("SpoolReload")),
])
add("Sight", "Rifle", 31, 1, flags=["Sight"], req=["Drum"], mods=[M("Speed", "Mul", 800, N("SightSpeed"))])
add("Bite", "Rifle", 32, 1, flags=["Bite"], req=["Drum"], mods=[M("Speed", "Mul", 800, N("BiteSpeed"))])
add("Link", "Rifle", 33, 1, pool=False, flags=["CommitBurst"], req=["Drum"], mods=[
    M("Reload", "Add", 700, N("LinkReload")),
    M("Cycle", "Mul", 1400, N("LinkCycle")),
])
add("Sear", "Laser", 34, 1, flags=["BeamSear"], req=["Lash"], mods=[M("Reload", "Add", 720, N("SearReload"), when="BeamRank", wmin=1)])
add("Kiln", "Laser", 35, 1, req=["Lash"], mods=[
    M("BeamKiln", "Set", 1500, N("KilnTick")),
    M("BeamWidth", "Mul", 1600, N("KilnWidth")),
    M("Reload", "Add", 720, N("KilnReload"), when="BeamRank", wmin=1),
])
add("Arc", "Laser", 36, 1, req=["Lash"], mods=[
    M("BeamArc", "Set", 1500, N("ArcRange")),
    M("BeamTick", "Mul", 1510, N("ArcTick"), when="BeamTicks", wmin=1),
])
add("Fork", "Laser", 37, 1, flags=["BeamFork"], req=["Lash"], mods=[M("Reload", "Add", 720, N("ForkReload"), when="BeamRank", wmin=1)])
add("Shunt", "Laser", 38, 1, flags=["BeamShunt"], req=["Lash"], mods=[
    M("BeamTick", "Mul", 1510, N("ShuntTick"), when="BeamTicks", wmin=1),
    M("BeamWidth", "Mul", 1600, N("ShuntWidth")),
])
add("Linger", "Laser", 39, 1, flags=["BeamLinger"], req=["Lash"], mods=[M("Reload", "Add", 720, N("LingerReload"), when="BeamRank", wmin=1)])
add("Cell", "Laser", 40, 1, req=["Lash"], mods=[
    M("BeamTicks", "Add", 1510, N("CellTicks"), when="BeamTicks", wmin=1),
    M("Reload", "Add", 720, N("CellReload"), when="BeamRank", wmin=1),
])
add("Jack", "Rocket", 41, 1, req=["Warhead"], mods=[
    M("Reload", "Mul", 730, N("JackReload")),
    M("Splash", "Mul", 1206, N("JackSplash")),
])
add("Slap", "Rocket", 42, 1, req=["Warhead", "Jack"], mods=[
    M("Reload", "Mul", 730, N("SlapReload")),
    M("Speed", "Mul", 800, N("SlapSpeed")),
])
add("Rack", "Rail", 43, 1, req=["Bore"], mods=[
    M("Reload", "Mul", 730, N("RackReload")),
    M("Speed", "Mul", 800, N("RackSpeed")),
])
add("Draw", "Rail", 44, 1, req=["Bore", "Rack"], mods=[
    M("Reload", "Mul", 730, N("DrawReload")),
    M("Pierce", "Add", 610, -N("DrawPierce")),
    M("Pierce", "Max", 611, 0),
])
add("Feed", "Rifle", 45, 1, req=["Drum"], mods=[
    M("Reload", "Mul", 730, N("FeedReload")),
    M("Cycle", "Mul", 1400, N("FeedCycle")),
])
add("Eject", "Rifle", 46, 1, req=["Drum", "Feed"], mods=[
    M("Reload", "Mul", 730, N("EjectReload")),
    M("Burst", "Add", 1420, -N("EjectBurst"), when="Burst", wmin=2),
    M("Burst", "Max", 1421, 1, when="Burst", wmin=2),
])
add("Vent", "Laser", 47, 1, req=["Lash"], mods=[
    M("Reload", "Mul", 730, N("VentReload")),
    M("BeamTick", "Mul", 1510, N("VentTick"), when="BeamTicks", wmin=1),
])
add("Cool", "Laser", 48, 1, req=["Lash", "Vent"], mods=[
    M("Reload", "Mul", 730, N("CoolReload")),
    M("BeamWidth", "Mul", 1600, N("CoolWidth")),
])
add("Shuck", "Shotgun", 49, 1, req=["Buck"], mods=[
    M("Cone", "Add", 400, N("ShuckCone")),
    M("Reload", "Mul", 730, N("ShuckReload")),
])
add("Slam", "Shotgun", 50, 1, req=["Buck", "Shuck"], mods=[
    M("Count", "Add", 410, -N("SlamPellets"), also=True),
    M("Count", "Max", 411, 1, also=True),
    M("Reload", "Mul", 730, N("SlamReload")),
])
add("Spin", "Nailgun", 68, 3, pool=False, mods=[
    M("SpinSpeed", "Add", 800, N("SpinBase")),
    M("SpinSpeed", "Add", 801, growth="Tiers", tiers=tiers_of("SpinBoost")),
    M("Reload", "Add", 700, N("SpinReload"), growth="TraitRatio"),
])

cross(["MIRV", "BLOOM", "SCORCH"], ["LANCE", "CRATER"])
cross(["DEEP", "AWL", "RAM"], ["KEEL"])
cross(["BELT", "WALK"], ["SPOOL", "SIGHT", "BITE"])
cross(["SEAR", "KILN"], ["ARC", "FORK"])
cross(["BORE"], ["LASH"])
cross(["MASS"], ["DEEP", "AWL", "RAM"])

if len(cards) != 60:
    raise SystemExit(f"expected 60 cards, got {len(cards)}")

INTS = {"Count", "Full", "Damage", "Pierce", "Bounces", "SplashDamage", "MeatBonus", "Burst", "BeamHit", "BeamTicks", "BeamRank"}
G_RELOAD = float(N("ReloadBase"))
G_MIN = float(N("ReloadMin"))
G_DAMAGE = int(N("BaseDamage"))
G_BOUNCE = int(N("MaxBouncesBase"))
G_ENERGY = float(N("EnergyBase"))
G_RADIUS = float(defaults["ProjectileRadius"])
G_CYCLE = float(N("DrumCycle"))
G_WIDTH = float(N("LashWidth"))
G_PAD = float(N("LashPad"))
G_PER = float(N("LashPerSecond"))
G_HOLD = float(N("LashMaxHold"))
G_RANGE = float(N("LashRange"))
G_HIT = int(N("LashHit"))

def amount(mod, level, removed):
    growth = mod.get("Growth", "Flat")
    value = float(mod.get("Value", 0) or 0)
    if growth == "Linear":
        raw = value * level
    elif growth == "TraitRatio":
        raw = value * trait_mul(level)
    elif growth == "Tiers":
        raw = at(mod.get("Tiers"), level)
    elif growth == "PerRemoved":
        raw = value * removed
    else:
        raw = value
    return raw + float(mod.get("Bias", 0) or 0)

def operate(current, op, raw, integer):
    if integer:
        now = int(current)
        step = int(raw)
        if op == "Mul":
            return int(now * raw)
        if op == "Set":
            return step
        if op == "Min":
            return min(now, step)
        if op == "Max":
            return max(now, step)
        return now + step
    if op == "Mul":
        return current * raw
    if op == "Set":
        return raw
    if op == "Min":
        return min(current, raw)
    if op == "Max":
        return max(current, raw)
    return current + raw

class State:
    def __init__(self, bonus):
        self.mirror = True
        self.collapsed = False
        self.mask = set()
        self.count = 1
        self.full = 1
        self.cone = 0.0
        self.damage = max(1, G_DAMAGE + bonus)
        self.pierce = 0
        self.bounces = G_BOUNCE
        self.reload = G_RELOAD
        self.bore_wait = 0.0
        self.speed = 1.0
        self.radius = G_RADIUS
        self.splash = 0.0
        self.splash_damage = 0
        self.range_cut = 0.0
        self.range_pad = 0.0
        self.meat_range = 0.0
        self.meat_bonus = 0
        self.kick_force = 0.0
        self.kick_range = 0.0
        self.stun_time = 0.0
        self.stun_range = 0.0
        self.cycle = G_CYCLE
        self.burst = 1
        self.walk = 0.0
        self.beam_hit = 0
        self.beam_tick = 1.0
        self.beam_ticks = 0
        self.beam_width = G_WIDTH
        self.beam_arc = 0.0
        self.beam_kiln = 1.0
        self.beam_rank = 0
        self.stick = 0.0
        self.dodge = 0.0
        self.gap = 0.0
        self.spin = 0.0
        self.energy = G_ENERGY

    def get(self, stat):
        return {
            "Count": self.count, "Full": self.full, "Cone": self.cone, "Damage": self.damage,
            "Pierce": self.pierce, "Bounces": self.bounces, "Reload": self.reload, "BoreWait": self.bore_wait,
            "Speed": self.speed, "Radius": self.radius, "Splash": self.splash, "SplashDamage": self.splash_damage,
            "RangeCut": self.range_cut, "RangePad": self.range_pad, "MeatRange": self.meat_range, "MeatBonus": self.meat_bonus,
            "KickForce": self.kick_force, "KickRange": self.kick_range, "StunTime": self.stun_time, "StunRange": self.stun_range,
            "Cycle": self.cycle, "Burst": self.burst, "WalkStep": self.walk, "BeamHit": self.beam_hit,
            "BeamTick": self.beam_tick, "BeamTicks": self.beam_ticks, "BeamWidth": self.beam_width, "BeamArc": self.beam_arc,
            "BeamKiln": self.beam_kiln, "BeamRank": self.beam_rank, "StickTime": self.stick, "Dodge": self.dodge,
            "Gap": self.gap, "SpinSpeed": self.spin, "Energy": self.energy,
        }[stat]

    def set(self, stat, value):
        if stat == "Count": self.count = int(value)
        elif stat == "Full": self.full = int(value)
        elif stat == "Cone": self.cone = value
        elif stat == "Damage": self.damage = int(value)
        elif stat == "Pierce": self.pierce = int(value)
        elif stat == "Bounces": self.bounces = int(value)
        elif stat == "Reload": self.reload = value
        elif stat == "BoreWait": self.bore_wait = value
        elif stat == "Speed": self.speed = value
        elif stat == "Radius": self.radius = value
        elif stat == "Splash": self.splash = value
        elif stat == "SplashDamage": self.splash_damage = int(value)
        elif stat == "RangeCut": self.range_cut = value
        elif stat == "RangePad": self.range_pad = value
        elif stat == "MeatRange": self.meat_range = value
        elif stat == "MeatBonus": self.meat_bonus = int(value)
        elif stat == "KickForce": self.kick_force = value
        elif stat == "KickRange": self.kick_range = value
        elif stat == "StunTime": self.stun_time = value
        elif stat == "StunRange": self.stun_range = value
        elif stat == "Cycle": self.cycle = value
        elif stat == "Burst": self.burst = int(value)
        elif stat == "WalkStep": self.walk = value
        elif stat == "BeamHit": self.beam_hit = int(value)
        elif stat == "BeamTick": self.beam_tick = value
        elif stat == "BeamTicks": self.beam_ticks = int(value)
        elif stat == "BeamWidth": self.beam_width = value
        elif stat == "BeamArc": self.beam_arc = value
        elif stat == "BeamKiln": self.beam_kiln = value
        elif stat == "BeamRank": self.beam_rank = int(value)
        elif stat == "StickTime": self.stick = value
        elif stat == "Dodge": self.dodge = value
        elif stat == "Gap": self.gap = value
        elif stat == "SpinSpeed": self.spin = value
        else: self.energy = value

    def apply(self, mod, level):
        if mod.get("UseWhen"):
            current = self.get(mod["WhenStat"])
            if mod.get("HasWhenMin") and current < mod["WhenMin"]:
                return
            if mod.get("HasWhenMax") and current > mod["WhenMax"]:
                return
        raw = amount(mod, level, max(0, self.full - self.count))
        if mod["Stat"] == "Count":
            self.count = int(operate(self.count, mod["Op"], raw, True))
            if self.mirror:
                self.full = self.count
            elif mod.get("AlsoFull"):
                self.full = int(operate(self.full, mod["Op"], raw, True))
            return
        self.set(mod["Stat"], operate(self.get(mod["Stat"]), mod["Op"], raw, mod["Stat"] in INTS))

    def recipe(self):
        beam = "Beam" in self.mask
        auto = "Auto" in self.mask and not beam
        nail = "Nail" in self.mask and "NoNail" not in self.mask
        return {
            "beam": beam,
            "auto": auto,
            "double_pump": "DoublePump" in self.mask and not auto and not beam,
            "count": self.count,
            "cone": self.cone,
            "damage": max(1, self.damage),
            "pierce": self.pierce,
            "bounces": self.bounces,
            "energy": self.energy,
            "speed": self.speed,
            "spin": self.spin,
            "radius": self.radius,
            "splash": self.splash,
            "splash_damage": self.splash_damage,
            "friendly": "FriendlySplash" in self.mask and "NoFriendlySplash" not in self.mask,
            "per_pellet": "PerPelletSplash" in self.mask,
            "point": "PointAim" in self.mask,
            "ignore": "IgnoreArmor" in self.mask,
            "ramp": "RampPierce" in self.mask,
            "nail": nail,
            "stick": self.stick if nail else 0,
            "range_cut": max(0, self.range_cut),
            "range_pad": self.range_pad,
            "falloff": 0,
            "meat_range": self.meat_range,
            "meat_bonus": self.meat_bonus,
            "kick_force": self.kick_force,
            "kick_range": self.kick_range,
            "stun_time": self.stun_time,
            "stun_range": self.stun_range,
            "cycle": self.cycle,
            "burst": self.burst,
            "walk": self.walk,
            "sight": "Sight" in self.mask,
            "bite": "Bite" in self.mask,
            "commit": "CommitBurst" in self.mask,
            "reload": max(G_MIN, self.reload),
            "bore_wait": self.bore_wait,
            "beam_pad": G_PAD,
            "beam_per": G_PER,
            "beam_hold": G_HOLD,
            "beam_hit": self.beam_hit,
            "beam_tick": self.beam_tick,
            "beam_ticks": self.beam_ticks,
            "beam_range": G_RANGE,
            "beam_width": self.beam_width,
            "beam_rank": self.beam_rank,
            "beam_sear": "BeamSear" in self.mask,
            "beam_kiln": self.beam_kiln,
            "beam_arc": self.beam_arc,
            "beam_fork": "BeamFork" in self.mask,
            "beam_shunt": "BeamShunt" in self.mask,
            "beam_linger": "BeamLinger" in self.mask,
            "dodge": self.dodge,
            "gap": self.gap,
        }

def read_role(card, role, level):
    for mod in card["Mods"]:
        if mod.get("Role") == role:
            return amount(mod, level, 0)
    return 0

def pipe(levels, bonus=0):
    state = State(bonus)
    steps = []
    index = 0
    ordered = sorted(cards.values(), key=lambda card: (card["Sort"], card["Id"]))
    owned = [(cards[ident], level) for ident, level in levels.items() if level > 0 and ident in cards]
    for card in ordered:
        for mod in card["Mods"]:
            if mod.get("Passive") and not mod.get("ByHook"):
                steps.append((mod["Order"], card["Sort"], index, "mod", mod, card, 1))
                index += 1
    for card, level in owned:
        if card["Hook"] == "Pin":
            steps.append((110, card["Sort"], index, "pin_early", None, card, level)); index += 1
            steps.append((310, card["Sort"], index, "pin_late", None, card, level)); index += 1
        if card["Hook"] == "Slug":
            steps.append((300, card["Sort"], index, "slug", None, card, level)); index += 1
        for mod in card["Mods"]:
            if not mod.get("Passive") and not mod.get("ByHook"):
                steps.append((mod["Order"], card["Sort"], index, "mod", mod, card, level))
                index += 1
        steps.append((8000, card["Sort"], index, "flags", None, card, level))
        index += 1
    steps.append((150, 10 ** 9, index, "freeze", None, None, 0)); index += 1
    steps.append((1250, 10 ** 9, index, "splash", None, None, 0))
    for step in sorted(steps):
        kind = step[3]
        if kind == "mod":
            state.apply(step[4], step[6])
        elif kind == "pin_early":
            if state.count <= 1:
                state.full = max(1, int(read_role(step[5], "nails", step[6])))
        elif kind == "pin_late":
            if not state.collapsed and state.count <= 1:
                state.count = max(1, state.full)
                state.cone = read_role(step[5], "cone", step[6])
        elif kind == "slug":
            state.count = 1
            state.cone = 0
            state.collapsed = True
        elif kind == "flags":
            state.mask.update(step[5]["Flags"])
        elif kind == "freeze":
            state.mirror = False
        elif kind == "splash":
            state.splash_damage = 1 if state.splash > 1 else 0
    return state.recipe()

OFF = {"LINK", "PUMP", "LOAD", "GAPE", "HEAP", "WASTE", "BREACH", "MASS", "TRACE"}
POOL = [
    "SPLIT", "FAN", "CHOKE", "MEAT", "RICO", "DOUBLE", "KICK", "STUN", "SLUG",
    "BUCK", "BORE", "DRUM", "WARHEAD", "LASH", "PIN", "RUSH", "DODGE", "SNAP",
    "MIRV", "BLOOM", "SCORCH", "LANCE", "CRATER", "SPOT",
    "DEEP", "AWL", "RAM", "KEEL",
    "BELT", "WALK", "SPOOL", "SIGHT", "BITE",
    "SEAR", "KILN", "ARC", "FORK", "SHUNT", "LINGER", "CELL",
    "JACK", "SLAP", "RACK", "DRAW", "FEED", "EJECT", "VENT", "COOL", "SHUCK", "SLAM",
]

def lv(levels, ident):
    if ident in OFF:
        return 0
    return levels.get(ident, 0)

def has(levels, ident):
    return lv(levels, ident) > 0

def legacy(levels, bonus=0):
    buck = lv(levels, "BUCK")
    bore = lv(levels, "BORE")
    drum = lv(levels, "DRUM")
    warhead = lv(levels, "WARHEAD")
    lash = lv(levels, "LASH")
    pin = lv(levels, "PIN")
    rush = lv(levels, "RUSH")
    split = lv(levels, "SPLIT")
    slug = has(levels, "SLUG")
    meat = lv(levels, "MEAT")
    count = 1
    if split > 0:
        count += max(0, int(at(tiers_of("SplitPellets"), split)))
    if buck > 0:
        count += max(0, int(at(tiers_of("BuckPellets"), buck)) - 1)
    full = count
    if count <= 1 and pin > 0:
        full = max(1, int(at(tiers_of("PinNails"), pin)))
    cone = 0.0
    if has(levels, "FAN"):
        cone += N("FanCone")
    if has(levels, "CHOKE"):
        cone = max(N("ChokeFloor"), cone - N("ChokeCone"))
    if buck > 0:
        cone += at(tiers_of("BuckCone"), buck)
    if slug:
        count = 1
        cone = 0
    elif count <= 1 and pin > 0:
        count = full
        cone = at(tiers_of("PinCone"), pin)
    if has(levels, "SHUCK"):
        cone += N("ShuckCone")
    if has(levels, "SLAM"):
        count = max(1, count - N("SlamPellets"))
        full = max(1, full - N("SlamPellets"))
    bounces = G_BOUNCE
    if pin > 0:
        bounces += int(at(tiers_of("PinBounce"), pin))
    if has(levels, "RICO"):
        bounces += N("RicoBounces")
    if lash > 0 or has(levels, "LANCE") or has(levels, "CRATER") or has(levels, "SPOT") or has(levels, "KEEL"):
        bounces = 0
    pierce = 0 if bore <= 0 else int(at(tiers_of("BorePierce"), bore))
    if has(levels, "DEEP"):
        pierce += N("DeepPierce")
    if has(levels, "DRAW"):
        pierce = max(0, pierce - N("DrawPierce"))
    reload = G_RELOAD
    bore_wait = 0.0
    if bore > 0:
        bore_wait = N("BoreReload") * trait_mul(bore)
        reload += bore_wait
    if drum > 0:
        reload += N("DrumReload") * trait_mul(drum)
    if rush > 0:
        reload += N("RushReload") * trait_mul(rush)
    if split > 0:
        reload += at(tiers_of("SplitReload"), split)
    if has(levels, "DOUBLE"):
        reload += N("DoubleReload")
    if has(levels, "BLOOM"):
        reload += N("BloomReload")
    if has(levels, "SCORCH"):
        reload += N("ScorchReload")
    if has(levels, "LANCE"):
        reload += N("LanceReload")
    if has(levels, "DEEP"):
        reload += N("DeepReload")
    if has(levels, "AWL"):
        reload += N("AwlReload")
    if has(levels, "BELT"):
        reload += N("BeltReload")
    if has(levels, "WALK"):
        reload += N("WalkReload")
    if has(levels, "SPOOL"):
        reload += N("SpoolReload")
    if has(levels, "LINK"):
        reload += N("LinkReload")
    if lash > 0:
        reload = N("LashReload")
        if has(levels, "SEAR"):
            reload += N("SearReload")
        if has(levels, "KILN"):
            reload += N("KilnReload")
        if has(levels, "FORK"):
            reload += N("ForkReload")
        if has(levels, "LINGER"):
            reload += N("LingerReload")
        if has(levels, "CELL"):
            reload += N("CellReload")
    for ident, key in (
        ("JACK", "JackReload"), ("SLAP", "SlapReload"), ("RACK", "RackReload"), ("DRAW", "DrawReload"),
        ("FEED", "FeedReload"), ("EJECT", "EjectReload"), ("VENT", "VentReload"), ("COOL", "CoolReload"),
        ("SHUCK", "ShuckReload"), ("SLAM", "SlamReload"),
    ):
        if has(levels, ident):
            reload *= N(key)
    snap = lv(levels, "SNAP")
    if snap > 0:
        reload *= at(tiers_of("SnapReload"), snap)
    speed = 1.0
    if warhead > 0:
        speed *= at(tiers_of("WarheadSpeed"), warhead)
    for ident, key in (
        ("MIRV", "MirvSpeed"), ("SCORCH", "ScorchSpeed"), ("LANCE", "LanceSpeed"), ("CRATER", "CraterSpeed"),
        ("SPOT", "SpotSpeed"), ("AWL", "AwlSpeed"), ("RAM", "RamSpeed"), ("KEEL", "KeelSpeed"),
        ("SIGHT", "SightSpeed"), ("BITE", "BiteSpeed"), ("SLAP", "SlapSpeed"), ("RACK", "RackSpeed"),
    ):
        if has(levels, ident):
            speed *= N(key)
    if rush > 0:
        speed *= at(tiers_of("RushSpeed"), rush)
    range_cut = 0.0
    meat_range = 0.0
    meat_bonus = 0
    if meat > 0:
        meat_range = max(meat_range, N("MeatRange"))
        meat_bonus += N("MeatBonus") * meat
        range_cut += N("MeatRangeCut") * meat
    if buck > 0:
        range_cut += at(tiers_of("BuckRangeCut"), buck)
    damage = max(1, G_DAMAGE + bonus)
    if slug:
        damage += N("SlugDamage") * max(0, full - count)
    if has(levels, "LANCE"):
        damage += N("LanceDamage")
    if has(levels, "KEEL"):
        damage += N("KeelDamage")
    radius = N("PinRadius") if pin > 0 else G_RADIUS
    if slug:
        radius = max(radius, N("SlugRadius"))
    if has(levels, "CRATER"):
        radius = max(radius, N("CraterBody"))
    splash = at(tiers_of("WarheadRadius"), warhead)
    if has(levels, "MIRV"):
        splash *= N("MirvRadiusScale")
    if has(levels, "BLOOM"):
        splash += N("BloomRadius")
    if has(levels, "LANCE"):
        splash *= N("LanceRadiusScale")
    if has(levels, "CRATER"):
        splash += N("CraterSplash")
    if has(levels, "SPOT"):
        splash += N("SpotSplash")
    if has(levels, "JACK"):
        splash *= N("JackSplash")
    splash_damage = 1 if splash > 1 else 0
    if has(levels, "SCORCH") and splash > 1:
        splash_damage = max(splash_damage, N("ScorchDamage"))
    if has(levels, "SPOT") and splash > 1:
        splash_damage += N("SpotSplashDamage")
    cycle = G_CYCLE
    if has(levels, "SPOOL"):
        cycle *= N("SpoolCycle")
    if has(levels, "LINK"):
        cycle *= N("LinkCycle")
    if has(levels, "FEED"):
        cycle *= N("FeedCycle")
    burst = 1 if drum <= 0 else max(1, int(at(tiers_of("DrumBurst"), drum)))
    if drum > 0 and has(levels, "BELT"):
        burst += N("BeltBurst")
    if drum > 0 and has(levels, "EJECT"):
        burst = max(1, burst - N("EjectBurst"))
    beam_ticks = 0
    beam_tick = 1.0
    if lash > 0:
        beam_ticks = max(1, int(at(tiers_of("LashTicks"), lash)))
        if has(levels, "CELL"):
            beam_ticks += N("CellTicks")
        beam_tick = at(tiers_of("LashTick"), lash)
        if has(levels, "ARC"):
            beam_tick *= N("ArcTick")
        if has(levels, "SHUNT"):
            beam_tick *= N("ShuntTick")
        if has(levels, "VENT"):
            beam_tick *= N("VentTick")
    width = G_WIDTH
    if has(levels, "KILN"):
        width *= N("KilnWidth")
    if has(levels, "SHUNT"):
        width *= N("ShuntWidth")
    if has(levels, "COOL"):
        width *= N("CoolWidth")
    return {
        "beam": lash > 0,
        "auto": drum > 0 and lash <= 0,
        "double_pump": has(levels, "DOUBLE") and drum <= 0 and lash <= 0,
        "count": count,
        "cone": cone,
        "damage": damage,
        "pierce": pierce,
        "bounces": bounces,
        "energy": G_ENERGY,
        "speed": speed,
        "spin": 0.0,
        "radius": radius,
        "splash": splash,
        "splash_damage": splash_damage,
        "friendly": warhead > 0 and not has(levels, "LANCE"),
        "per_pellet": has(levels, "MIRV"),
        "point": has(levels, "SPOT"),
        "ignore": has(levels, "AWL"),
        "ramp": has(levels, "RAM"),
        "nail": pin > 0 and not slug,
        "stick": N("PinStick") if pin > 0 and not slug else 0,
        "range_cut": max(0, range_cut),
        "range_pad": N("SlugFalloffPad") if slug else 0,
        "falloff": 0,
        "meat_range": meat_range,
        "meat_bonus": meat_bonus,
        "kick_force": N("KickForce") if has(levels, "KICK") else 0,
        "kick_range": N("KickRange"),
        "stun_time": N("StunTime") if has(levels, "STUN") else 0,
        "stun_range": N("StunRange"),
        "cycle": cycle,
        "burst": burst,
        "walk": N("WalkCone") if has(levels, "WALK") else 0,
        "sight": has(levels, "SIGHT"),
        "bite": has(levels, "BITE"),
        "commit": has(levels, "LINK"),
        "reload": max(G_MIN, reload),
        "bore_wait": bore_wait,
        "beam_pad": G_PAD,
        "beam_per": G_PER,
        "beam_hold": G_HOLD,
        "beam_hit": max(1, G_HIT) if lash > 0 else 0,
        "beam_tick": beam_tick,
        "beam_ticks": beam_ticks,
        "beam_range": G_RANGE,
        "beam_width": width,
        "beam_rank": lash,
        "beam_sear": has(levels, "SEAR"),
        "beam_kiln": N("KilnTick") if has(levels, "KILN") else 1,
        "beam_arc": N("ArcRange") if has(levels, "ARC") else 0,
        "beam_fork": has(levels, "FORK"),
        "beam_shunt": has(levels, "SHUNT"),
        "beam_linger": has(levels, "LINGER"),
        "dodge": at(tiers_of("DodgeChance"), lv(levels, "DODGE")) if has(levels, "DODGE") else 0,
        "gap": N("DoubleGap") if has(levels, "DOUBLE") else 0,
    }

CLUSTER = {"MIRV", "BLOOM", "SCORCH"}
LANCE = {"LANCE", "CRATER"}
DEEP = {"DEEP", "AWL", "RAM"}
SWEEP = {"BELT", "WALK"}
TRACK = {"SPOOL", "SIGHT", "BITE"}
BRAND = {"SEAR", "KILN"}
ARC = {"ARC", "FORK"}

def old_blocked(ident, owned):
    if ident in OFF:
        return True
    def owns(group):
        return any(item in owned for item in group)
    if ident in {"SHUCK", "SLAM"} and "BUCK" not in owned:
        return True
    if ident in CLUSTER | LANCE | {"SPOT", "JACK", "SLAP"} and "WARHEAD" not in owned:
        return True
    if ident == "LASH" and "BORE" in owned:
        return True
    if ident == "BORE" and "LASH" in owned:
        return True
    if ident in CLUSTER and owns(LANCE):
        return True
    if ident in LANCE and owns(CLUSTER):
        return True
    if ident in DEEP | {"KEEL", "RACK", "DRAW"} and "BORE" not in owned:
        return True
    if ident in DEEP and "KEEL" in owned:
        return True
    if ident == "KEEL" and owns(DEEP):
        return True
    if ident in SWEEP | TRACK | {"LINK", "FEED", "EJECT"} and "DRUM" not in owned:
        return True
    if ident in SWEEP and owns(TRACK):
        return True
    if ident in TRACK and owns(SWEEP):
        return True
    if ident in BRAND | ARC | {"SHUNT", "LINGER", "CELL", "VENT", "COOL"} and "LASH" not in owned:
        return True
    if ident in BRAND and owns(ARC):
        return True
    if ident in ARC and owns(BRAND):
        return True
    if ident == "SLAM" and "SHUCK" not in owned:
        return True
    if ident == "SLAP" and "JACK" not in owned:
        return True
    if ident == "DRAW" and "RACK" not in owned:
        return True
    if ident == "EJECT" and "FEED" not in owned:
        return True
    if ident == "COOL" and "VENT" not in owned:
        return True
    return False

def symmetric():
    link = {ident: set(cards[ident]["Excludes"]) for ident in cards}
    for ident, others in list(link.items()):
        for other in others:
            link[other].add(ident)
    return link

LINKS = symmetric()

def new_blocked(ident, owned):
    card = cards[ident]
    if not card["InPool"]:
        return True
    for req in card["Requires"]:
        if req not in owned:
            return True
    for other in LINKS[ident]:
        if other in owned:
            return True
    return False

def near(a, b):
    if isinstance(a, bool) or isinstance(b, bool):
        return a is b
    if isinstance(a, str) or isinstance(b, str):
        return a == b
    return abs(float(a) - float(b)) <= 1e-3

def compare(levels, bonus=0):
    old = legacy(levels, bonus)
    new = pipe(levels, bonus)
    bad = [f"{key}: legacy {old[key]} new {new[key]}" for key in old if not near(old[key], new[key])]
    return bad

fails = []

def check(levels, bonus=0, **expect):
    bad = compare(levels, bonus)
    if bad:
        fails.append((dict(levels), bonus, bad))
        return
    got = pipe(levels, bonus)
    for key, value in expect.items():
        if not near(got[key], value):
            fails.append((dict(levels), bonus, [f"{key}: expected {value} got {got[key]}"]))

check({})
check({}, kick_range=180, stun_range=160, kick_force=0, count=1, reload=0.7, radius=13, burst=1, beam_tick=1, beam_kiln=1, beam_width=8)
check({"CHOKE": 1}, cone=6)
check({"FAN": 1, "CHOKE": 1, "BUCK": 1}, count=2, cone=16)
check({"FAN": 1, "CHOKE": 1}, cone=6)
check({"SPLIT": 4, "SLUG": 1}, count=1, damage=15, radius=22, cone=0)
check({"PIN": 1}, count=2, cone=8, bounces=2, radius=6, stick=0.6, nail=True)
check({"PIN": 1, "SLUG": 1}, count=1, damage=3, radius=22, nail=False, stick=0, cone=0)
check({"PIN": 1, "SPLIT": 1}, count=2, radius=6, bounces=2, cone=0)
check({"PIN": 1, "FAN": 1}, cone=8, count=2)
check({"PIN": 1, "SHUCK": 1}, cone=16, count=2)
check({"LASH": 1, "BORE": 1}, reload=0.3, bore_wait=0.35, pierce=1, beam=True, bounces=0)
check({"LASH": 1, "SEAR": 1, "KILN": 1, "FORK": 1, "LINGER": 1, "CELL": 1}, reload=0.3 + 0.1 + 0.06 + 0.08 + 0.12 + 0.1)
check({"BORE": 1}, pierce=1, bore_wait=0.35)
check({"LASH": 1, "SNAP": 3}, reload=0.2)
check({"JACK": 1, "SNAP": 1}, reload=0.7 * 0.8 * 0.8)
check({"WARHEAD": 1, "MIRV": 1, "BLOOM": 1}, splash=90 * 0.55 + 80, per_pellet=True)
check({"WARHEAD": 1, "MIRV": 1, "BLOOM": 1, "LANCE": 1}, splash=(90 * 0.55 + 80) * 0.7, friendly=False)
check({"WARHEAD": 1, "SPOT": 1, "JACK": 1}, splash=(90 + 40) * 0.8, splash_damage=2, point=True)
check({"WARHEAD": 1, "SCORCH": 1, "SPOT": 1}, splash_damage=3)
check({"WARHEAD": 1}, splash=90, splash_damage=1, friendly=True, speed=0.78)
check({"DRUM": 1, "BELT": 1, "EJECT": 1}, burst=5, auto=True)
check({"DRUM": 1, "DOUBLE": 1}, auto=True, double_pump=False)
check({"DOUBLE": 1}, double_pump=True, gap=0.12)
check({"SLUG": 1, "SPLIT": 4, "SLAM": 1}, damage=13, count=1)
check({"BUCK": 1, "SHUCK": 1, "SLAM": 1}, count=1, cone=18)
check({"MEAT": 2}, meat_range=140, meat_bonus=2, range_cut=2)
check({"DODGE": 3}, dodge=0.32)
check({"KEEL": 1, "BORE": 1}, damage=6, speed=0.6, bounces=0)
check({"CRATER": 1}, radius=22)
check({"LASH": 1, "CELL": 1}, beam_ticks=6)
check({"LASH": 1, "ARC": 1}, beam_tick=0.6 * 1.15, beam_arc=220)
check({"KILN": 1}, beam_width=8 * 0.75, beam_kiln=0.75)
check({"COOL": 1}, beam_width=8 * 0.8)
check({"FEED": 1, "SPOOL": 1}, cycle=0.16 * 0.65 * 1.2)
check({"RUSH": 2}, speed=1.4)
check({"BORE": 2}, pierce=1)
check({}, bonus=4, damage=5)
check({"SLUG": 1}, damage=1, radius=22, range_pad=120)
check({"DRUM": 1, "BELT": 1}, burst=6)
check({"LASH": 3, "SEAR": 1, "KILN": 1, "CELL": 1, "VENT": 1, "COOL": 1})
check({"BORE": 3, "DEEP": 1, "RACK": 1, "DRAW": 1})
check({"WARHEAD": 3, "MIRV": 1, "BLOOM": 1, "SCORCH": 1, "SPOT": 1, "JACK": 1, "SLAP": 1})
check({"DRUM": 3, "BELT": 1, "EJECT": 1, "FEED": 1, "SPOOL": 1, "SIGHT": 1})
check({"PIN": 3, "SLUG": 1})
check({"SNAP": 3, "JACK": 1, "SLAP": 1})
check({"MEAT": 2, "BUCK": 3})

for ident in POOL:
    cap = cards[ident]["MaxLevel"]
    for rank in range(1, cap + 1):
        check({ident: rank})

kitchen = {ident: cards[ident]["MaxLevel"] for ident in POOL}
check(kitchen)
check(kitchen, bonus=7)

rng = random.Random(260926)
for _ in range(300):
    picked = rng.sample(POOL, rng.randint(0, 12))
    check({ident: rng.randint(1, cards[ident]["MaxLevel"]) for ident in picked}, bonus=rng.randint(0, 3))

for _ in range(200):
    owned = {}
    order = POOL[:]
    rng.shuffle(order)
    for ident in order:
        if old_blocked(ident, set(owned)):
            continue
        if rng.random() < 0.4:
            owned[ident] = rng.randint(1, cards[ident]["MaxLevel"])
    check(owned)

blocked_fail = []

def check_blocked(owned):
    have = set(owned)
    for ident in POOL:
        if old_blocked(ident, have) != new_blocked(ident, have):
            blocked_fail.append((sorted(have), ident, old_blocked(ident, have), new_blocked(ident, have)))

check_blocked([])
check_blocked(["BUCK"])
check_blocked(["BUCK", "SHUCK"])
check_blocked(["MIRV", "WARHEAD"])
check_blocked(["LANCE", "WARHEAD"])
check_blocked(["BORE", "KEEL"])
check_blocked(["BORE", "DEEP"])
check_blocked(["DRUM", "BELT"])
check_blocked(["DRUM", "SPOOL"])
check_blocked(["LASH", "SEAR"])
check_blocked(["LASH", "ARC"])
check_blocked(["BORE", "LASH"])
check_blocked(["LASH", "VENT"])
check_blocked(["DRUM", "FEED"])
check_blocked(["WARHEAD", "JACK"])
check_blocked(["BORE", "RACK"])
for _ in range(200):
    check_blocked(rng.sample(POOL, rng.randint(0, 10)))

for ident in OFF:
    if not new_blocked(ident, set()):
        blocked_fail.append(([], ident, True, False))

if len(POOL) != 50:
    fails.append(({}, 0, [f"pool {len(POOL)}"]))

if fails or blocked_fail:
    for item in fails[:20]:
        print("RECIPE", item[0], "bonus", item[1])
        for line in item[2][:12]:
            print(" ", line)
    for item in blocked_fail[:20]:
        print("BLOCKED", item)
    raise SystemExit(f"{len(fails)} recipe mismatches, {len(blocked_fail)} blocked mismatches")

out = root / "Assets" / "trinkets"
out.mkdir(parents=True, exist_ok=True)
written = 0
for card in cards.values():
    exclude_ids = sorted(card["Excludes"], key=lambda ident: (cards[ident]["Sort"], ident))
    doc = {
        "Id": card["Id"],
        "Title": card["Title"],
        "Pack": card["Pack"],
        "MaxLevel": card["MaxLevel"],
        "Sort": card["Sort"],
        "Icon": card["Icon"],
    }
    if card["Blurb"]:
        doc["Blurb"] = card["Blurb"]
    if not card["InPool"]:
        doc["InPool"] = False
    if card["Price"]:
        doc["Price"] = card["Price"]
    if card["OwnedWeight"]:
        doc["OwnedWeight"] = card["OwnedWeight"]
    if card["UnlockLap"]:
        doc["UnlockLap"] = card["UnlockLap"]
    if card["Hook"] != "None":
        doc["Hook"] = card["Hook"]
    if card["Flags"]:
        doc["Flags"] = card["Flags"]
    refs = []
    if card["Requires"]:
        doc["Requires"] = [f"trinkets/{ident.lower()}.trinket" for ident in card["Requires"]]
        refs.extend(doc["Requires"])
    if exclude_ids:
        doc["Excludes"] = [f"trinkets/{ident.lower()}.trinket" for ident in exclude_ids]
        refs.extend(doc["Excludes"])
    doc["Mods"] = card["Mods"]
    if card["Notes"]:
        doc["Notes"] = card["Notes"]
    doc["__references"] = refs
    doc["__version"] = 0
    path = out / f"{card['Id'].lower()}.trinket"
    path.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(json.dumps({"guid": str(uuid.uuid4())}, indent=2) + "\n", encoding="utf-8")
    written += 1

def rarity(pack):
    table = copy["Rarity"]
    if pack in ("Entry", "Rifle", "Shotgun"):
        return table["Common"]
    if pack in ("Junior", "Nailgun"):
        return table["Uncommon"]
    if pack == "Abomination":
        return table["Epic"]
    return table["Rare"]

def fmt(value):
    if isinstance(value, bool):
        return value
    number = float(value)
    if abs(number - round(number)) < 0.001:
        return str(int(round(number)))
    return f"{number:.2f}".rstrip("0").rstrip(".")

lines = ["# Trinkets", "", "## Globals", "", "| Field | Value |", "| --- | --- |"]
for key in ("MaxLevel", "BaseDamage", "MaxBouncesBase", "EnergyBase", "ReloadBase", "ReloadMin", "DrumCycle", "LashWidth"):
    lines.append(f"| {key} | {fmt(traits[key])} |")
lines.append(f"| ProjectileRadius | {fmt(G_RADIUS)} |")
lines += ["", "## Packs", "", "| Pack | Rarity | Price | Weight | Single |", "| --- | --- | --- | --- | --- |"]
for pack in ("Rifle", "Shotgun", "Nailgun", "Laser", "Rail", "Rocket", "Entry", "Junior", "Warrior", "Abomination"):
    stats = traits[pack]
    single = "yes" if pack == "Abomination" else ""
    lines.append(f"| {pack} | {rarity(pack)} | {stats['Price']} | {stats['Weight']} | {single} |")
section = None
for card in sorted(cards.values(), key=lambda item: (item["Sort"], item["Id"])):
    if section != card["Pack"]:
        section = card["Pack"]
        lines += ["", f"## {section}"]
    lines += ["", f"### {card['Title']}", "", "| Field | Value |", "| --- | --- |", f"| Id | {card['Id']} |", f"| Pack | {card['Pack']} |", f"| InPool | {'yes' if card['InPool'] else 'no'} |", f"| MaxLevel | {card['MaxLevel']} |"]
    if card["Price"]:
        lines.append(f"| Price | {card['Price']} |")
    if card["OwnedWeight"]:
        lines.append(f"| OwnedWeight | {card['OwnedWeight']} |")
    if card["UnlockLap"]:
        lines.append(f"| UnlockLap | {card['UnlockLap']} |")
    if card["Hook"] != "None":
        lines.append(f"| Hook | {card['Hook']} |")
    if card["Blurb"]:
        lines.append(f"| Blurb | {card['Blurb'].replace('|', '\\|')} |")
    if card["Requires"]:
        lines.append(f"| Requires | {', '.join(card['Requires'])} |")
    if card["Excludes"]:
        names = sorted(card["Excludes"], key=lambda ident: (cards[ident]["Sort"], ident))
        lines.append(f"| Excludes | {', '.join(names)} |")
    for flag in card["Flags"]:
        lines.append(f"| Flag | {flag} |")
    for mod in card["Mods"]:
        if mod.get("Growth") == "Tiers":
            body = " / ".join(fmt(mod["Tiers"][key]) for key in ("Level1", "Level2", "Level3", "Level4") if key in mod["Tiers"])
        elif mod.get("Growth") == "Linear":
            body = f"{fmt(mod['Value'])} x level"
        elif mod.get("Growth") == "TraitRatio":
            body = f"{fmt(mod['Value'])} x rank"
        elif mod.get("Growth") == "PerRemoved":
            body = f"{fmt(mod['Value'])} per removed"
        else:
            body = fmt(mod.get("Value", 0))
        if mod.get("Bias"):
            body += f" bias {fmt(mod['Bias'])}"
        if mod.get("ByHook"):
            body += " hook"
        if mod.get("Passive"):
            body += " passive"
        body += f" @{mod['Order']}"
        lines.append(f"| {mod['Stat']} {mod['Op']} | {body} |")
lines.append("")
(root / "TRINKETS.md").write_text("\n".join(lines), encoding="utf-8")

slim = {
    "MaxLevel": traits["MaxLevel"],
    "RefreshPrice": traits["RefreshPrice"],
    "RefreshStep": traits["RefreshStep"],
    "BaseDamage": traits["BaseDamage"],
    "MaxBouncesBase": traits["MaxBouncesBase"],
    "EnergyBase": traits["EnergyBase"],
    "ReloadBase": traits["ReloadBase"],
    "ReloadMin": traits["ReloadMin"],
    "ProjectileRadius": 13,
    "Rifle": traits["Rifle"],
    "Shotgun": traits["Shotgun"],
    "Nailgun": traits["Nailgun"],
    "Laser": traits["Laser"],
    "Rail": traits["Rail"],
    "Rocket": traits["Rocket"],
    "Entry": traits["Entry"],
    "Junior": traits["Junior"],
    "Warrior": traits["Warrior"],
    "Abomination": {**traits["Abomination"], "Single": True},
    "DrumCycle": traits["DrumCycle"],
    "LashPad": traits["LashPad"],
    "LashPerSecond": traits["LashPerSecond"],
    "LashMaxHold": traits["LashMaxHold"],
    "LashRange": traits["LashRange"],
    "LashWidth": traits["LashWidth"],
    "__references": [],
    "__version": 0,
}
(root / "Assets/settings/traits.omrtrait").write_text(json.dumps(slim, indent=2) + "\n", encoding="utf-8")

raw_text = (root / "Assets/settings/text.omrtext").read_text(encoding="utf-8")
key_at = raw_text.index('"Traits":')
brace = raw_text.index("{", key_at)
obj, end = json.JSONDecoder().raw_decode(raw_text, brace)
pretty = json.dumps({"Rarity": obj["Rarity"]}, indent=2)
pretty = pretty.replace("\n", "\n  ")
raw_text = raw_text[:key_at] + '"Traits": ' + pretty + raw_text[end:]
(root / "Assets/settings/text.omrtext").write_text(raw_text, encoding="utf-8")
print(f"parity ok, {written} trinkets")
