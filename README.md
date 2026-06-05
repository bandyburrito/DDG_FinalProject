# Shoki's Adventure

A turn-based tactical roguelite made in Unity 6.

You play as Shoki — an alien whose ship has crash-landed on Earth. Government forces are closing in. Survive 10 waves of soldiers on an isometric grid, recruit companions between rounds, and escape before they capture you.

---

## How to play

| Input | Action |
|---|---|
| Left click | Move to tile / attack target |
| Q | Toggle melee ↔ ranged |
| Space | Skip current phase (move → attack → end turn) |
| Esc | Pause |

**Each turn:** move up to 2 tiles, then attack — or skip either phase with Space.  
**Watch the yellow tiles** — enemies commit to telegraphed attacks one turn in advance. Step off them.

---

## Characters

| Name | Role |
|---|---|
| Shoki | You. Melee and ranged, upgrades through the run |
| Drone | Companion — ranged glass cannon |
| Brawler | Companion — tanky melee, soaks hits |
| Trickster | Companion — fast, acts twice per turn |

Companions are recruited at waves 3, 6, and 9. They die permanently if their HP hits zero.

---

## Enemies

| Type | Behaviour |
|---|---|
| Soldier | Basic melee rush |
| Sniper | Stays at range, retreats if you close in |
| Heavy | Slow, high HP, hits hard |

---

## Upgrades

After each wave choose +25% melee or +25% ranged damage.  
At **3 melee upgrades** your strike hits all 8 surrounding tiles at once.  
At **3 ranged upgrades** your shot detonates in a 3×3 blast on landing.

---

## Built with

- Unity 6 (6000.3.2f1) · URP 2D
- C# · OnGUI rendering
- Pixel art assets made in Aseprite

---

*HSLU DDG module project — Week 6 submission.*
