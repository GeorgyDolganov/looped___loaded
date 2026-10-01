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
    "RicoBounces": 1,
    "KickForce": 110, "KickRange": 180, "StunTime": 0.45, "StunRange": 160,
    "SlugRadius": 22, "SlugDamage": 2, "SlugFalloffPad": 120, "SlugBounce": 4, "SlugPierce": 4,
    "ElectrifyHit": 1, "ProjectileRadius": 13,
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
    "Rico": "ui/traits/RICO-extra-bounce-64x64.png",
    "Kick": "ui/traits/KICK-64x64.png", "Stun": "ui/traits/STUN-64x64.png",
    "Slug": "ui/traits/SLUG-64x64.png",
    "Buck": "ui/traits/BUCK-readable-64x64.png",
    "Bore": "ui/traits/PIERCE-impact-64x64.png",
    "Ram": "ui/traits/PIERCE-damage-up-64x64.png",
    "Drum": "ui/traits/DRUM-three-bullets-64x64.png", "Frenzy": "ui/traits/FRENZY-rage-64x64.png",
    "Warhead": "ui/traits/WARHEAD-64x64.png", "Cassette": "ui/traits/CASETTE-64x64.png", "Bloom": "ui/traits/BLOOM-64x64.png",
    "Ignorance": "ui/traits/IGNORANCE-64x64.png",
    "Electrify": "ui/traits/ELECTRIFY-64x64.png", "Focus": "ui/traits/FOCUS-64x64.png",
    "Arc": "ui/traits/ARC-64x64.png", "Shunt": "ui/traits/SHUNT-64x64.png",
    "Linger": "ui/traits/LINGER-64x64.png",
    "Reloader": "ui/traits/RELOADER-64x64.png",
    "Rush": "ui/traits/RUSH-64x64.png", "Dodge": "ui/traits/DOGE-64x64.png",
    "Pin": "ui/traits/BLEED-64x64.png",
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

add("Rico", "Junior", 4, 1, mods=[M("Bounces", "Add", 500, N("RicoBounces"))])
KICK_FORCE = {"Level1": 110, "Level2": 160, "Level3": 220}
add("Kick", "Warrior", 6, 3, mods=[
    M("KickForce", "Add", 900, growth="Tiers", tiers=KICK_FORCE),
    M("KickRange", "Set", 0, N("KickRange"), passive=True),
])
add("Stun", "Warrior", 7, 1, mods=[
    M("StunTime", "Add", 900, N("StunTime")),
    M("StunRange", "Set", 0, N("StunRange"), passive=True),
])
add("Slug", "Abomination", 8, 1, hook="Slug", flags=["NoNail"], req=["Bore", "Buck"], notes=[note("One shot")], mods=[
    M("Damage", "Add", 1000, N("SlugDamage"), growth="PerRemoved"),
    M("Bounces", "Add", 505, N("SlugBounce"), growth="PerRemoved"),
    M("Pierce", "Add", 620, N("SlugPierce"), growth="PerRemoved"),
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
    M("Speed", "Mul", 800, 0.95),
])
add("Drum", "Rifle", 11, 3, flags=["Auto"], mods=[
    M("Burst", "Set", 1400, growth="Tiers", tiers=tiers_of("DrumBurst")),
    M("Reload", "Add", 700, N("DrumReload"), growth="TraitRatio"),
])
add("Warhead", "Rocket", 12, 3, flags=["FriendlySplash"], mods=[
    M("Splash", "Set", 1200, growth="Tiers", tiers=tiers_of("WarheadRadius")),
    M("Speed", "Mul", 800, growth="Tiers", tiers=tiers_of("WarheadSpeed")),
])
add("Electrify", "Laser", 13, 3, flags=["Beam"], weight=N("ElectrifyRankWeight"), mods=[
    M("Reload", "Set", 710, N("ElectrifyReload")),
    M("BeamTick", "Set", 1500, growth="Tiers", tiers=tiers_of("ElectrifyTick")),
    M("BeamTicks", "Set", 1500, growth="Tiers", tiers=tiers_of("ElectrifyTicks")),
    M("BeamHit", "Set", 1500, max(1, N("ElectrifyHit"))),
    M("BeamRank", "Set", 705, 1, growth="Linear", hidden=True),
])
add("Pin", "Nailgun", 14, 3)
add("Rush", "Rifle", 15, 3, mods=[
    M("Speed", "Mul", 800, growth="Tiers", tiers=tiers_of("RushSpeed")),
])
add("Dodge", "Nailgun", 16, 3, mods=[M("Dodge", "Set", 900, growth="Tiers", tiers=tiers_of("DodgeChance"))])
add("Reloader", "Nailgun", 17, 3, price=N("ReloaderPrice"), lap=N("ReloaderUnlockLap"), mods=[
    M("Reload", "Mul", 740, growth="Tiers", tiers=tiers_of("ReloaderReload"), points=True),
])
add("Cassette", "Rocket", 18, 3, req=["Warhead"], mods=[
    M("ExtraSplash", "Add", 1210, growth="Tiers", tiers={"Level1": 1, "Level2": 2, "Level3": 3}),
])
add("Bloom", "Rocket", 19, 1, req=["Warhead"], mods=[
    M("Splash", "Add", 1202, N("BloomRadius")),
    M("Reload", "Add", 700, N("BloomReload")),
])
add("Ignorance", "Rocket", 21, 1, flags=["NoFriendlySplash"])
add("Ram", "Rail", 26, 1, flags=["RampPierce"], req=["Bore"], mods=[M("Speed", "Mul", 800, 0.9)])
add("Frenzy", "Rifle", 32, 1, flags=["Bite"], req=["Drum"], mods=[M("Speed", "Mul", 800, N("BiteSpeed"))])
add("Focus", "Laser", 34, 1, flags=["BeamSear"], req=["Electrify"], mods=[M("Reload", "Add", 720, N("SearReload"), when="BeamRank", wmin=1)])
add("Arc", "Laser", 36, 1, req=["Electrify"], mods=[
    M("BeamArc", "Set", 1500, N("ArcRange")),
    M("BeamTick", "Mul", 1510, N("ArcTick"), when="BeamTicks", wmin=1),
])
add("Shunt", "Laser", 38, 1, flags=["BeamShunt"], req=["Electrify"], mods=[
    M("BeamTick", "Mul", 1510, N("ShuntTick"), when="BeamTicks", wmin=1),
    M("BeamWidth", "Mul", 1600, N("ShuntWidth")),
])
add("Linger", "Laser", 39, 1, flags=["BeamLinger"], req=["Electrify"], mods=[M("Reload", "Add", 720, N("LingerReload"), when="BeamRank", wmin=1)])

cross(["FOCUS"], ["ARC"])
cross(["DRUM"], ["ELECTRIFY"])

if len(cards) != 22:
    raise SystemExit(f"expected 23 cards, got {len(cards)}")

INTS = {"Count", "Full", "Damage", "Pierce", "Bounces", "SplashDamage", "ExtraSplash", "Burst", "BeamHit", "BeamTicks", "BeamRank"}
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
G_HIT = int(N("ElectrifyHit"))

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
        self.extra_splash = 0
        self.range_cut = 0.0
        self.range_pad = 0.0
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
        self.beam_rank = 0
        self.stick = 0.0
        self.dodge = 0.0
        self.energy = G_ENERGY

    def get(self, stat):
        return {
            "Count": self.count, "Full": self.full, "Cone": self.cone, "Damage": self.damage,
            "Pierce": self.pierce, "Bounces": self.bounces, "Reload": self.reload, "BoreWait": self.bore_wait,
            "Speed": self.speed, "Radius": self.radius, "Splash": self.splash, "SplashDamage": self.splash_damage, "ExtraSplash": self.extra_splash,
            "RangeCut": self.range_cut, "RangePad": self.range_pad,
            "KickForce": self.kick_force, "KickRange": self.kick_range, "StunTime": self.stun_time, "StunRange": self.stun_range,
            "Cycle": self.cycle, "Burst": self.burst, "WalkStep": self.walk, "BeamHit": self.beam_hit,
            "BeamTick": self.beam_tick, "BeamTicks": self.beam_ticks, "BeamWidth": self.beam_width, "BeamArc": self.beam_arc,
            "BeamRank": self.beam_rank, "StickTime": self.stick, "Dodge": self.dodge,
            "Energy": self.energy,
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
        elif stat == "ExtraSplash": self.extra_splash = int(value)
        elif stat == "RangeCut": self.range_cut = value
        elif stat == "RangePad": self.range_pad = value
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
        elif stat == "BeamRank": self.beam_rank = int(value)
        elif stat == "StickTime": self.stick = value
        elif stat == "Dodge": self.dodge = value
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
            "count": self.count,
            "cone": self.cone,
            "damage": max(1, self.damage),
            "pierce": self.pierce,
            "bounces": self.bounces,
            "energy": self.energy,
            "speed": self.speed,
            "radius": self.radius,
            "splash": self.splash,
            "splash_damage": self.splash_damage,
            "extra_splash": self.extra_splash,
            "friendly": "FriendlySplash" in self.mask and "NoFriendlySplash" not in self.mask,
            "per_pellet": "PerPelletSplash" in self.mask,
            "ramp": "RampPierce" in self.mask,
            "nail": nail,
            "stick": self.stick if nail else 0,
            "range_cut": max(0, self.range_cut),
            "range_pad": self.range_pad,
            "falloff": 0,
            "kick_force": self.kick_force,
            "kick_range": self.kick_range,
            "stun_time": self.stun_time,
            "stun_range": self.stun_range,
            "cycle": self.cycle,
            "burst": self.burst,
            "walk": self.walk,
            "bite": "Bite" in self.mask,
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
            "beam_arc": self.beam_arc,
            "beam_shunt": "BeamShunt" in self.mask,
            "beam_linger": "BeamLinger" in self.mask,
            "dodge": self.dodge,
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

POOL = [
    "RICO", "KICK", "STUN", "SLUG",
    "BUCK", "BORE", "DRUM", "WARHEAD", "ELECTRIFY", "PIN", "RUSH", "DODGE", "RELOADER",
    "CASSETTE", "BLOOM", "IGNORANCE",
    "RAM",
    "FRENZY",
    "FOCUS", "ARC", "SHUNT", "LINGER",
]

def lv(levels, ident):
    return levels.get(ident, 0)

def has(levels, ident):
    return lv(levels, ident) > 0

def legacy(levels, bonus=0):
    buck = lv(levels, "BUCK")
    bore = lv(levels, "BORE")
    drum = lv(levels, "DRUM")
    warhead = lv(levels, "WARHEAD")
    electrify = lv(levels, "ELECTRIFY")
    rush = lv(levels, "RUSH")
    slug = has(levels, "SLUG")
    cassette = lv(levels, "CASSETTE")
    count = 1
    if buck > 0:
        count += max(0, int(at(tiers_of("BuckPellets"), buck)) - 1)
    full = count
    cone = 0.0
    if buck > 0:
        cone += at(tiers_of("BuckCone"), buck)
    if slug:
        count = 1
        cone = 0
    bounces = G_BOUNCE
    if has(levels, "RICO"):
        bounces += N("RicoBounces")
    if slug:
        bounces += N("SlugBounce") * max(0, full - count)
    pierce = 0 if bore <= 0 else int(at(tiers_of("BorePierce"), bore))
    if slug:
        pierce += N("SlugPierce") * max(0, full - count)
    reload = G_RELOAD
    bore_wait = 0.0
    if bore > 0:
        bore_wait = N("BoreReload") * trait_mul(bore)
        reload += bore_wait
    if drum > 0:
        reload += N("DrumReload") * trait_mul(drum)
    if has(levels, "BLOOM"):
        reload += N("BloomReload")
    if electrify > 0:
        reload = N("ElectrifyReload")
        if has(levels, "FOCUS"):
            reload += N("SearReload")
        if has(levels, "LINGER"):
            reload += N("LingerReload")
    reloader = lv(levels, "RELOADER")
    if reloader > 0:
        reload *= at(tiers_of("ReloaderReload"), reloader)
    speed = 1.0
    if bore > 0:
        speed *= 0.95
    if warhead > 0:
        speed *= at(tiers_of("WarheadSpeed"), warhead)
    for ident, key in (
        ("RAM", "RamSpeed"), ("FRENZY", "BiteSpeed"),
    ):
        if has(levels, ident):
            speed *= N(key)
    if rush > 0:
        speed *= at(tiers_of("RushSpeed"), rush)
    range_cut = 0.0
    if buck > 0:
        range_cut += at(tiers_of("BuckRangeCut"), buck)
    damage = max(1, G_DAMAGE + bonus)
    if slug:
        damage += N("SlugDamage") * max(0, full - count)
    radius = G_RADIUS
    if slug:
        radius = max(radius, N("SlugRadius"))
    splash = at(tiers_of("WarheadRadius"), warhead)
    if has(levels, "BLOOM"):
        splash += N("BloomRadius")
    splash_damage = 1 if splash > 1 else 0
    cycle = G_CYCLE
    burst = 1 if drum <= 0 else max(1, int(at(tiers_of("DrumBurst"), drum)))
    beam_ticks = 0
    beam_tick = 1.0
    if electrify > 0:
        beam_ticks = max(1, int(at(tiers_of("ElectrifyTicks"), electrify)))
        beam_tick = at(tiers_of("ElectrifyTick"), electrify)
        if has(levels, "ARC"):
            beam_tick *= N("ArcTick")
        if has(levels, "SHUNT"):
            beam_tick *= N("ShuntTick")
    width = G_WIDTH
    if has(levels, "SHUNT"):
        width *= N("ShuntWidth")
    return {
        "beam": electrify > 0,
        "auto": drum > 0 and electrify <= 0,
        "count": count,
        "cone": cone,
        "damage": damage,
        "pierce": pierce,
        "bounces": bounces,
        "energy": G_ENERGY,
        "speed": speed,
        "radius": radius,
        "splash": splash,
        "splash_damage": splash_damage,
        "extra_splash": (1, 2, 3)[min(cassette, 3) - 1] if cassette > 0 else 0,
        "friendly": warhead > 0 and not has(levels, "IGNORANCE"),
        "per_pellet": False,
        "ramp": has(levels, "RAM"),
        "nail": False,
        "stick": 0,
        "range_cut": max(0, range_cut),
        "range_pad": N("SlugFalloffPad") if slug else 0,
        "falloff": 0,
        "kick_force": at(KICK_FORCE, lv(levels, "KICK")),
        "kick_range": N("KickRange"),
        "stun_time": N("StunTime") if has(levels, "STUN") else 0,
        "stun_range": N("StunRange"),
        "cycle": cycle,
        "burst": burst,
        "walk": 0,
        "bite": has(levels, "FRENZY"),
        "reload": max(G_MIN, reload),
        "bore_wait": bore_wait,
        "beam_pad": G_PAD,
        "beam_per": G_PER,
        "beam_hold": G_HOLD,
        "beam_hit": max(1, G_HIT) if electrify > 0 else 0,
        "beam_tick": beam_tick,
        "beam_ticks": beam_ticks,
        "beam_range": G_RANGE,
        "beam_width": width,
        "beam_rank": electrify,
        "beam_sear": has(levels, "FOCUS"),
        "beam_arc": N("ArcRange") if has(levels, "ARC") else 0,
        "beam_shunt": has(levels, "SHUNT"),
        "beam_linger": has(levels, "LINGER"),
        "dodge": at(tiers_of("DodgeChance"), lv(levels, "DODGE")) if has(levels, "DODGE") else 0,
    }

CLUSTER = {"CASSETTE", "BLOOM"}
BRAND = {"FOCUS"}
ARC = {"ARC"}

def old_blocked(ident, owned):
    def owns(group):
        return any(item in owned for item in group)
    if ident == "RAM" and "BORE" not in owned:
        return True
    if ident in CLUSTER and "WARHEAD" not in owned:
        return True
    if ident == "ELECTRIFY" and "DRUM" in owned:
        return True
    if ident == "DRUM" and "ELECTRIFY" in owned:
        return True
    if ident == "FRENZY" and "DRUM" not in owned:
        return True
    if ident == "SLUG" and ("BORE" not in owned or "BUCK" not in owned):
        return True
    if ident in BRAND | ARC | {"SHUNT", "LINGER"} and "ELECTRIFY" not in owned:
        return True
    if ident in BRAND and owns(ARC):
        return True
    if ident in ARC and owns(BRAND):
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
check({}, kick_range=180, stun_range=160, kick_force=0, count=1, reload=0.7, radius=13, burst=1, beam_tick=1, beam_width=8)
check({"BUCK": 3, "SLUG": 1}, count=1, damage=9, radius=22, cone=0, bounces=17, pierce=16)
check({"PIN": 1}, count=1, cone=0, bounces=1, radius=13, stick=0, nail=False)
check({"PIN": 1, "SLUG": 1}, count=1, damage=1, radius=22, nail=False, stick=0, cone=0)
check({"KICK": 1}, kick_force=110, kick_range=180)
check({"KICK": 3}, kick_force=220, kick_range=180)
check({"ELECTRIFY": 1, "BORE": 1}, reload=0.3, bore_wait=0.35, pierce=1, beam=True, bounces=1)
check({"ELECTRIFY": 1, "FOCUS": 1, "LINGER": 1}, reload=0.3 + 0.1 + 0.12)
check({"BORE": 1}, pierce=1, bore_wait=0.35)
check({"ELECTRIFY": 1, "RELOADER": 3}, reload=0.2)
check({"WARHEAD": 1, "CASSETTE": 1, "BLOOM": 1}, splash=90 + 80, extra_splash=1, per_pellet=False)
check({"WARHEAD": 1, "CASSETTE": 3, "BLOOM": 1, "IGNORANCE": 1}, splash=90 + 80, friendly=False, extra_splash=3)
check({"WARHEAD": 1}, splash=90, splash_damage=1, friendly=True, speed=0.78)
check({"DRUM": 1}, auto=True, burst=3)
check({"DODGE": 3}, dodge=0.32)
check({"ELECTRIFY": 1}, beam_ticks=4)
check({"ELECTRIFY": 1, "ARC": 1}, beam_tick=0.6 * 1.15, beam_arc=220)
check({"RUSH": 2}, speed=1.4)
check({"BORE": 2}, pierce=1)
check({}, bonus=4, damage=5)
check({"SLUG": 1}, damage=1, radius=22, range_pad=120, pierce=0, bounces=1)
check({"ELECTRIFY": 3, "FOCUS": 1})
check({"BORE": 3})
check({"WARHEAD": 3, "CASSETTE": 2, "BLOOM": 1}, extra_splash=2)
check({"DRUM": 3}, burst=6)
check({"PIN": 3, "SLUG": 1})

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
check_blocked(["BORE"])
check_blocked(["BUCK", "BORE"])
check_blocked(["CASSETTE", "WARHEAD"])
check_blocked(["IGNORANCE"])
check_blocked(["ELECTRIFY", "FOCUS"])
check_blocked(["ELECTRIFY", "ARC"])
check_blocked(["BORE", "ELECTRIFY"])
check_blocked(["DRUM", "ELECTRIFY"])
check_blocked(["DRUM", "FRENZY"])
for _ in range(200):
    check_blocked(rng.sample(POOL, rng.randint(0, 10)))

if len(POOL) != 22:
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
