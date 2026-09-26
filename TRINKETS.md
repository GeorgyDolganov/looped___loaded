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
| Rifle | COMMON | 2 | 5 |  |
| Shotgun | COMMON | 2 | 5 |  |
| Nailgun | UNCOMMON | 2 | 4 |  |
| Laser | RARE | 3 | 3 |  |
| Rail | RARE | 3 | 3 |  |
| Rocket | RARE | 3 | 3 |  |
| Entry | COMMON | 2 | 6 |  |
| Junior | UNCOMMON | 3 | 4 |  |
| Warrior | RARE | 5 | 2 |  |
| Abomination | EPIC | 8 | 1 | yes |

## Entry

### SPLIT

| Field | Value |
| --- | --- |
| Id | SPLIT |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 4 |
| Count Add | 1 / 2 / 4 / 7 @100 |
| Reload Add | 0.15 / 0.25 / 0.5 @700 |

### FAN

| Field | Value |
| --- | --- |
| Id | FAN |
| Pack | Entry |
| InPool | yes |
| MaxLevel | 1 |
| Cone Add | 14 @200 |

## Junior

### CHOKE

| Field | Value |
| --- | --- |
| Id | CHOKE |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Cone Add | -10 @210 |
| Cone Max | 6 @211 |

### MEAT

| Field | Value |
| --- | --- |
| Id | MEAT |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 2 |
| MeatRange Max | 140 @900 |
| MeatBonus Add | 1 x level @900 |
| RangeCut Add | 1 x level @0 |

### RICO

| Field | Value |
| --- | --- |
| Id | RICO |
| Pack | Junior |
| InPool | yes |
| MaxLevel | 1 |
| Bounces Add | 1 @500 |

## Warrior

### DOUBLE

| Field | Value |
| --- | --- |
| Id | DOUBLE |
| Pack | Warrior |
| InPool | yes |
| MaxLevel | 1 |
| Flag | DoublePump |
| Reload Add | 0.35 @700 |
| Gap Set | 0.12 @700 |

### KICK

| Field | Value |
| --- | --- |
| Id | KICK |
| Pack | Warrior |
| InPool | yes |
| MaxLevel | 1 |
| KickForce Add | 110 @900 |
| KickRange Set | 180 passive @0 |

### STUN

| Field | Value |
| --- | --- |
| Id | STUN |
| Pack | Warrior |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | *Doesn't affect boss. |
| StunTime Add | 0.45 @900 |
| StunRange Set | 160 passive @0 |

## Abomination

### SLUG

| Field | Value |
| --- | --- |
| Id | SLUG |
| Pack | Abomination |
| InPool | yes |
| MaxLevel | 1 |
| Hook | Slug |
| Blurb | Get +damage for each removed projectile |
| Flag | NoNail |
| Damage Add | 2 per removed @1000 |
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
| Blurb | Did somebody say "RAILGUN"? |
| Excludes | LASH |
| Pierce Set | 1 / 1 / 2 @600 |
| Reload Add | 0.35 x rank @700 |
| BoreWait Set | 0.35 x rank @700 |

## Rifle

### DRUM

| Field | Value |
| --- | --- |
| Id | DRUM |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | HOLD TO SHOOT MULTIPLE ROUNDS ONE AFTER ANOTHER |
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

### LASH

| Field | Value |
| --- | --- |
| Id | LASH |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 3 |
| OwnedWeight | 12 |
| Blurb | HOLD TO ZAP. Short reload. |
| Excludes | BORE |
| Flag | Beam |
| Reload Set | 0.3 @710 |
| Bounces Set | 0 @510 |
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
| Hook | Pin |
| Blurb | Nails that damage after time. Why they are exactly nine inch? |
| Flag | Nail |
| Count Set | 2 / 3 / 5 hook @110 |
| Cone Set | 8 / 10 / 12 hook @310 |
| Bounces Add | 1 / 2 / 3 @500 |
| Radius Set | 6 @1050 |
| StickTime Set | 0.6 @1050 |

## Rifle

### RUSH

| Field | Value |
| --- | --- |
| Id | RUSH |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 3 |
| Speed Mul | 1.2 / 1.4 / 1.65 @800 |
| Reload Add | 0.12 x rank @700 |

## Nailgun

### DODGE

| Field | Value |
| --- | --- |
| Id | DODGE |
| Pack | Nailgun |
| InPool | yes |
| MaxLevel | 3 |
| Blurb | Improve your chance of ignoring damage |
| Dodge Set | 0.1 / 0.2 / 0.32 @900 |

### SNAP

| Field | Value |
| --- | --- |
| Id | SNAP |
| Pack | Nailgun |
| InPool | yes |
| MaxLevel | 3 |
| Price | 4 |
| UnlockLap | 3 |
| Reload Mul | 0.8 / 0.64 / 0.5 @740 |

## Rocket

### MIRV

| Field | Value |
| --- | --- |
| Id | MIRV |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Explosions not only on first contact! |
| Requires | WARHEAD |
| Excludes | LANCE, CRATER |
| Flag | PerPelletSplash |
| Splash Mul | 0.55 @1201 |
| Speed Mul | 0.8 @800 |

### BLOOM

| Field | Value |
| --- | --- |
| Id | BLOOM |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Excludes | LANCE, CRATER |
| Splash Add | 80 @1202 |
| Reload Add | 0.2 @700 |

### SCORCH

| Field | Value |
| --- | --- |
| Id | SCORCH |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Excludes | LANCE, CRATER |
| Speed Mul | 0.75 @800 |
| Reload Add | 0.12 @700 |
| SplashDamage Max | 2 @1260 |

### LANCE

| Field | Value |
| --- | --- |
| Id | LANCE |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Are you tiered from hitting yourself? Try this! Disables Ricochet. |
| Requires | WARHEAD |
| Excludes | MIRV, BLOOM, SCORCH |
| Flag | NoFriendlySplash |
| Damage Add | 2 @900 |
| Splash Mul | 0.7 @1203 |
| Speed Mul | 0.7 @800 |
| Reload Add | 0.3 @700 |
| Bounces Set | 0 @510 |

### CRATER

| Field | Value |
| --- | --- |
| Id | CRATER |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Excludes | MIRV, BLOOM, SCORCH |
| Radius Max | 22 @1110 |
| Splash Add | 56 @1204 |
| Speed Mul | 0.65 @800 |
| Bounces Set | 0 @510 |

### SPOT

| Field | Value |
| --- | --- |
| Id | SPOT |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | AIM TO BOOOOM |
| Requires | WARHEAD |
| Flag | PointAim |
| Speed Mul | 0.85 @800 |
| Splash Add | 40 @1205 |
| SplashDamage Add | 1 @1261 |
| Bounces Set | 0 @510 |

## Rail

### DEEP

| Field | Value |
| --- | --- |
| Id | DEEP |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Requires | BORE |
| Excludes | KEEL, MASS |
| Pierce Add | 2 @601 |
| Reload Add | 0.25 @700 |

### AWL

| Field | Value |
| --- | --- |
| Id | AWL |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Ignores boss armor. |
| Requires | BORE |
| Excludes | KEEL, MASS |
| Flag | IgnoreArmor |
| Speed Mul | 0.85 @800 |
| Reload Add | 0.12 @700 |

### RAM

| Field | Value |
| --- | --- |
| Id | RAM |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Each body you punch through hits the next HARDER.  |
| Requires | BORE |
| Excludes | KEEL, MASS |
| Flag | RampPierce |
| Speed Mul | 0.75 @800 |

### KEEL

| Field | Value |
| --- | --- |
| Id | KEEL |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Removes richochet. |
| Requires | BORE |
| Excludes | DEEP, AWL, RAM |
| Damage Add | 5 @900 |
| Speed Mul | 0.6 @800 |
| Bounces Set | 0 @510 |

## Rifle

### BELT

| Field | Value |
| --- | --- |
| Id | BELT |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Requires | DRUM |
| Excludes | SPOOL, SIGHT, BITE |
| Burst Add | 3 @1410 |
| Reload Add | 0.3 @700 |

### WALK

| Field | Value |
| --- | --- |
| Id | WALK |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Requires | DRUM |
| Excludes | SPOOL, SIGHT, BITE |
| WalkStep Add | 3 @900 |
| Reload Add | 0.12 @700 |

### SPOOL

| Field | Value |
| --- | --- |
| Id | SPOOL |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Requires | DRUM |
| Excludes | BELT, WALK |
| Cycle Mul | 0.65 @1400 |
| Reload Add | 0.15 @700 |

### SIGHT

| Field | Value |
| --- | --- |
| Id | SIGHT |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Later shots in a burst use half spread. |
| Requires | DRUM |
| Excludes | BELT, WALK |
| Flag | Sight |
| Speed Mul | 0.9 @800 |

### BITE

| Field | Value |
| --- | --- |
| Id | BITE |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | +damage on a body this burst already hit.  |
| Requires | DRUM |
| Excludes | BELT, WALK |
| Flag | Bite |
| Speed Mul | 0.8 @800 |

### LINK

| Field | Value |
| --- | --- |
| Id | LINK |
| Pack | Rifle |
| InPool | no |
| MaxLevel | 1 |
| Blurb | The burst finishes if you release. |
| Requires | DRUM |
| Flag | CommitBurst |
| Reload Add | 0.2 @700 |
| Cycle Mul | 1.1 @1400 |

## Laser

### SEAR

| Field | Value |
| --- | --- |
| Id | SEAR |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | +1 damage while the beam stays on a body. |
| Requires | LASH |
| Excludes | ARC, FORK |
| Flag | BeamSear |
| Reload Add | 0.1 @720 |

### KILN

| Field | Value |
| --- | --- |
| Id | KILN |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Tick ×0.75 while latched.  |
| Requires | LASH |
| Excludes | ARC, FORK |
| BeamKiln Set | 0.75 @1500 |
| BeamWidth Mul | 0.75 @1600 |
| Reload Add | 0.06 @720 |

### ARC

| Field | Value |
| --- | --- |
| Id | ARC |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Jumps once to a near neighbor. |
| Requires | LASH |
| Excludes | SEAR, KILN |
| BeamArc Set | 220 @1500 |
| BeamTick Mul | 1.15 @1510 |

### FORK

| Field | Value |
| --- | --- |
| Id | FORK |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Side bolts hit on their own. |
| Requires | LASH |
| Excludes | SEAR, KILN |
| Flag | BeamFork |
| Reload Add | 0.08 @720 |

### SHUNT

| Field | Value |
| --- | --- |
| Id | SHUNT |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Ignores shields and plates. |
| Requires | LASH |
| Flag | BeamShunt |
| BeamTick Mul | 1.25 @1510 |
| BeamWidth Mul | 0.85 @1600 |

### LINGER

| Field | Value |
| --- | --- |
| Id | LINGER |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Blurb | Remaining ticks finish where you let go. |
| Requires | LASH |
| Flag | BeamLinger |
| Reload Add | 0.12 @720 |

### CELL

| Field | Value |
| --- | --- |
| Id | CELL |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Requires | LASH |
| BeamTicks Add | 2 @1510 |
| Reload Add | 0.1 @720 |

## Rocket

### JACK

| Field | Value |
| --- | --- |
| Id | JACK |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD |
| Reload Mul | 0.8 @730 |
| Splash Mul | 0.8 @1206 |

### SLAP

| Field | Value |
| --- | --- |
| Id | SLAP |
| Pack | Rocket |
| InPool | yes |
| MaxLevel | 1 |
| Requires | WARHEAD, JACK |
| Reload Mul | 0.85 @730 |
| Speed Mul | 0.85 @800 |

## Rail

### RACK

| Field | Value |
| --- | --- |
| Id | RACK |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Requires | BORE |
| Reload Mul | 0.75 @730 |
| Speed Mul | 0.85 @800 |

### DRAW

| Field | Value |
| --- | --- |
| Id | DRAW |
| Pack | Rail |
| InPool | yes |
| MaxLevel | 1 |
| Requires | BORE, RACK |
| Reload Mul | 0.85 @730 |
| Pierce Add | -1 @610 |
| Pierce Max | 0 @611 |

## Rifle

### FEED

| Field | Value |
| --- | --- |
| Id | FEED |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Requires | DRUM |
| Reload Mul | 0.75 @730 |
| Cycle Mul | 1.2 @1400 |

### EJECT

| Field | Value |
| --- | --- |
| Id | EJECT |
| Pack | Rifle |
| InPool | yes |
| MaxLevel | 1 |
| Requires | DRUM, FEED |
| Reload Mul | 0.85 @730 |
| Burst Add | -1 @1420 |
| Burst Max | 1 @1421 |

## Laser

### VENT

| Field | Value |
| --- | --- |
| Id | VENT |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Requires | LASH |
| Reload Mul | 0.8 @730 |
| BeamTick Mul | 1.2 @1510 |

### COOL

| Field | Value |
| --- | --- |
| Id | COOL |
| Pack | Laser |
| InPool | yes |
| MaxLevel | 1 |
| Requires | LASH, VENT |
| Reload Mul | 0.85 @730 |
| BeamWidth Mul | 0.8 @1600 |

## Shotgun

### SHUCK

| Field | Value |
| --- | --- |
| Id | SHUCK |
| Pack | Shotgun |
| InPool | yes |
| MaxLevel | 1 |
| Requires | BUCK |
| Cone Add | 8 @400 |
| Reload Mul | 0.8 @730 |

### SLAM

| Field | Value |
| --- | --- |
| Id | SLAM |
| Pack | Shotgun |
| InPool | yes |
| MaxLevel | 1 |
| Requires | BUCK, SHUCK |
| Count Add | -1 @410 |
| Count Max | 1 @411 |
| Reload Mul | 0.85 @730 |

## Entry

### PUMP

| Field | Value |
| --- | --- |
| Id | PUMP |
| Pack | Entry |
| InPool | no |
| MaxLevel | 1 |
| Count Add | 1 @100 |
| Reload Add | 0.25 @700 |

## Junior

### LOAD

| Field | Value |
| --- | --- |
| Id | LOAD |
| Pack | Junior |
| InPool | no |
| MaxLevel | 1 |
| Count Add | 2 @100 |
| Reload Add | 0.15 @700 |

## Warrior

### GAPE

| Field | Value |
| --- | --- |
| Id | GAPE |
| Pack | Warrior |
| InPool | no |
| MaxLevel | 1 |
| Cone Add | 14 @220 |

## Abomination

### HEAP

| Field | Value |
| --- | --- |
| Id | HEAP |
| Pack | Abomination |
| InPool | no |
| MaxLevel | 1 |
| Count Add | 3 @100 |
| Reload Add | 0.4 @700 |

### WASTE

| Field | Value |
| --- | --- |
| Id | WASTE |
| Pack | Abomination |
| InPool | no |
| MaxLevel | 1 |
| MeatRange Max | 80 @900 |
| MeatBonus Add | 1 x level @900 |
| RangeCut Add | 1 x level @0 |

### BREACH

| Field | Value |
| --- | --- |
| Id | BREACH |
| Pack | Abomination |
| InPool | no |
| MaxLevel | 1 |
| Pierce Add | 1 @600 |

## Rail

### MASS

| Field | Value |
| --- | --- |
| Id | MASS |
| Pack | Rail |
| InPool | no |
| MaxLevel | 3 |
| Hook | Slug |
| Blurb | One shot. For each removed projectile +damage.  |
| Excludes | DEEP, AWL, RAM |
| Flag | NoNail |
| Damage Add | 3 per removed @1000 |
| Speed Mul | 0.7 @800 |
| Reload Add | 0.45 @700 |

### TRACE

| Field | Value |
| --- | --- |
| Id | TRACE |
| Pack | Rail |
| InPool | no |
| MaxLevel | 3 |
| Blurb | = |
| Speed Mul | 1.6 @800 |
| Reload Add | 0.25 @700 |

## Nailgun

### SPIN

| Field | Value |
| --- | --- |
| Id | SPIN |
| Pack | Nailgun |
| InPool | no |
| MaxLevel | 3 |
| Blurb | Shots sweep harder against the clock. Longer reload. |
| SpinSpeed Add | 220 @800 |
| SpinSpeed Add | 140 / 260 / 420 @801 |
| Reload Add | 0.12 x rank @700 |
