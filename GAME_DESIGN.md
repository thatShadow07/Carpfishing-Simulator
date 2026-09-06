# Game Design Document

## 1. Vision

CarpFishing Simulator is a first-person carp fishing simulator built around the complete fishing-session fantasy rather than only the moment of catching a fish.

The player prepares equipment, travels to a venue, reads the water, chooses a swim, builds rigs, fishes, fights and lands carp, records catches and improves their equipment.

The target is realistic enough to satisfy anglers while remaining approachable to players who have never fished.

## 2. Core pillars

### Authenticity
Fishing decisions should matter: bait, rig, location, weather, time, water conditions and fish behaviour influence results.

### Immersion
The game should feel like going fishing: loading the car, arriving at the lake, setting up the bivvy, casting, waiting and reacting to the alarm.

### Accessibility
Realism must not become tedious. Complex systems should be understandable through interaction and feedback.

### Atmosphere
Water, lighting, vegetation, weather, wildlife, audio and day/night transitions are major parts of the experience.

### Performance
The game should target mid-range PCs through sensible geometry, LODs, culling, efficient AI, pooling and optimized materials.

## 3. Core loop

Prepare → Buy → Travel → Explore → Choose spot → Set up → Build rig → Cast/Fish → Bite → Fight → Land → Capture → Log → Upgrade → Repeat.

## 4. First playable vertical slice

Deadline: **March 1, 2027**

The slice should contain one small lake and a complete basic fishing session:

- First-person controller
- Fishing rod and reel
- Basic tackle
- Casting
- Rig in water
- Carp AI
- Bite/alarm system
- Fish fight
- Landing
- Fish-in-hand capture moment
- Basic catch log

## 5. Fish

Initial species:

- Common carp
- Mirror carp
- Barbel

Later:

- Grass carp
- Leather carp
- Crucian carp
- Other venue-specific species

Fish should have attributes such as weight, strength, stamina, preferred depths, feeding behaviour and habitat preferences.

## 6. Fish AI

Main states:

1. Roaming
2. Exploring
3. Feeding
4. Investigating bait
5. Taking bait
6. Hooked
7. Fighting
8. Landing

Influencing factors:

- Depth
- Water temperature
- Time of day
- Weather
- Food availability
- Fishing pressure
- Player presence
- Vegetation and obstacles
- Lake characteristics

## 7. Fishing and fight system

The fight should communicate weight, direction, strength and fatigue.

Player controls should include:

- Rod direction
- Drag/tension
- Line recovery
- Positioning

Possible failure states:

- Line break
- Hook loss
- Loose fish
- Fish reaching an obstacle

## 8. Rig building

Rig construction happens at a workbench and is intentionally physical.

Basic workflow:

Hooklink → Hook → Knotless knot → Hair → Boilie → Stopper

Parameters:

- Hair length
- Hook size
- Hooklink length
- Bait size
- Bait type

Initial bait types:

- Boilies: 15/20/24 mm
- Pop-ups
- Pellets: 4/6/8 mm
- Particle mixes
- Fake corn
- PVA

Planned rigs:

- Basic hair rig
- Snowman
- Chod
- Ronnie
- Hinged stiff rig

## 9. World

The first venue is one lake with:

- Multiple depths
- Different bottom types
- Vegetation
- Obstacles
- Better and worse swims

The player initially knows little about the venue and learns it through observation and fishing.

Future venues can introduce different fish populations, water conditions and terrain.

## 10. Travel and bivvy

The player can drive to fishing locations and organize equipment in the car trunk.

Session equipment can include:

- Rods
- Rod pod
- Bivvy
- Bedchair
- Buckets
- Tackle
- Bait

Travel, roads, fuel and fast travel can be expanded later.

## 11. Weather and time

Initial conditions:

- Sunny
- Cloudy
- Rain
- Fog
- Wind

Time:

- Dawn
- Day
- Sunset
- Night

These systems should eventually affect fish behaviour and atmosphere.

## 12. Progression

The player starts with basic equipment, catches fish, earns money/reputation and upgrades gear.

Money sinks include:

- Bait
- Tackle
- Rods
- Reels
- Maintenance
- Travel
- Advanced equipment

## 13. Future multiplayer

Multiplayer is not part of the vertical-slice priority. Long-term target: cooperative sessions for 2–4 players.
