# Fishing System Specification

## Objective

Create a believable basic carp fishing loop for the vertical slice without requiring a full simulation of every real-world variable.

## Casting

Input should determine cast direction and power. The cast produces a line/rig state in the water and records the approximate landing position.

Future improvements can add casting technique, wind influence and accuracy.

## Rig state

A rig can be:

- In hand
- Casting
- In flight
- In water
- Fishing
- Hooked
- Retrieved

## Bite

A fish can investigate bait before taking it. A successful take changes the rig/fish state and triggers the bite/alarm system.

The first prototype can use simplified probability and distance checks; later versions can incorporate species, bait, depth, temperature, pressure and weather.

## Fight

The fight should model:

- Fish weight
- Fish strength
- Fish stamina
- Direction
- Line tension
- Rod pressure
- Drag
- Obstacles

The player should manage tension rather than simply hold one button.

## Failure

Possible outcomes:

- Successful landing
- Hook pulled
- Line break
- Fish reaches an obstacle
- Other controlled failure states added later

## Landing

The landing sequence should transition from active fight to a controlled fish-in-hand state. This is the emotional payoff of the loop and should feel physical and deliberate.

## First implementation order

1. Rod interaction
2. Casting placeholder
3. Rig in water
4. Fish target
5. Bite event
6. Alarm
7. Hooked state
8. Simple tension/fight model
9. Landing trigger
10. Fish capture
