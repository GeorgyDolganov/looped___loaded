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
| DrumCycle | 0.16 |
| LashWidth | 8 |
| ProjectileRadius | 13 |

## Packs

| Pack | Rarity | Price | Weight | Single |
| --- | --- | --- | --- | --- |
| Rifle | COMMON | 3 | 5 |  |
| Shotgun | COMMON | 3 | 5 |  |
| Nailgun | UNCOMMON | 3 | 4 |  |
| Laser | RARE | 4 | 3 |  |
| Rail | RARE | 4 | 3 |  |
| Rocket | RARE | 4 | 3 |  |
| Entry | COMMON | 3 | 6 |  |
| Junior | UNCOMMON | 4 | 4 |  |
| Warrior | RARE | 6 | 2 |  |
| Abomination | EPIC | 9 | 1 | yes |

## Entry

### RICO

| Field | Value |
| --- | --- |
| Id | RICO |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 3 |
| Bounces Add | 1 / 3 / 5 @500 |
| Energy Mul | 1.25 / 1.6 / 2 @500 |

## Entry

### KICK

| Field | Value |
| --- | --- |
| Id | KICK |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 3 |
| KickForce Add | 480 / 760 / 1100 @900 |
| KickRange Set | 1600 passive @0 |

## Junior

### BULK

| Field | Value |
| --- | --- |
| Id | BULK |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Rounds get huge. |
| Radius Mul | 2 / 4 / 7 @1150 |

### STUN

| Field | Value |
| --- | --- |
| Id | STUN |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | *Doesn't affect boss. |
| StunTime Add | 0.45 @900 |

## Abomination

### SLUG

| Field | Value |
| --- | --- |
| Id | SLUG |
| Pack | Abomination |
| InPool | yes |
| MaxLevel | 1 |
| Hook | Slug |
| Blurb | Get +damage, +4 bounces, and +4 pierce for each removed projectile. |
| Requires | BORE, BUCK |
| Flag | NoNail |
| Damage Add | 2 per removed @1000 |
| Bounces Add | 4 per removed @505 |
| Pierce Add | 4 per removed @620 |
| Radius Max | 22 @1100 |
| RangePad Set | 120 @900 |

## Shotgun

### BUCK

| Field | Value |
| --- | --- |
| Id | BUCK |
| Pack | Shotgun |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Say hello to my little friend |
| Count Add | 2 / 3 / 5 bias -1 @100 |
| Cone Add | 10 / 16 / 24 @220 |
| RangeCut Add | 0.83 / 0.94 / 1 @0 |

## Rail

### BORE

| Field | Value |
| --- | --- |
| Id | BORE |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Fill them with holes. |
| Pierce Set | 1 / 2 / 3 @600 |

## Rifle

### DRUM

| Field | Value |
| --- | --- |
| Id | DRUM |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Hold to shoot multiple rounds, one after another. |
| Excludes | ELECTRIFY |
| Flag | Auto |
| Burst Set | 3 / 4 / 6 @1400 |
| Reload Add | 0.45 x rank @700 |

## Rocket

### WARHEAD

| Field | Value |
| --- | --- |
| Id | WARHEAD |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | SLOW BUT AREAL. Also damages you! PS No rocket jump! |
| Flag | FriendlySplash |
| Splash Set | 90 / 126 / 176 @1200 |
| Speed Mul | 0.78 / 0.68 / 0.58 @800 |

## Laser

### ELECTRIFY

| Field | Value |
| --- | --- |
| Id | ELECTRIFY |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 3 |
| OwnedWeight | 12 |
| Blurb | HOLD TO ZAP. Short reload. |
| Excludes | DRUM, FETCH, GHOST |
| Flag | Beam |
| Reload Set | 0.3 @710 |
| BeamTick Set | 0.6 / 0.4 / 0.2 @1500 |
| BeamTicks Set | 4 / 6 / 8 @1500 |
| BeamHit Set | 1 @1500 |
| BeamRank Set | 1 x level @705 |

## Nailgun

### PIN

| Field | Value |
| --- | --- |
| Id | PIN |
| Pack | Nailgun |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Any damage marks the body. It loses 1 health each second for 3s / 5s / 8s. |

## Rifle

### RUSH

| Field | Value |
| --- | --- |
| Id | RUSH |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 3 |
| Speed Mul | 1.2 / 1.4 / 1.65 @800 |

## Entry

### DODGE

| Field | Value |
| --- | --- |
| Id | DODGE |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Improve your chance of ignoring damage |
| Dodge Set | 0.1 / 0.2 / 0.32 @900 |

## Nailgun

### RELOADER

| Field | Value |
| --- | --- |
| Id | RELOADER |
| Pack | Nailgun |
| InPool | yes |
| MaxLevel | 3 |
| Price | 5 |
| UnlockLap | 3 |
| Reload Mul | 0.8 / 0.64 / 0.5 @740 |

## Rocket

### CASSETTE

| Field | Value |
| --- | --- |
| Id | CASSETTE |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Projectiles can explode +1/2/3 times after they already exploded. |
| Requires | WARHEAD |
| ExtraSplash Add | 1 / 2 / 3 @1210 |

## Junior

### BLOOM

| Field | Value |
| --- | --- |
| Id | BLOOM |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Splash Add | 80 @1202 |
| Reload Add | 0.2 @700 |

### IGNORANCE

| Field | Value |
| --- | --- |
| Id | IGNORANCE |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Your explosions do not hurt you. |
| Flag | NoFriendlySplash |

## Rail

### RAM

| Field | Value |
| --- | --- |
| Id | RAM |
| Pack | Rail |
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
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Flies straight back to you, through walls and bodies. |
| Excludes | ELECTRIFY |
| Flag | Fetch |
| Bounces Set | 0 @520 |
| Reload Add | 0.35 @700 |

## Rifle

### FRENZY

| Field | Value |
| --- | --- |
| Id | FRENZY |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | +damage on a body this burst already hit.  |
| Requires | DRUM |
| Flag | Bite |
| Speed Mul | 0.8 @800 |

## Junior

### FOCUS

| Field | Value |
| --- | --- |
| Id | FOCUS |
| Pack | Junior |
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
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Jumps once to a near neighbor. |
| Requires | ELECTRIFY |
| Excludes | FOCUS |
| BeamArc Set | 220 @1500 |
| BeamTick Mul | 1.15 @1510 |

### SHUNT

| Field | Value |
| --- | --- |
| Id | SHUNT |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Ignores shields and plates. |
| Requires | ELECTRIFY |
| Flag | BeamShunt |
| BeamTick Mul | 1.25 @1510 |
| BeamWidth Mul | 0.85 @1600 |

### LINGER

| Field | Value |
| --- | --- |
| Id | LINGER |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Remaining ticks finish where you let go. |
| Requires | ELECTRIFY |
| Flag | BeamLinger |
| Reload Add | 0.12 @720 |

## Junior

### PINBALL

| Field | Value |
| --- | --- |
| Id | PINBALL |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Each ricochet hits HARDER and flies FASTER. |
| BounceDamage Add | 1 / 1 / 2 @900 |
| BounceSpeed Add | 0.1 / 0.2 / 0.3 @900 |

## Entry

### RETURN

| Field | Value |
| --- | --- |
| Id | RETURN |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Spent rounds chase you faster. Faster each rank. |
| Pickup Mul | 1.35 / 1.7 / 2.15 @800 |

### GHOST

| Field | Value |
| --- | --- |
| Id | GHOST |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Rounds pass through panels, shards, and spinners. |
| Excludes | ELECTRIFY |
| Flag | Ghost |

## Warrior

### TURRET

| Field | Value |
| --- | --- |
| Id | TURRET |
| Pack | Warrior |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | A turret circles you and shoots the nearest body for 1 damage every 2s / 1.5s / 1s. |
