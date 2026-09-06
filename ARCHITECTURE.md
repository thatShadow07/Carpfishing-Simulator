# Technical Architecture

## Principles

- Simple before clever.
- Small, focused systems.
- Data separated from behaviour where useful.
- Gameplay systems should be testable independently.
- Avoid premature abstraction.
- Keep Unity-specific code at clear boundaries.

## Planned systems

### Player

- FirstPersonController
- PlayerInteraction
- PlayerInventory

### Fishing

- FishingRod
- FishingReel
- FishingLine
- CastingSystem
- FishingRig
- BiteSystem
- AlarmSystem
- FishFightSystem
- LandingSystem

### Fish

- FishController
- FishAI
- FishStateMachine
- FishSpeciesData
- FishStats

### Equipment

- EquipmentData
- TackleData
- BaitData
- InventorySystem
- EquipmentInteraction

### Rig building

- RigBuilder
- RigComponent
- HookComponent
- HooklinkComponent
- HairComponent
- BaitComponent

### World

- LakeController
- Swim/Spot data
- Water system
- Depth data
- Environment/weather
- DayNightSystem

### Progression

- CurrencySystem
- ShopSystem
- CatchLog
- SaveSystem

## Data-driven design

Use ScriptableObjects for static game data where appropriate:

- Fish species
- Baits
- Hooks
- Rods
- Reels
- Lines
- Hooklinks
- Leads
- Equipment

Runtime state should remain separate from asset definitions.

## Folder convention

```text
Assets/Scripts/
├── Core/
├── Player/
├── Fishing/
├── Fish/
├── Equipment/
├── Rigging/
├── World/
├── Progression/
├── UI/
└── Utilities/
```

## Naming

- Classes: PascalCase
- Methods: PascalCase
- Public properties: PascalCase
- Private fields: camelCase, preferably serialized only when needed
- Interfaces: `I` + PascalCase
- ScriptableObjects: descriptive `...Data` names

## Collaboration rule

If a change requires moving systems, changing ownership of responsibilities or introducing a major framework, document the reason before implementing it.
