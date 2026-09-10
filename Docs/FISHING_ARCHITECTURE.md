# Fishing Physics Architecture

## Scope

This document defines the foundation for the fishing vertical slice. It replaces the prototype behaviour where several scripts could independently move the fish, sinker or line.

## Physical chain

```text
Rod tip -> FishingLine -> physical rig / lead -> leader and hook (Phase 3) -> fish
```

For Phase 1, the existing `Sinker` component is the physical root of the temporary rig. It owns the lead Rigidbody, water behaviour and lake-bed hold. A separate hook/leader representation is introduced with the fish hookup work, not as an unused second system now.

## Ownership

- `CastingSystem` creates one rig and gives it an initial launch velocity. It never moves the rig afterwards.
- `FishingLine` owns the physical joint from rod tip to rig, the available line length, tension and break state.
- `Sinker` owns the lead Rigidbody and water/lake-bed state. On the lake bed it uses a breakable physics joint to the world; it is not made kinematic or positioned by script.
- `FishingReel` only changes available line length. It never adds a force directly to the rig or moves a fish.
- `FishAI` chooses intent. `FishFightController` is responsible for fish movement physics in the later fish phase.
- On a hookup, the rig releases its lake-bed hold and is connected to the fish body through a physics joint. The main line continues to target the rig; it does not switch directly to the fish.

## Phase order

1. Rod, line, rig and lead: launch, water entry, sinking, stable lake-bed hold and physical response.
2. Reel and drag: line recovery/payout and drag-limited load.
3. Hook and fish connection.
4. Fish fight, stamina, obstacles and rod load.
5. Failure and landing.

## Scene requirements

- `FishingLine.lineStart` is an object at the rod tip.
- The lead prefab has a Rigidbody, Collider and `Sinker`.
- The lake bed has a non-trigger Collider and is included in `WaterDepth.lakeBedLayer`.
- The water volume or surface identifies the relevant `WaterDepth`.

## Explicitly forbidden

- Parenting a live physics rig to the rod.
- Setting a live rig or fish Transform to simulate motion.
- Changing FishingLine's target from rig to fish during a fight.
- Moving a fish toward the player on reel input.
