# Trinkets

## Globals


| Field          | Value |
| -------------- | ----- |
| MaxLevel       | 3     |
| BaseDamage     | 1     |
| MaxBouncesBase | 1     |
| EnergyBase     | 5500  |
| ReloadBase     | 0.7   |
| ReloadMin      | 0.2   |
| CostRatio      | 2     |
| TraitRatio     | 1.4   |
| SwarmRatio     | 1.4   |


## Packs


| Pack        | Rarity   | Price | Weight |
| ----------- | -------- | ----- | ------ |
| Entry       | ENTRY    | 2     | 6      |
| Junior      | UNCOMMON | 3     | 4      |
| Warrior     | RARE     | 5     | 2      |
| Abomination | EPIC     | 8     | 1      |
| Shotgun     | COMMON   | 2     | 5      |
| Rifle       | COMMON   | 2     | 5      |
| Nailgun     | UNCOMMON | 2     | 4      |
| Rocket      | RARE     | 3     | 3      |
| Rail        | RARE     | 3     | 3      |
| Laser       | RARE     | 3     | 3      |


## Entry

### SPLIT


| Field        | Value |
| ------------ | ----- |
| Code         | SPLIT |
| Title        | SPLIT |
| Blurb        |       |
| Pack         | Entry |
| MaxLevel     | 1     |
| SplitPellets | 1     |




### FAN


| Field    | Value               |
| -------- | ------------------- |
| Code     | FAN                 |
| Title    | FAN                 |
| Blurb    | Pellets spread 14°. |
| Pack     | Entry               |
| MaxLevel | 1                   |
| FanCone  | 14                  |




### PUMP


| Field       | Value |
| ----------- | ----- |
| Code        | PUMP  |
| Title       | PUMP  |
| Blurb       |       |
| Pack        | Entry |
| MaxLevel    | 1     |
| PumpPellets | 1     |
| PumpReload  | 0.25  |




## Junior



### LOAD


| Field       | Value  |
| ----------- | ------ |
| Code        | LOAD   |
| Title       | LOAD   |
| Blurb       |        |
| Pack        | Junior |
| MaxLevel    | 1      |
| LoadPellets | 2      |
| LoadReload  | 0.15   |




### CHOKE


| Field      | Value                |
| ---------- | -------------------- |
| Code       | CHOKE                |
| Title      | CHOKE                |
| Blurb      | Cone -10°. Floor 6°. |
| Pack       | Junior               |
| MaxLevel   | 1                    |
| ChokeCone  | 10                   |
| ChokeFloor | 6                    |




### MEAT


| Field        | Value                           |
| ------------ | ------------------------------- |
| Code         | MEAT                            |
| Title        | MEAT                            |
| Blurb        | +1 dmg inside 140. -100% range. |
| Pack         | Junior                          |
| MaxLevel     | 1                               |
| MeatRange    | 140                             |
| MeatBonus    | 1                               |
| MeatRangeCut | 1                               |




### RICO


| Field       | Value              |
| ----------- | ------------------ |
| Code        | RICO               |
| Title       | RICO               |
| Blurb       | Pellets bounce +1. |
| Pack        | Junior             |
| MaxLevel    | 1                  |
| RicoBounces | 1                  |




## Warrior



### GAPE


| Field    | Value      |
| -------- | ---------- |
| Code     | GAPE       |
| Title    | GAPE       |
| Blurb    | Cone +14°. |
| Pack     | Warrior    |
| MaxLevel | 1          |
| GapeCone | 14         |




### DOUBLE


| Field        | Value                                          |
| ------------ | ---------------------------------------------- |
| Code         | DOUBLE                                         |
| Title        | DOUBLE                                         |
| Blurb        | Two fans, 0.12s apart. Reload +0.55s. One mag. |
| Pack         | Warrior                                        |
| MaxLevel     | 1                                              |
| DoubleGap    | 0.12                                           |
| DoubleReload | 0.55                                           |




### KICK


| Field     | Value                 |
| --------- | --------------------- |
| Code      | KICK                  |
| Title     | KICK                  |
| Blurb     | Shove 110 inside 180. |
| Pack      | Warrior               |
| MaxLevel  | 1                     |
| KickForce | 110                   |
| KickRange | 180                   |




### STUN


| Field     | Value                                |
| --------- | ------------------------------------ |
| Code      | STUN                                 |
| Title     | STUN                                 |
| Blurb     | 0.45s stagger inside 160. No bosses. |
| Pack      | Warrior                              |
| MaxLevel  | 1                                    |
| StunTime  | 0.45                                 |
| StunRange | 160                                  |




## Abomination



### HEAP


| Field       | Value                      |
| ----------- | -------------------------- |
| Code        | HEAP                       |
| Title       | HEAP                       |
| Blurb       | +3 pellets. Reload +0.40s. |
| Pack        | Abomination                |
| MaxLevel    | 1                          |
| HeapPellets | 3                          |
| HeapReload  | 0.4                        |




### WASTE


| Field         | Value                          |
| ------------- | ------------------------------ |
| Code          | WASTE                          |
| Title         | WASTE                          |
| Blurb         | +1 dmg inside 80. -100% range. |
| Pack          | Abomination                    |
| MaxLevel      | 1                              |
| WasteRange    | 80                             |
| WasteBonus    | 1                              |
| WasteRangeCut | 1                              |




### BREACH


| Field        | Value                 |
| ------------ | --------------------- |
| Code         | BREACH                |
| Title        | BREACH                |
| Blurb        | Pellets punch 1 body. |
| Pack         | Abomination           |
| MaxLevel     | 1                     |
| BreachPierce | 1                     |




### SLUG


| Field          | Value                                |
| -------------- | ------------------------------------ |
| Code           | SLUG                                 |
| Title          | SLUG                                 |
| Blurb          | One fat slug. +2 damage for each projectile it removes. |
| Pack           | Abomination                          |
| MaxLevel       | 1                                    |
| SlugRadius     | 22                                   |
| SlugDamage     | 2                                    |
| SlugFalloffPad | 120                                  |




## Shotgun



### BUCK


| Field        | Value                         |
| ------------ | ----------------------------- |
| Code         | BUCK                          |
| Title        | BUCK                          |
| Blurb        | Say hello to my little friend |
| Pack         | Shotgun                       |
| MaxLevel     | 3                             |
| BuckPellets  | 2 / 3 / 5                     |
| BuckCone     | 10 / 16 / 24                  |
| BuckRangeCut | 0.83 / 0.94 / 1               |




### SHUCK


| Field       | Value                     |
| ----------- | ------------------------- |
| Code        | SHUCK                     |
| Title       | SHUCK                     |
| Blurb       | Reload ×0.80. Spread +8°. |
| Pack        | Shotgun                   |
| MaxLevel    | 1                         |
| Requires    | Buck                      |
| ShuckReload | 0.8                       |
| ShuckCone   | 8                         |




### SLAM


| Field       | Value                        |
| ----------- | ---------------------------- |
| Code        | SLAM                         |
| Title       | SLAM                         |
| Blurb       | Reload ×0.85. −1 projectile. |
| Pack        | Shotgun                      |
| MaxLevel    | 1                            |
| Requires    | Buck, Shuck                  |
| SlamReload  | 0.85                         |
| SlamPellets | 1                            |




## Rifle



### DRUM


| Field      | Value                                           |
| ---------- | ----------------------------------------------- |
| Code       | DRUM                                            |
| Title      | DRUM                                            |
| Blurb      | HOLD TO SHOOT MULTIPLE ROUNDS ONE AFTER ANOTHER |
| Pack       | Rifle                                           |
| MaxLevel   | 3                                               |
| DrumBurst  | 3 / 4 / 6                                       |
| DrumCycle  | 0.16                                            |
| DrumReload | 0.7                                             |




### RUSH


| Field      | Value                            |
| ---------- | -------------------------------- |
| Code       | RUSH                             |
| Title      | RUSH                             |
| Blurb      | Shots fly faster. Longer reload. |
| Pack       | Rifle                            |
| MaxLevel   | 3                                |
| RushSpeed  | 1.2 / 1.4 / 1.65                 |
| RushReload | 0.18                             |




### BELT


| Field      | Value                                     |
| ---------- | ----------------------------------------- |
| Code       | BELT                                      |
| Title      | BELT                                      |
| Blurb      | +3 burst. Reload +0.45s. Locks out TRACK. |
| Pack       | Rifle                                     |
| MaxLevel   | 1                                         |
| Requires   | Drum                                      |
| Excludes   | Spool, Sight, Bite                        |
| BeltBurst  | 3                                         |
| BeltReload | 0.45                                      |




### WALK


| Field      | Value                                                          |
| ---------- | -------------------------------------------------------------- |
| Code       | WALK                                                           |
| Title      | WALK                                                           |
| Blurb      | Later volleys spread +3° each. Reload +0.20s. Locks out TRACK. |
| Pack       | Rifle                                                          |
| MaxLevel   | 1                                                              |
| Requires   | Drum                                                           |
| Excludes   | Spool, Sight, Bite                                             |
| WalkCone   | 3                                                              |
| WalkReload | 0.2                                                            |




### SPOOL


| Field       | Value                                        |
| ----------- | -------------------------------------------- |
| Code        | SPOOL                                        |
| Title       | SPOOL                                        |
| Blurb       | Cycle ×0.65. Reload +0.25s. Locks out SWEEP. |
| Pack        | Rifle                                        |
| MaxLevel    | 1                                            |
| Requires    | Drum                                         |
| Excludes    | Belt, Walk                                   |
| SpoolCycle  | 0.65                                         |
| SpoolReload | 0.25                                         |




### SIGHT


| Field      | Value                                                   |
| ---------- | ------------------------------------------------------- |
| Code       | SIGHT                                                   |
| Title      | SIGHT                                                   |
| Blurb      | Later volleys use half spread. Slower. Locks out SWEEP. |
| Pack       | Rifle                                                   |
| MaxLevel   | 1                                                       |
| Requires   | Drum                                                    |
| Excludes   | Belt, Walk                                              |
| SightSpeed | 0.9                                                     |




### BITE


| Field     | Value                                                                |
| --------- | -------------------------------------------------------------------- |
| Code      | BITE                                                                 |
| Title     | BITE                                                                 |
| Blurb     | +1 damage on a body this burst already hit. Slower. Locks out SWEEP. |
| Pack      | Rifle                                                                |
| MaxLevel  | 1                                                                    |
| Requires  | Drum                                                                 |
| Excludes  | Belt, Walk                                                           |
| BiteSpeed | 0.8                                                                  |




### LINK


| Field      | Value                                                          |
| ---------- | -------------------------------------------------------------- |
| Code       | LINK                                                           |
| Title      | LINK                                                           |
| Blurb      | The burst finishes if you release. Cycle ×1.10. Reload +0.20s. |
| Pack       | Rifle                                                          |
| MaxLevel   | 1                                                              |
| Requires   | Drum                                                           |
| LinkCycle  | 1.1                                                            |
| LinkReload | 0.2                                                            |




### FEED


| Field      | Value                      |
| ---------- | -------------------------- |
| Code       | FEED                       |
| Title      | FEED                       |
| Blurb      | Reload ×0.75. Cycle ×1.20. |
| Pack       | Rifle                      |
| MaxLevel   | 1                          |
| Requires   | Drum                       |
| FeedReload | 0.75                       |
| FeedCycle  | 1.2                        |




### EJECT


| Field       | Value                   |
| ----------- | ----------------------- |
| Code        | EJECT                   |
| Title       | EJECT                   |
| Blurb       | Reload ×0.85. Burst −1. |
| Pack        | Rifle                   |
| MaxLevel    | 1                       |
| Requires    | Drum, Feed              |
| EjectReload | 0.85                    |
| EjectBurst  | 1                       |




## Nailgun



### PIN


| Field     | Value                                                         |
| --------- | ------------------------------------------------------------- |
| Code      | PIN                                                           |
| Title     | PIN                                                           |
| Blurb     | Nails that damage after time. Why they are exactly nine inch? |
| Pack      | Nailgun                                                       |
| MaxLevel  | 3                                                             |
| PinNails  | 2 / 3 / 5                                                     |
| PinBounce | 1 / 2 / 3                                                     |
| PinCone   | 8 / 10 / 12                                                   |
| PinRadius | 6                                                             |
| PinStick  | 0.6                                                           |




### DODGE


| Field       | Value                        |
| ----------- | ---------------------------- |
| Code        | DODGE                        |
| Title       | DODGE                        |
| Blurb       | Slip a hit. 10% / 20% / 32%. |
| Pack        | Nailgun                      |
| MaxLevel    | 3                            |
| DodgeChance | 0.1 / 0.2 / 0.32             |




### SNAP


| Field         | Value                            |
| ------------- | -------------------------------- |
| Code          | SNAP                             |
| Title         | SNAP                             |
| Blurb         | Reload only. −20% / −36% / −50%. |
| Pack          | Nailgun                          |
| MaxLevel      | 3                                |
| SnapUnlockLap | 3                                |
| SnapPrice     | 4                                |
| SnapReload    | 0.8 / 0.64 / 0.5                 |




## Rocket



### WARHEAD


| Field         | Value                                                |
| ------------- | ---------------------------------------------------- |
| Code          | WARHEAD                                              |
| Title         | WARHEAD                                              |
| Blurb         | SLOW BUT AREAL. Also damages you! PS No rocket jump! |
| Pack          | Rocket                                               |
| MaxLevel      | 3                                                    |
| WarheadRadius | 90 / 126 / 176                                       |
| WarheadSpeed  | 0.78 / 0.68 / 0.58                                   |




### MIRV


| Field           | Value                                                        |
| --------------- | ------------------------------------------------------------ |
| Code            | MIRV                                                         |
| Title           | MIRV                                                         |
| Blurb           | Each pellet splashes. Radius ×0.55. Slower. Locks out LANCE. |
| Pack            | Rocket                                                       |
| MaxLevel        | 1                                                            |
| Requires        | Warhead                                                      |
| Excludes        | Lance, Crater                                                |
| MirvRadiusScale | 0.55                                                         |
| MirvSpeed       | 0.8                                                          |




### BLOOM


| Field       | Value                                              |
| ----------- | -------------------------------------------------- |
| Code        | BLOOM                                              |
| Title       | BLOOM                                              |
| Blurb       | +80 splash radius. Reload +0.30s. Locks out LANCE. |
| Pack        | Rocket                                             |
| MaxLevel    | 1                                                  |
| Requires    | Warhead                                            |
| Excludes    | Lance, Crater                                      |
| BloomRadius | 80                                                 |
| BloomReload | 0.3                                                |




### SCORCH


| Field        | Value                                                    |
| ------------ | -------------------------------------------------------- |
| Code         | SCORCH                                                   |
| Title        | SCORCH                                                   |
| Blurb        | Splash damage 2. Slower. Reload +0.20s. Locks out LANCE. |
| Pack         | Rocket                                                   |
| MaxLevel     | 1                                                        |
| Requires     | Warhead                                                  |
| Excludes     | Lance, Crater                                            |
| ScorchDamage | 2                                                        |
| ScorchSpeed  | 0.75                                                     |
| ScorchReload | 0.2                                                      |




### LANCE


| Field            | Value                                                                            |
| ---------------- | -------------------------------------------------------------------------------- |
| Code             | LANCE                                                                            |
| Title            | LANCE                                                                            |
| Blurb            | No friendly splash. +2 direct hit. Smaller radius. No bounce. Locks out CLUSTER. |
| Pack             | Rocket                                                                           |
| MaxLevel         | 1                                                                                |
| Requires         | Warhead                                                                          |
| Excludes         | Mirv, Bloom, Scorch                                                              |
| LanceDamage      | 2                                                                                |
| LanceRadiusScale | 0.7                                                                              |
| LanceSpeed       | 0.7                                                                              |
| LanceReload      | 0.45                                                                             |




### CRATER


| Field        | Value                                                       |
| ------------ | ----------------------------------------------------------- |
| Code         | CRATER                                                      |
| Title        | CRATER                                                      |
| Blurb        | Fat body. +56 splash. No bounce. Slower. Locks out CLUSTER. |
| Pack         | Rocket                                                      |
| MaxLevel     | 1                                                           |
| Requires     | Warhead                                                     |
| Excludes     | Mirv, Bloom, Scorch                                         |
| CraterBody   | 22                                                          |
| CraterSplash | 56                                                          |
| CraterSpeed  | 0.65                                                        |




### SPOT


| Field     | Value                                                       |
| --------- | ----------------------------------------------------------- |
| Code      | SPOT                                                        |
| Title     | SPOT                                                        |
| Blurb            | Shots fly to the cursor and burst there. +40 splash radius. +1 splash damage. No bounce. Slower. |
| Pack             | Rocket                                                                                            |
| MaxLevel         | 1                                                                                                 |
| Requires         | Warhead                                                                                           |
| SpotSpeed        | 0.85                                                                                              |
| SpotSplash       | 40                                                                                                |
| SpotSplashDamage | 1                                                                                                 |




### JACK


| Field      | Value                       |
| ---------- | --------------------------- |
| Code       | JACK                        |
| Title      | JACK                        |
| Blurb      | Reload ×0.80. Splash ×0.80. |
| Pack       | Rocket                      |
| MaxLevel   | 1                           |
| Requires   | Warhead                     |
| JackReload | 0.8                         |
| JackSplash | 0.8                         |




### SLAP


| Field      | Value                      |
| ---------- | -------------------------- |
| Code       | SLAP                       |
| Title      | SLAP                       |
| Blurb      | Reload ×0.85. Speed ×0.85. |
| Pack       | Rocket                     |
| MaxLevel   | 1                          |
| Requires   | Warhead, Jack              |
| SlapReload | 0.85                       |
| SlapSpeed  | 0.85                       |




## Rail



### BORE


| Field      | Value                                       |
| ---------- | ------------------------------------------- |
| Code       | BORE                                        |
| Title      | BORE                                        |
| Blurb      | Did somebody say "RAILGUN"? Locks out LASH. |
| Pack       | Rail                                        |
| MaxLevel   | 3                                           |
| Excludes   | Lash                                        |
| BorePierce | 1 / 1 / 2                                   |
| BoreReload | 0.55                                        |




### DEEP


| Field      | Value                                     |
| ---------- | ----------------------------------------- |
| Code       | DEEP                                      |
| Title      | DEEP                                      |
| Blurb      | +2 pierce. Reload +0.40s. Locks out MASS. |
| Pack       | Rail                                      |
| MaxLevel   | 1                                         |
| Requires   | Bore                                      |
| Excludes   | Mass, Keel                                |
| DeepPierce | 2                                         |
| DeepReload | 0.4                                       |




### AWL


| Field     | Value                                                         |
| --------- | ------------------------------------------------------------- |
| Code      | AWL                                                           |
| Title     | AWL                                                           |
| Blurb     | Ignores the core face. Slower. Reload +0.20s. Locks out MASS. |
| Pack      | Rail                                                          |
| MaxLevel  | 1                                                             |
| Requires  | Bore                                                          |
| Excludes  | Mass, Keel                                                    |
| AwlSpeed  | 0.85                                                          |
| AwlReload | 0.2                                                           |




### RAM


| Field    | Value                                                                     |
| -------- | ------------------------------------------------------------------------- |
| Code     | RAM                                                                       |
| Title    | RAM                                                                       |
| Blurb    | Each body you punch through hits the next harder. Slower. Locks out MASS. |
| Pack     | Rail                                                                      |
| MaxLevel | 1                                                                         |
| Requires | Bore                                                                      |
| Excludes | Mass, Keel                                                                |
| RamSpeed | 0.75                                                                      |




### MASS


| Field      | Value                                                               |
| ---------- | ------------------------------------------------------------------- |
| Code       | MASS                                                                |
| Title      | MASS                                                                |
| Blurb      | One shot. No fan. +3 damage, plus +3 per projectile it removes. Slower. Reload +0.45s. Locks out DEEP. |
| Pack       | Rail                                                                |
| MaxLevel   | 1                                                                   |
| Requires   | Bore                                                                |
| Excludes   | Deep, Awl, Ram                                                      |
| MassDamage | 3                                                                   |
| MassSpeed  | 0.7                                                                 |
| MassReload | 0.45                                                                |




### KEEL


| Field      | Value                                                                  |
| ---------- | ---------------------------------------------------------------------- |
| Code       | KEEL                                                                   |
| Title      | KEEL                                                                   |
| Blurb      | No bounce. +2 damage. Stops on the first wall. Slower. Locks out DEEP. |
| Pack       | Rail                                                                   |
| MaxLevel   | 1                                                                      |
| Requires   | Bore                                                                   |
| Excludes   | Deep, Awl, Ram                                                         |
| KeelDamage | 2                                                                      |
| KeelSpeed  | 0.8                                                                    |




### TRACE


| Field       | Value                            |
| ----------- | -------------------------------- |
| Code        | TRACE                            |
| Title       | TRACE                            |
| Blurb       | Shots fly faster. Reload +0.25s. |
| Pack        | Rail                             |
| MaxLevel    | 1                                |
| Requires    | Bore                             |
| TraceSpeed  | 1.6                              |
| TraceReload | 0.25                             |




### RACK


| Field      | Value                      |
| ---------- | -------------------------- |
| Code       | RACK                       |
| Title      | RACK                       |
| Blurb      | Reload ×0.75. Speed ×0.85. |
| Pack       | Rail                       |
| MaxLevel   | 1                          |
| Requires   | Bore                       |
| RackReload | 0.75                       |
| RackSpeed  | 0.85                       |




### DRAW


| Field      | Value                    |
| ---------- | ------------------------ |
| Code       | DRAW                     |
| Title      | DRAW                     |
| Blurb      | Reload ×0.85. −1 pierce. |
| Pack       | Rail                     |
| MaxLevel   | 1                        |
| Requires   | Bore, Rack               |
| DrawReload | 0.85                     |
| DrawPierce | 1                        |




## Laser



### LASH


| Field          | Value                                      |
| -------------- | ------------------------------------------ |
| Code           | LASH                                       |
| Title          | LASH                                       |
| Blurb          | HOLD TO ZAP. Short reload. Locks out BORE. |
| Pack           | Laser                                      |
| MaxLevel       | 3                                          |
| Excludes       | Bore                                       |
| LashRankWeight | 12                                         |
| LashPad        | 0.4                                        |
| LashPerSecond  | 0.5                                        |
| LashMaxHold    | 1.1                                        |
| LashHit        | 1                                          |
| LashTick       | 0.6 / 0.4 / 0.2                            |
| LashTicks      | 4 / 6 / 8                                  |
| LashReload     | 0.3                                        |
| LashRange      | 1600                                       |
| LashWidth      | 8                                          |




### SEAR


| Field      | Value                                                                   |
| ---------- | ----------------------------------------------------------------------- |
| Code       | SEAR                                                                    |
| Title      | SEAR                                                                    |
| Blurb      | +1 damage while the beam stays on a body. Reload +0.15s. Locks out ARC. |
| Pack       | Laser                                                                   |
| MaxLevel   | 1                                                                       |
| Requires   | Lash                                                                    |
| Excludes   | Arc, Fork                                                               |
| SearReload | 0.15                                                                    |




### KILN


| Field      | Value                                                             |
| ---------- | ----------------------------------------------------------------- |
| Code       | KILN                                                              |
| Title      | KILN                                                              |
| Blurb      | Tick ×0.75 while latched. Narrower. Reload +0.10s. Locks out ARC. |
| Pack       | Laser                                                             |
| MaxLevel   | 1                                                                 |
| Requires   | Lash                                                              |
| Excludes   | Arc, Fork                                                         |
| KilnTick   | 0.75                                                              |
| KilnWidth  | 0.75                                                              |
| KilnReload | 0.1                                                               |




### ARC


| Field    | Value                                                              |
| -------- | ------------------------------------------------------------------ |
| Code     | ARC                                                                |
| Title    | ARC                                                                |
| Blurb    | Jumps once to a neighbor within 220. Slower tick. Locks out BRAND. |
| Pack     | Laser                                                              |
| MaxLevel | 1                                                                  |
| Requires | Lash                                                               |
| Excludes | Sear, Kiln                                                         |
| ArcRange | 220                                                                |
| ArcTick  | 1.15                                                               |




### FORK


| Field      | Value                                                        |
| ---------- | ------------------------------------------------------------ |
| Code       | FORK                                                         |
| Title      | FORK                                                         |
| Blurb      | Side bolts hit on their own. Reload +0.12s. Locks out BRAND. |
| Pack       | Laser                                                        |
| MaxLevel   | 1                                                            |
| Requires   | Lash                                                         |
| Excludes   | Sear, Kiln                                                   |
| ForkReload | 0.12                                                         |




### SHUNT


| Field      | Value                                              |
| ---------- | -------------------------------------------------- |
| Code       | SHUNT                                              |
| Title      | SHUNT                                              |
| Blurb      | Ignores shields and plates. Slower tick. Narrower. |
| Pack       | Laser                                              |
| MaxLevel   | 1                                                  |
| Requires   | Lash                                               |
| ShuntTick  | 1.25                                               |
| ShuntWidth | 0.85                                               |




### LINGER


| Field        | Value                                                   |
| ------------ | ------------------------------------------------------- |
| Code         | LINGER                                                  |
| Title        | LINGER                                                  |
| Blurb        | Remaining ticks finish where you let go. Reload +0.20s. |
| Pack         | Laser                                                   |
| MaxLevel     | 1                                                       |
| Requires     | Lash                                                    |
| LingerReload | 0.2                                                     |




### CELL


| Field      | Value                               |
| ---------- | ----------------------------------- |
| Code       | CELL                                |
| Title      | CELL                                |
| Blurb      | +2 ticks per charge. Reload +0.15s. |
| Pack       | Laser                               |
| MaxLevel   | 1                                   |
| Requires   | Lash                                |
| CellTicks  | 2                                   |
| CellReload | 0.15                                |




### VENT


| Field      | Value                     |
| ---------- | ------------------------- |
| Code       | VENT                      |
| Title      | VENT                      |
| Blurb      | Reload ×0.80. Tick ×1.20. |
| Pack       | Laser                     |
| MaxLevel   | 1                         |
| Requires   | Lash                      |
| VentReload | 0.8                       |
| VentTick   | 1.2                       |




### COOL


| Field      | Value                      |
| ---------- | -------------------------- |
| Code       | COOL                       |
| Title      | COOL                       |
| Blurb      | Reload ×0.85. Width ×0.80. |
| Pack       | Laser                      |
| MaxLevel   | 1                          |
| Requires   | Lash, Vent                 |
| CoolReload | 0.85                       |
| CoolWidth  | 0.8                        |




## Not in RoundTraits.All



### SPIN


| Field      | Value                                                |
| ---------- | ---------------------------------------------------- |
| Code       | SPIN                                                 |
| Title      | SPIN                                                 |
| Blurb      | Shots sweep harder against the clock. Longer reload. |
| Pack       | Nailgun                                              |
| InPool     | false                                                |
| SpinBase   | 220                                                  |
| SpinBoost  | 140 / 260 / 420                                      |
| SpinReload | 0.12                                                 |


