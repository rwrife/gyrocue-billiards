# Man-Cave Asset Purchase List

`Practice.unity` now contains a fixed, editor-editable room: every object below is a
plain GameObject in the scene hierarchy under `ManCaveRoom`, built from Unity primitives
and materials in `Assets/Materials/ManCave/`. Nothing about the room is code-driven —
select, move, delete, or replace any of it in the editor.

Placeholders that stand in for assets to purchase are grouped under `PH_*` parents or
named for the thing they represent. To swap one in: import the purchased asset, drop it
at the placeholder's position, and delete the placeholder. All positions are real-world
metres (the room is ~7.6m x 6m, floor at y = -0.79, table cloth at y = 0), so correctly
scaled store assets will fit without rescaling.

## Furniture and fixtures

| Placeholder (hierarchy path) | What to buy | Notes |
|---|---|---|
| `ManCaveRoom/TableBase` | Pool table body: cabinet, legs, rail woodwork | The playfield, cushions and pockets stay code-driven for physics; buy the *look*, keep our collision. Playfield is 2.54m x 1.27m (9ft), rail top at y ≈ 0.04 |
| `ManCaveRoom/Bar` (BarTop, BarFront, BarKick) | L-shaped or straight home bar counter, dark wood | ~2.6m run along the east wall, counter top at 1.08m above floor |
| `ManCaveRoom/Bar/Stool*` (x4) | Bar stools, wood + leather seat | Seat height ~0.72m |
| `ManCaveRoom/Bar/BackBarShelf*` + `Bottle_*` (x27) | Back-bar shelving + liquor bottle pack | Emissive/glass shader bottles read best under the shelf glow strip |
| `ManCaveRoom/Fireplace` | Brick fireplace with mantel | ~1.7m wide surround; swap `FireGlow` + `FireLogA/B` for a fire VFX (particles or flipbook) and keep the orange point light `Lighting/FireLight` |
| `ManCaveRoom/Lighting/PendantShade*` (x2) | Industrial dome pendant lamps | Over the table; the actual Unity spot lights stay — replace visuals only |
| `ManCaveRoom/Lighting/BarPendantShade*` (x2) | Small pendant lamps | Over the bar |
| `ManCaveRoom/SouthWallDecor/CueRack` + `RackCue*` | Wall-mounted cue rack with 4 cues | South wall centre |

## Game machines

| Placeholder | What to buy | Notes |
|---|---|---|
| `ManCaveRoom/PH_GameMachines/PH_Pinball_*` | Pinball machine, playable-look with emissive backglass | North-west corner, angled body; ~1.4m tall head |
| `ManCaveRoom/PH_GameMachines/PH_Arcade_*` | Upright arcade cabinet with emissive marquee + screen | North wall, east of the fireplace |

## Wall decor and memorabilia

| Placeholder | What to buy | Notes |
|---|---|---|
| `ManCaveRoom/NeonSigns/Sign_*` (x6, each with `_Back`) | Neon sign pack: BAR, beer brands, WHISKEY, BREWERY, BILLIARDS LOUNGE | Keep emissive materials; each sign pairs with a coloured point light in `Lighting/NeonWash*` |
| `ManCaveRoom/WestWallDecor/TVFrame` + `TVScreen` | Flat-screen TV | Swap `TVScreen`'s material for a video texture (sports loop); `Lighting/TVGlow` provides the cast light |
| `ManCaveRoom/WestWallDecor/FrameW*` + `ArtW*` (x5), `SouthWallDecor/FrameS*` + `ArtS*` (x5) | Framed sports photos / posters pack | Mixed sizes, 0.3–0.6m |
| `ManCaveRoom/EastWallDecor/Dartboard` + `DartCabinet` + `DartBull` | Dartboard with wooden cabinet | East wall at 1.7m height |
| `ManCaveRoom/EastWallDecor/ChalkboardMenu` + `ChalkFrame` | Chalkboard menu sign | Hand-written drinks menu texture |
| `ManCaveRoom/PH_Memorabilia/PH_DeerMount_*` | Mounted deer head on plaque | West wall, high |
| `ManCaveRoom/PH_Memorabilia/PH_Pennant*` (x3) | Felt sports pennants | Triangular; primitives can only fake these |
| `ManCaveRoom/PH_Memorabilia/PH_JerseyFrame*` + `PH_Jersey*` (x2) | Framed jerseys | North wall above the fireplace mantel |
| `ManCaveRoom/PH_Memorabilia/PH_Vinyl*` (x4 + labels) | Framed vinyl records | North wall, east end |
| `ManCaveRoom/PH_Memorabilia/PH_WallClock_*` | Wall clock | North wall centre-high |
| `ManCaveRoom/Fireplace/MantelClock` | Mantel clock | On the mantel shelf |

## Shelved collectibles (south-east corner, `PH_CollectibleShelves`)

| Placeholder | What to buy | Notes |
|---|---|---|
| `PH_Shelf0..4` | Wall shelf set (5 shelves) | 1m wide, east wall |
| `PH_Book_a*`, `PH_Book_b*` (x13) | Book row pack | Two lower shelves |
| `PH_Trophy*` (x2 cup + base) | Trophy pack | Middle shelf |
| `PH_Helmet*` (x2) | Football helmets | Fourth shelf |
| `PH_ModelCar*` (x3) | Die-cast model cars | Top shelf |

## Materials to upgrade (`Assets/Materials/ManCave/`)

The room's surfaces are flat-colour Standard-shader materials. Replace with textured PBR
sets, keeping the same material asset names so the scene picks them up without rewiring:

| Material | Replace with |
|---|---|
| `MC_Brick`, `MC_BrickDark` | Red brick PBR (albedo/normal/roughness), used on north + east walls and fireplace |
| `MC_WoodFloor` | Wide-plank hardwood PBR |
| `MC_WoodDark`, `MC_WoodMid` | Stained wood panelling PBR (south + west walls, wainscoting, beams, bar) |
| `MC_Rug`, `MC_RugBorder` | Persian rug texture on a plane |
| `MC_Leather` | Leather PBR (stool seats) |
| `MC_Neon*`, `MC_SignWhite` | Keep emissive; real signs bring their own |
| `MC_FireGlow` | Retire once a fire VFX replaces the glow quad |

## Not for purchase

- Balls, cue stick, playfield/cushion/pocket geometry: code-built by `PracticeTableBuilder`
  for physics reasons. Only their *materials* should be upgraded (ball set with numbers,
  `MC_Felt`-style cloth).
- All lights under `ManCaveRoom/Lighting`: these are the scene's real Unity lights, tuned
  for the dramatic look. Purchased fixtures replace the visible lamp meshes only.
