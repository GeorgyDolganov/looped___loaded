# Trinkets

## Globals

| Field | Value |
| --- | --- |
| MaxLevel | 3 |
| BaseDamage | 1 |
| MaxBouncesBase | 1 |
| EnergyBase | 5500 |
| ReloadBase | 0.7 |
| ReloadMin | 0.2 |
| ProjectileRadius | 13 |
| DrumCycle | 0.16 |
| LashWidth | 8 |

## Rarities

| Rarity | Label | Price | Weight |
| --- | --- | --- | --- |
| Common | COMMON | 6 | 6 |
| Uncommon | UNCOMMON | 8 | 4 |
| Rare | RARE | 10 | 2 |
| Epic | EPIC | 14 | 1 |

## Cards

### BACKDASH

| Field | Value |
| --- | --- |
| Id | BACKDASH |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Activate to dash backward. 3s cooldown. |

### REPULSE

| Field | Value |
| --- | --- |
| Id | REPULSE |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Activate to blast nearby enemies away. |

### BARRIER

| Field | Value |
| --- | --- |
| Id | BARRIER |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Activate to raise a shield that absorbs the next hit. |

### RICOCHET

| Field | Value |
| --- | --- |
| Id | RICO |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| Bounces Add | 1 / 3 / 5 @500 |
| Energy Mul | 1.25 / 1.6 / 2 @500 |

### KICK

| Field | Value |
| --- | --- |
| Id | KICK |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| KickForce Add | 480 / 760 / 1100 @900 |
| KickRange Set | 1600 passive @0 |

### BULK

| Field | Value |
| --- | --- |
| Id | BULK |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Rounds get huge. |
| Radius Mul | 2 / 4 / 7 @1150 |

### STUN

| Field | Value |
| --- | --- |
| Id | STUN |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | *Doesn't affect boss. |
| StunTime Add | 0.45 @900 |

### SLUG

| Field | Value |
| --- | --- |
| Id | SLUG |
| Rarity | EPIC |
| InPool | yes |
| MaxLevel | 1 |
| Hook | Slug |
| Blurb | Set projectiles to 1 and gives bonuses for each removed projectile. Increases projectile size if it was smaller. |
| Requires | BUCK |
| Flag | NoNail |
| Damage Add | 2 per removed @1000 |
| Bounces Add | 4 per removed @505 |
| Pierce Add | 4 per removed @620 |
| Radius Max | 22 @1100 |
| RangePad Set | 120 @900 |
| RangeMul Mul | 0.4 per removed bias 1 @910 |

### BUCK

| Field | Value |
| --- | --- |
| Id | BUCK |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Say hello to my little friend |
| Count Add | 2 / 3 / 5 bias -1 @100 |
| Cone Add | 10 / 16 / 24 @220 |
| RangeCut Add | 0.83 / 0.94 / 1 @0 |

### PIERCE

| Field | Value |
| --- | --- |
| Id | BORE |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Fill them with holes. |
| Pierce Set | 1 / 2 / 3 @600 |

### DRUM

| Field | Value |
| --- | --- |
| Id | DRUM |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Hold to shoot multiple rounds, one after another. |
| Excludes | ELECTRIFY |
| Flag | Auto |
| Burst Set | 3 / 4 / 6 @1400 |
| Reload Add | 0.45 x rank @700 |

### WARHEAD

| Field | Value |
| --- | --- |
| Id | WARHEAD |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Area damage. Also damages you. No rocket jumping! |
| Flag | FriendlySplash |
| Splash Set | 90 / 126 / 176 @1200 |
| Speed Mul | 0.78 / 0.68 / 0.58 @800 |

### ELECTRIFY

| Field | Value |
| --- | --- |
| Id | ELECTRIFY |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| OwnedWeight | 12 |
| Blurb | HOLD TO ZAP. |
| Excludes | DRUM, FETCH, GHOST |
| Flag | Beam |
| Reload Set | 0.3 @710 |
| BeamTick Set | 0.6 / 0.4 / 0.2 @1500 |
| BeamTicks Set | 4 / 6 / 8 @1500 |
| BeamHit Set | 1 @1500 |
| BeamRank Set | 1 x level @705 |
| Bounces Add | -1 x level @510 |

### BLEED

| Field | Value |
| --- | --- |
| Id | PIN |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Any damage marks the body. It loses 1 health each second. |

### RUSH

| Field | Value |
| --- | --- |
| Id | RUSH |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| Speed Mul | 1.2 / 1.4 / 1.65 @800 |

### DODGE

| Field | Value |
| --- | --- |
| Id | DODGE |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Improve your chance of ignoring damage |
| Dodge Set | 0.1 / 0.2 / 0.32 @900 |

### RELOADER

| Field | Value |
| --- | --- |
| Id | RELOADER |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Price | 10 |
| UnlockLap | 3 |
| Reload Mul | 0.8 / 0.64 / 0.5 @740 |

### CASSETTE

| Field | Value |
| --- | --- |
| Id | CASSETTE |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Projectiles can explode again after they already exploded. |
| Requires | WARHEAD |
| ExtraSplash Add | 1 / 2 / 3 @1210 |

### BLOOM

| Field | Value |
| --- | --- |
| Id | BLOOM |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Splash Add | 80 @1202 |
| Reload Add | 0.2 @700 |

### IGNORANCE

| Field | Value |
| --- | --- |
| Id | IGNORANCE |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Your explosions do not hurt you. |
| Requires | WARHEAD |
| Flag | NoFriendlySplash |

### RAM

| Field | Value |
| --- | --- |
| Id | RAM |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Each body you punch through hits the next HARDER.  |
| Requires | BORE |
| Flag | RampPierce |
| Speed Mul | 0.9 @800 |

### FETCH

| Field | Value |
| --- | --- |
| Id | FETCH |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 2 |
| Blurb | Bullet flies straight back to you, through walls and bodies. |
| Excludes | ELECTRIFY, RETURN |
| Flag | Fetch |
| Reload Add | 0.35 @700 |

### FRENZY

| Field | Value |
| --- | --- |
| Id | FRENZY |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | +damage on a body this burst already hit.  |
| Requires | DRUM |
| Flag | Bite |
| Speed Mul | 0.8 @800 |

### FOCUS

| Field | Value |
| --- | --- |
| Id | FOCUS |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | +1 damage while the beam stays on a body. |
| Requires | ELECTRIFY |
| Excludes | ARC |
| Flag | BeamSear |
| Reload Add | 0.1 @720 |

### ARC

| Field | Value |
| --- | --- |
| Id | ARC |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 5 |
| Blurb | Chains the bolt to a nearby enemy. Each rank adds another jump. |
| Requires | ELECTRIFY |
| Excludes | FOCUS |
| BeamArc Add | 1 x level @1500 |
| BeamTick Mul | 1.15 @1510 |

### SHUNT

| Field | Value |
| --- | --- |
| Id | SHUNT |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Ignores shields and plates. |
| Requires | ELECTRIFY |
| Flag | BeamShunt |
| BeamTick Mul | 1.25 @1510 |

### LINGER

| Field | Value |
| --- | --- |
| Id | LINGER |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Remaining ticks finish where you let go. |
| Requires | ELECTRIFY |
| Flag | BeamLinger |
| Reload Add | 0.12 @720 |

### PINBALL

| Field | Value |
| --- | --- |
| Id | PINBALL |
| Rarity | UNCOMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Each ricochet hits HARDER and flies FASTER. |
| BounceDamage Add | 1 / 1 / 2 @900 |
| BounceSpeed Add | 0.1 / 0.2 / 0.3 @900 |

### RETURN

| Field | Value |
| --- | --- |
| Id | RETURN |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Spent rounds chase you faster. Faster each rank. |
| Excludes | FETCH |
| Pickup Mul | 1.35 / 1.7 / 2.15 @800 |

### GHOST

| Field | Value |
| --- | --- |
| Id | GHOST |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Rounds pass through panels, shards, and spinners. |
| Excludes | ELECTRIFY |
| Flag | Ghost |

### TURRET

| Field | Value |
| --- | --- |
| Id | TURRET |
| Rarity | RARE |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | A turret circles you and shoots the nearest body. |

### REROLL

| Field | Value |
| --- | --- |
| Id | REROLL |
| Rarity | COMMON |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | One free chapel refresh each round. |
