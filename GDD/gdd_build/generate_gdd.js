const fs = require("fs");
const path = require("path");
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  ImageRun, AlignmentType, LevelFormat, BorderStyle, WidthType, ShadingType,
  PageBreak, HeadingLevel, PageOrientation
} = require("docx");

// ── Assets ───────────────────────────────────────────────────────────────────
const SPRITE_DIR = "/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/Assets/Resources/Entities";
const TILE_DIR   = "/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/Assets/Resources/Tiles";

function readSpriteIfExists(filePath) {
  try { return fs.readFileSync(filePath); } catch { return null; }
}

const sprites = {
  shoki:   readSpriteIfExists(path.join(SPRITE_DIR, "shoki.png")),
  sniper:  readSpriteIfExists(path.join(SPRITE_DIR, "sniper.png")),
  heavy:   readSpriteIfExists(path.join(SPRITE_DIR, "heavy.png")),
  drone:   readSpriteIfExists(path.join(SPRITE_DIR, "drone.png")),
  ground:  readSpriteIfExists(path.join(TILE_DIR,   "ground.png")),
  ground2: readSpriteIfExists(path.join(TILE_DIR,   "ground_alt.png")),
};

const coreLoopDiagram = readSpriteIfExists("/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/GDD/gdd_build/core_loop.png");

// ── Styling helpers ──────────────────────────────────────────────────────────
const border = { style: BorderStyle.SINGLE, size: 1, color: "BBBBBB" };
const borders = { top: border, bottom: border, left: border, right: border };

function p(text, opts = {}) {
  return new Paragraph({
    spacing: { after: 60 },
    children: [new TextRun({ text, ...opts })],
  });
}
function bullet(text) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    children: [new TextRun(text)],
  });
}
function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    children: [new TextRun(text)],
  });
}
function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    children: [new TextRun(text)],
  });
}
function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    children: [new TextRun(text)],
  });
}
function rich(...runs) {
  return new Paragraph({
    spacing: { after: 60 },
    children: runs.map(r => typeof r === "string" ? new TextRun(r) : new TextRun(r)),
  });
}

// Table cell with padding
function cell(text, opts = {}) {
  return new TableCell({
    borders,
    width: { size: opts.w || 2340, type: WidthType.DXA },
    shading: opts.header ? { fill: "EDEDED", type: ShadingType.CLEAR } : undefined,
    margins: { top: 50, bottom: 50, left: 100, right: 100 },
    children: [new Paragraph({
      children: [new TextRun({ text, bold: !!opts.bold || !!opts.header, size: 18 })],
    })],
  });
}

// Simple table builder. cols = array of widths in DXA. rows = array of arrays of strings.
function table(cols, rows, firstRowHeader = true) {
  const totalWidth = cols.reduce((a, b) => a + b, 0);
  return new Table({
    width: { size: totalWidth, type: WidthType.DXA },
    columnWidths: cols,
    rows: rows.map((cells, rowIdx) => new TableRow({
      children: cells.map((text, colIdx) => cell(text, {
        w: cols[colIdx],
        header: rowIdx === 0 && firstRowHeader,
      })),
    })),
  });
}

// Image wrapper that handles missing assets gracefully
function spriteImg(spriteKey, width = 80, height = 80) {
  const data = sprites[spriteKey];
  if (!data) {
    return new Paragraph({ children: [new TextRun({ text: `[${spriteKey}.png]`, italics: true, color: "808080" })] });
  }
  return new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 60 },
    children: [new ImageRun({
      type: "png",
      data,
      transformation: { width, height },
      altText: { title: spriteKey, description: `${spriteKey} sprite`, name: spriteKey },
    })],
  });
}

// Caption under a sprite
function caption(text) {
  return new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 160 },
    children: [new TextRun({ text, size: 18, italics: true, color: "606060" })],
  });
}

// ── Page sections ────────────────────────────────────────────────────────────

const coverPage = [
  new Paragraph({ spacing: { before: 1600, after: 0 }, children: [] }),
  new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 200 },
    children: [new TextRun({ text: "Shoki's Adventure", size: 72, bold: true })],
  }),
  new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 600 },
    children: [new TextRun({ text: "Game Design Document", size: 36, color: "606060" })],
  }),
  new Paragraph({
    alignment: AlignmentType.CENTER,
    children: [new TextRun({ text: "DDG Module — Week 6 Submission", size: 26 })],
  }),
  new Paragraph({
    alignment: AlignmentType.CENTER,
    spacing: { after: 240 },
    children: [new TextRun({ text: "Engine: Unity 6  •  Art: 2D pixel  •  Genre: Turn-based tactical roguelite", size: 22, color: "606060" })],
  }),
  new Paragraph({ children: [new PageBreak()] }),
];

const versionHistory = [
  h1("Version History"),
  p("Each entry below corresponds to one documented iteration of the prototype — a playtest, a decision, or a measured fix. Iteration 1 captures the original concept; subsequent entries are dated to the development sprints."),
  table([1080, 2160, 6120], [
    ["Iteration", "Author / Phase", "Summary"],
    ["1", "Concept",        "Pivot from top-down 30×30 to isometric 8×8. Reduced movement scope; rewrote spatial systems."],
    ["2", "Pre-prototype",  "Dropped Pokémon-type counter system. Added 3-companion roster and Into the Breach-style enemy telegraphs."],
    ["3", "Playtest sprint 1", "Difficulty rebalance — enemy damage cut ~45%; post-wave heal raised 50%→75%."],
    ["4", "Playtest sprint 2", "Grid silhouette variation: Void tile type, corner chips and interior holes per wave."],
    ["5", "Juiciness pass",   "Top-face-only tile highlights; floating HP bars; procedural hit-burst particles."],
    ["6", "Animation prep",   "Smooth tile-to-tile path-walking replaces instant teleport for player, enemies, and companions."],
    ["7", "Stability sprint", "BFS-reachable move highlights; turn-order desync fix when an entity dies mid-turn."],
  ]),
  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const gameOverview = [
  h1("Game Overview"),
  h2("Game Concept"),
  p("Shoki is an alien whose spacecraft has crash-landed on Earth. Government human forces are closing in to capture him. The player must survive 10 escalating waves of human soldiers on an 8×8 isometric grid, repair the ship between rounds, and escape home."),
  p("The game blends the readable telegraph system of Into the Breach with the build-craft progression of a roguelite. Every turn is a small puzzle: where to move, melee or ranged, bait into a trap or avoid it, save HP for later."),

  h2("Feature Set"),
  bullet("Turn-based tactical combat on an isometric 8×8 grid"),
  bullet("Initiative-based turn order (d20 + speed)"),
  bullet("3 enemy archetypes — Soldier (melee grunt), Sniper (ranged), Heavy (high-HP melee)"),
  bullet("3 recruitable companions — Drone (glass cannon), Brawler (tank), Trickster (acts twice)"),
  bullet("Enemy telegraph system — every attack and move shown one turn in advance"),
  bullet("Environmental traps that affect both player and enemies"),
  bullet("Permadeath companions + damage-multiplier upgrades stacked across the run"),
  bullet("Procedural room layout regenerated each wave"),

  h2("Genre & Audience"),
  p("Genre: Turn-based tactical roguelite. Target audience: players who enjoy Into the Breach, Mewgenics, FTL, and Shovel Knight — readable systems, high replayability, short runs (~20 minutes)."),

  h2("LeBlanc Fun Type"),
  rich({ text: "Primary: ", bold: true }, { text: "Challenge" }, { text: " — every turn is a readable but non-trivial decision. No hidden information; outcome rests on the player reading the board correctly." }),
  rich({ text: "Secondary: ", bold: true }, { text: "Discovery" }, { text: " — wave-by-wave room regeneration and companion picks create distinct run shapes." }),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const gameplay = [
  h1("Gameplay & Mechanics"),

  h2("Core Loop"),
  rich({ text: "Micro (one turn): ", bold: true }, "Move ≤ moveRange tiles → trap check on arrival → optional attack (melee or ranged) → end turn → enemies execute telegraphed plans."),
  rich({ text: "Macro (one wave): ", bold: true }, "Survive all enemies → 75% of wave damage heals → +25% melee or ranged upgrade → at waves 3, 6, 9 choose a companion → new room generates."),
  rich({ text: "Meta (one run): ", bold: true }, "Survive all 10 waves. HP carries across waves; damage multipliers compound; companion roster grows. Permadeath on companion loss; full restart on player loss."),

  // Embedded core loop diagram
  coreLoopDiagram
    ? new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 200, after: 200 },
        children: [new ImageRun({
          type: "png",
          data: coreLoopDiagram,
          transformation: { width: 600, height: 360 },
          altText: { title: "Core Loop", description: "Micro / Macro / Meta loop diagram", name: "core_loop" },
        })],
      })
    : new Paragraph({ children: [new TextRun({ text: "[core_loop diagram]", italics: true, color: "808080" })] }),
  caption("Figure 1 — Core loop diagram. Micro = one turn; Macro = one wave; Meta = full run."),

  h2("Movement"),
  p("Cardinal-only pathfinding on the grid (4-direction BFS). Player moveRange = 2 tiles per turn. Move highlights show only tiles reachable in those steps via the same BFS — so highlighted tiles always match what the player can actually walk to."),
  p("Movement plays smoothly: each tile transition is a ~0.16-second position lerp. The walker faces the destination at the start of the step and traps fire on visual arrival rather than at click-time."),

  h2("Combat"),
  p("Two attack modes, toggled with Q:"),
  bullet("Melee — hits all 8 adjacent tiles. Higher damage. Forces close engagement."),
  bullet("Ranged — single target at Chebyshev distance 2–4. Lower damage. Keeps Shoki safe but rewards good positioning."),
  p("Each mode has its own damage multiplier (+25% per wave-upgrade), forcing the player to commit to a build over the 10-wave run."),

  h2("Enemy Statistics"),
  table([1800, 1200, 1200, 1600, 1600, 1960], [
    ["Enemy",   "HP", "Speed", "Move Range", "Damage", "Behavior"],
    ["Soldier", "30", "1", "2", "8 melee",    "Path toward Shoki, attack adjacent"],
    ["Sniper",  "20", "2", "2", "5 ranged",   "Keep 2–4 tiles from Shoki"],
    ["Heavy",   "60", "0", "1", "12 melee",   "Slow approach, heavy hit"],
  ]),

  h2("Companion Statistics"),
  table([1800, 1200, 1200, 1600, 1600, 1960], [
    ["Companion", "HP", "Speed", "Move Range", "Damage", "Niche"],
    ["Drone",     "20", "5", "3", "6 ranged",  "Glass cannon — keeps 2–4 tile range"],
    ["Brawler",   "40", "1", "2", "12 melee",  "Tank — soaks hits"],
    ["Trickster", "25", "6", "3", "5 ranged",  "Acts twice per turn"],
  ]),

  h2("Traps"),
  table([1500, 7860], [
    ["Trap",      "Effect"],
    ["Spike",     "10 damage on entry, then consumed (tile reverts to ground)"],
    ["Pit",       "Instant kill (treated as 9999 damage); does not consume"],
    ["Slow Zone", "Applies −1 moveRange for one turn; does not consume"],
  ]),
  p("Traps trigger on collision for both player and enemy — emergent strategy comes from baiting telegraphed enemy movement onto spikes or pits."),

  h2("Randomness & Balancing"),
  bullet("Initiative roll each round (d20 + speed) determines turn order"),
  bullet("Room layout regenerated per wave: trap positions, obstacles, void cluster shapes, spawn points"),
  bullet("Random spawn pool with minimum 3-tile separation from Shoki"),
  bullet("Wave composition fixed (deterministic) — escalation is a designer-controlled curve, not randomized"),
  p("Balancing target: a competent player should clear 10 waves on ~60% of attempts. The Iteration 3 rebalance brought worst-case round damage from ~63 down to ~33 — recoverable thanks to the 75% post-wave heal."),

  h2("Wave Progression Curve"),
  p("The 10-wave composition forms an intentional difficulty ramp. Enemy counts grow modestly; archetype variety is the main lever (snipers introduced at wave 4, heavies at wave 6, multi-heavy compositions at the end)."),
  table([1100, 1400, 1400, 1400, 4060], [
    ["Wave", "Soldiers", "Snipers", "Heavies", "Notes"],
    ["1",  "2", "0", "0", "Onboarding — pure melee rush"],
    ["2",  "2", "0", "0", "Same composition, learning curve"],
    ["3",  "3", "0", "0", "First Soldier increase → first Companion pick"],
    ["4",  "2", "1", "0", "Sniper introduced — ranged threat"],
    ["5",  "2", "1", "0", "Practice mixed composition"],
    ["6",  "2", "1", "1", "Heavy introduced → second Companion pick"],
    ["7",  "2", "2", "0", "Double sniper — positioning matters most"],
    ["8",  "1", "2", "1", "Hardest single composition"],
    ["9",  "2", "1", "1", "Brief breather → third Companion pick"],
    ["10", "2", "1", "2", "Boss-feel finale — two heavies"],
  ]),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const interactiveElements = [
  h1("Two Interactive Elements"),
  p("The DDG brief requires at least two interactive elements (beyond the player character) that follow the sensory-input → processing → action pattern, and that can interact with each other. Shoki's Adventure meets this requirement with two systems that explicitly form a feedback loop."),

  h2("Interactive Element 1 — Enemy AI"),
  rich({ text: "Sensory input (See): ", bold: true }, "At the start of every round each enemy queries TurnManager for all player-faction entities (Shoki + companions) and selects the nearest by Manhattan distance."),
  rich({ text: "Processing: ", bold: true }, "EnemyAI.ComputePlan() determines a move destination (Soldier/Heavy approach target; Sniper maintains 2–4 tile distance) and decides whether to plan an attack (in-range after move). The plan is rendered as telegraph overlays — yellow on the attack tile, grey on the move tile."),
  rich({ text: "Action: ", bold: true }, "On the enemy's turn they EXECUTE the telegraphed plan. They walk the planned path tile-by-tile and attack the planned tile even if Shoki has moved. This commit-to-plan design is deliberate — it gives the player a fair information advantage they can act on."),

  h2("Interactive Element 2 — Environmental Traps"),
  rich({ text: "Sensory input (Collision): ", bold: true }, "Each tile has an Entity.MoveTo() collision hook — when any entity arrives on a tile, TrapSystem.ResolveTrap is invoked with that entity and tile."),
  rich({ text: "Processing: ", bold: true }, "TrapSystem switches on the tile's trap type. Spike → 10 damage + tile reverts to ground. Pit → 9999 damage (instant kill), tile persists. Slow Zone → applies a 1-turn moveRange penalty, tile persists."),
  rich({ text: "Action: ", bold: true }, "The trap calls entity.TakeDamage (which triggers hit-burst particles and HP-bar refresh) or applies a status effect, and may rewrite the tile state via SetTileType."),

  h2("How the two elements interact"),
  p("Because enemies COMMIT to their telegraphed plans, the player can read enemy intent and deliberately bait them onto trap tiles. The Enemy AI's pathfinder treats Spike, Pit, and Slow Zone tiles as walkable — they're not aware of their own danger. This creates emergent strategy: rather than always attacking enemies directly, the player can save HP and damage by moving INTO a position that forces the telegraphed enemy path to cross a Pit on the next turn."),
  p("Concrete example: a Heavy is two tiles north of Shoki, telegraphed to walk south and attack. There's a Pit one tile south of the Heavy. If Shoki moves one tile east, the telegraphed path still goes through the Pit — the Heavy walks in and dies. This is the system-on-system interaction the DDG requirement asks for."),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const paj = [
  h1("Playability, Accessibility & Juiciness"),
  p("Three rubric-explicit qualities the DDG brief asks the GDD to address. Each is supported by named mechanics or systems in this prototype."),

  h2("Playability"),
  bullet("Every legal action is visualised BEFORE the player commits — move highlights, attack highlights, and enemy telegraphs all show next-step consequences."),
  bullet("No hidden information: enemy plans (move + attack) are always rendered for the round; HP values are visible as bars; trap tile types are distinguishable by sprite."),
  bullet("Controls are minimal (left click, Q, Space) and shown on-screen at all times via the HUD reminder line."),
  bullet("Turn-based pacing — no time pressure. Players can take as long as they need to read the board."),

  h2("Accessibility"),
  bullet("Input scheme reduces to one mouse button and two keys — playable one-handed with no chord requirements."),
  bullet("Highlight colours pass a base contrast check on the purple isometric ground (blue/orange/yellow distinct from base tile colour)."),
  bullet("HP bars use colour AND length encoding (not colour alone) — colour-blind players still parse via fill ratio."),
  bullet("All UI text is high-contrast white on dark backgrounds (HUD overlay, end screens)."),
  bullet("No reflex requirements. No locked-in audio cues required to play."),

  h2("Juiciness"),
  bullet("Hit-burst particles on every damage event (9 procedural sprites that radiate, fade, and shrink — yellow for enemy hits, red for player damage)."),
  bullet("Floating HP bars on enemies that shrink left-to-right and colour-shift green → yellow → red."),
  bullet("Smooth tile-to-tile walking animation replaces instant teleport. Sprites face the direction of movement."),
  bullet("Yellow telegraph tiles PULSE between frames to signal danger."),
  bullet("Highlight overlays now cover only the top diamond face of each cube, not the whole cube — cleaner visual read."),
  bullet("Post-wave heal value displayed on the upgrade screen so the player sees their recovery."),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const interestingChoices = [
  h1("Interesting Choices"),
  p("Every turn surfaces multiple meaningful decisions:"),
  table([2340, 7020], [
    ["Choice point", "What's at stake"],
    ["Melee vs Ranged each turn", "Engage close for higher damage but risk being adjacent to a Heavy next turn, or stay safe at range with lower output."],
    ["Where to move",             "Telegraphed enemy attacks show the danger tiles in yellow. Move into safety, into trap-bait position, or to set up your own attack range."],
    ["Post-wave upgrade",         "+25% melee or +25% ranged. Compounds over 10 waves — committing early matters."],
    ["Companion pick (waves 3/6/9)", "Three picks across a run. Drone = damage; Brawler = HP soak; Trickster = action economy. Permadeath: a lost companion is gone for the run."],
    ["Whether to act this turn",  "Sometimes the best move is no attack — preserving position for the next round's setup."],
  ]),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const story = [
  h1("Story, Setting & Character"),
  h2("Narrative Hook"),
  p("Shoki crash-landed on Earth and government forces have surrounded the wreckage. To escape, he must hold his ground across 10 waves while repairing the ship's drive between rounds. Companions encountered along the way — a survey drone, a mech-suit brawler, an alien trickster — choose to fight alongside him."),
  p("Tone: light pulp sci-fi. Underdog framing — the player is the alien, the antagonists are very-grounded humans. Traps are industrial crash-site hazards (live wires, debris pits, electrical pylons)."),

  h2("Main Character — Shoki"),
  p("Sympathetic protagonist. Average HP (100), versatile (melee or ranged toggle), unremarkable mobility (2 tiles). His advantage is the player's ability to read the board — never raw stats."),

  h2("Why This Narrative Works"),
  bullet("Every mechanic is justified by the fiction (traps = crash debris, waves = converging humans, escape = win condition)"),
  bullet("No cutscenes needed — the 10-wave countdown carries the entire arc"),
  bullet("Companions are characters that justify their stats (Drone = scout, Brawler = mech, Trickster = saboteur)"),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const interface_ = [
  h1("Interface"),
  h2("Visual Gamestate (Lecture Learning)"),
  p("The board communicates every relevant state without text:"),
  table([2340, 7020], [
    ["Visual cue",                "Meaning"],
    ["Blue diamond on tile top",  "Valid move target this turn"],
    ["Orange diamond on tile top","Valid attack target"],
    ["Yellow pulsing diamond",    "Enemy will ATTACK this tile next turn (telegraph)"],
    ["Grey ghost diamond",        "Enemy will MOVE to this tile next turn"],
    ["Floating bar (green→yellow→red)", "HP fraction of an enemy or companion"],
    ["Particle burst (yellow/red)", "Hit landed — yellow when player hits, red when player takes damage"],
    ["Missing ground tile",       "Void — impassable, falls outside the room silhouette"],
  ]),

  h2("HUD"),
  bullet("Top-left: Shoki HP bar and numeric value"),
  bullet("Top-right: Wave counter + companion roster (each with HP)"),
  bullet("Bottom-left: Damage multipliers (melee × ranged ×)"),
  bullet("Bottom-right: Current attack mode (Melee/Ranged) with [Q] hint"),
  bullet("Top-center: Controls reminder + telegraph legend"),

  h2("Controls"),
  table([1500, 7860], [
    ["Input",      "Action"],
    ["Left click", "Move to highlighted tile, then attack on a second click"],
    ["Q",          "Toggle Melee / Ranged attack mode"],
    ["Space",      "End turn (only available after moving)"],
  ]),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const ai = [
  h1("Artificial Intelligence"),

  h2("Enemy AI — Telegraph Architecture"),
  p("At the start of every round, each enemy runs ComputePlan(): pick a target, determine the move destination, determine the attack tile (if in range after the planned move). The plan is rendered as two telegraph overlays on the board — yellow for the attack target, grey for the move target."),
  p("On the enemy's turn they COMMIT to their telegraphed plan. They walk the planned path (smooth lerp per tile) and attack the planned tile even if Shoki has moved. This means a smart player who reads telegraphs can dodge attacks by stepping off the yellow tile."),

  h2("Per-Type AI"),
  table([1800, 7560], [
    ["Type",    "Decision logic"],
    ["Soldier", "Pathfind toward Shoki via BFS. Plan attack if adjacent after move (Chebyshev ≤ 1)."],
    ["Sniper",  "Maintain distance 2–4 (Manhattan). Retreat if too close, approach if too far. Plan ranged attack if in range after move."],
    ["Heavy",   "Same approach as Soldier but moveRange = 1 — slower threat, harder to dodge once close."],
  ]),

  h2("Companion AI"),
  bullet("Drone — pathfind to keep Manhattan distance 2–4 from nearest enemy, ranged attack"),
  bullet("Brawler — pathfind to nearest enemy, melee attack adjacent"),
  bullet("Trickster — executes a Drone-like turn TWICE per round"),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const gameArt = [
  h1("Game Art"),
  h2("Style"),
  p("2D pixel art, low-resolution (16-pixel base unit). Inspired by Shovel Knight (palette + chunky pixel), Into the Breach (telegraph clarity), Mewgenics (creature design). Isometric 2:1 diamond projection with tiles that have a 3D-cube appearance and entity sprites with a bottom-center pivot."),

  h2("Tile Art"),
  spriteImg("ground", 100, 100),
  caption("Ground tile (32×32 px, isometric cube)"),
  spriteImg("ground2", 100, 100),
  caption("Alternate ground tile — used to vary the visual rhythm across rooms"),

  h2("Character Art"),
  spriteImg("shoki", 100, 100),
  caption("Shoki — alien protagonist"),
  spriteImg("drone", 100, 100),
  caption("Drone — ranged glass-cannon companion"),
  spriteImg("sniper", 100, 100),
  caption("Sniper — ranged human enemy"),
  spriteImg("heavy", 100, 100),
  caption("Heavy — tank-class human enemy"),
  p("Soldier, Brawler, and Trickster sprites are planned next; the runtime uses tinted procedural fallback squares until those assets land (grey for Soldier, amber for Brawler, magenta for Trickster)."),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const technology = [
  h1("Technology"),
  h2("Engine & Stack"),
  bullet("Unity 6 (6000.3.2f1) — Universal Render Pipeline (2D)"),
  bullet("C# scripting only — no visual scripting or external state machines"),
  bullet("OnGUI-based HUD for prototype speed (no Canvas/TMP wiring overhead)"),
  bullet("Procedural placeholder graphics where Aseprite sprites haven't landed yet"),
  bullet("Aseprite for sprite creation; PNGs exported into Assets/Resources/{Tiles,Entities}/"),

  h2("Key Systems"),
  table([2340, 7020], [
    ["System",         "Responsibility"],
    ["GridManager",    "Grid state, isometric projection, BFS pathfinding, tile-type rendering, highlight management"],
    ["TurnManager",    "Initiative roll, turn execution order, end-of-round transitions"],
    ["TelegraphSystem","Per-round enemy plan computation; yellow / grey telegraph overlays"],
    ["RoomGenerator",  "Per-wave layout: voids, obstacles, traps, spawn pool"],
    ["EnemySpawner",   "Wave-driven enemy/companion instantiation"],
    ["CombatSystem",   "Melee/ranged damage resolution, hit-burst particle invocation"],
    ["TrapSystem",     "Trap-effect resolution on entity arrival"],
    ["UpgradeManager", "Damage multiplier persistence across waves"],
    ["GameManager",    "Wave progression, state machine, win/loss conditions"],
  ]),

  h2("Design Patterns Used"),
  bullet("Singleton — every manager exposes a static Instance; matches the always-one-instance contract"),
  bullet("Observer — TurnManager.OnTurnStart, Entity.OnHPChanged, GameManager.OnWaveCleared events"),
  bullet("State machine — GameManager.State enum gates UI rendering and input routing"),
  bullet("Component composition — HpBar, HitParticle attach to entities without inheritance"),
  bullet("Coroutines for animation — WalkToTileSmooth, ExecuteTurn yield-based sequencing"),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const playtestLog = [
  h1("Playtest Feedback Log"),
  p("The DDG brief explicitly requires playtest feedback and development decisions to be documented. The table below captures the observations from each playtest cycle that drove the iterations on the next page."),
  table([1100, 3500, 4760], [
    ["Cycle",            "Observation / Feedback",                                                         "Decision"],
    ["Concept review",   "30×30 top-down grid was too large to read at a glance; every move felt low-stakes.", "→ Iteration 1: pivot to isometric 8×8, 2-tile cardinal movement."],
    ["Design review",    "Pokémon-type counters added a memorisation tax without making the puzzle harder. Players asked \"why does fire beat grass here?\".", "→ Iteration 2: drop type counters; add Companion system and enemy telegraphs."],
    ["Playtest 1",       "\"I died on wave 2 every time.\" — every test run ended by wave 3 because the worst-case round dealt 63 damage.", "→ Iteration 3: enemy damage cut ~45%; post-wave heal raised 50%→75%."],
    ["Playtest 2",       "\"All the rooms look the same.\" — players reported fatigue after the 4th wave because every map was a clean 8×8.", "→ Iteration 4: per-wave Void tile carving — corner chips and interior holes."],
    ["Playtest 3",       "\"I can't tell which tiles I can actually attack.\" — attack highlights tinted entire cubes so adjacent cube sides bled into the highlighted area.", "→ Iteration 5 (part 1): top-face-only highlights using the flat procedural diamond."],
    ["Playtest 3",       "\"How much HP does this guy have?\" — enemies showed no per-entity health, only a global HUD HP bar for Shoki.", "→ Iteration 5 (part 2): floating HP bars on enemies and companions."],
    ["Playtest 3",       "\"Hits feel weightless.\" — damage events showed only an HP number change, no visual confirmation.", "→ Iteration 5 (part 3): procedural hit-burst particles (yellow for hits dealt, red for hits taken)."],
    ["Playtest 4",       "\"I can't see Shoki move — he just teleports.\" — instant MoveTo made trap timing confusing and broke animation prep.", "→ Iteration 6: smooth coroutine-driven path walking for player, enemies, and companions."],
    ["Stability test",   "\"Clicked a diagonal blue tile, Shoki stopped one step short.\" — Chebyshev highlight box included tiles BFS couldn't reach in moveRange steps.", "→ Iteration 7 (part 1): replace highlight box with BFS GetReachableTiles."],
    ["Stability test",   "\"The game froze when an enemy walked into a pit.\" — TurnManager's _currentIndex desynced when an entity was removed from _turnOrder mid-turn.", "→ Iteration 7 (part 2): UnregisterEntity now decrements _currentIndex correctly."],
  ]),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const iterationHistory = [
  h1("Iteration History"),
  p("The DDG rubric asks for documented iteration cycles with the reasoning behind each change. Every iteration below was driven by a specific observation — concept review, playtest, or stability incident — and is reflected in the codebase."),

  h2("Iteration 1 — Concept Pivot: Top-down 30×30 → Isometric 8×8"),
  rich({ text: "Trigger: ", bold: true }, "Early concept review."),
  rich({ text: "Change: ", bold: true }, "Switched view from top-down to isometric. Reduced the playfield from a 30×30 grid with 3×3 chunk movement to an 8×8 isometric grid with 2-tile cardinal movement."),
  rich({ text: "Why: ", bold: true }, "30×30 was too large to read at a glance. The compact 8×8 forces every move to matter and lets the entire battlefield fit on-screen. Isometric perspective gave more visual depth without losing tactical clarity."),
  rich({ text: "Impact: ", bold: true }, "Required rewriting all spatial systems — coordinate projection, sort order, click-to-grid. Worth it for the tightness of the resulting decisions."),

  h2("Iteration 2 — Dropped Pokémon-Type Counters; Added Companions + Telegraphs"),
  rich({ text: "Trigger: ", bold: true }, "Internal design review."),
  rich({ text: "Change: ", bold: true }, "Removed a planned Pokémon-style type-counter system. Added a 3-companion roster (Drone, Brawler, Trickster) chosen at waves 3, 6, and 9. Adopted Into the Breach-style enemy telegraphs."),
  rich({ text: "Why: ", bold: true }, "Type counters added a memorization tax without improving the core puzzle. Companions add strategic depth via the permadeath stake. Telegraphs eliminate hidden information so the player can outplay rather than guess — directly aligned with the LeBlanc Challenge fun type."),

  h2("Iteration 3 — Difficulty Rebalance"),
  rich({ text: "Trigger: ", bold: true }, "First playtest sprint."),
  rich({ text: "Change: ", bold: true }, "Soldier melee 15→8, Sniper ranged 8→5, Heavy melee 25→12. Post-wave heal raised from 50%→75% of damage taken."),
  rich({ text: "Why: ", bold: true }, "Math check on wave 6 worst-case round: 2 Soldiers + Sniper + Heavy = 15+15+8+25 = 63 damage in a single round — lethal in 2 rounds at 100 HP. New numbers cap worst case at 8+8+5+12 = 33, recoverable thanks to the bumped heal. Loss rate dropped from \"every run\" to roughly 1-in-3 in subsequent playtests."),

  h2("Iteration 4 — Grid Silhouette Variation (Voids)"),
  rich({ text: "Trigger: ", bold: true }, "Playtest fatigue — every wave looked the same."),
  rich({ text: "Change: ", bold: true }, "Added a Void tile type that renders nothing and blocks pathfinding. RoomGenerator.CarveVoids() chips small clusters from each corner (2–8 tiles total) and drops 0–2 interior holes per wave."),
  rich({ text: "Why: ", bold: true }, "Static rectangles felt repetitive across 10 waves. Varied silhouettes change tactical character per wave (a chipped corner removes spawn options; an interior hole reshapes the safest paths). Adds visual identity without new art."),

  h2("Iteration 5 — Visual Juiciness Pass"),
  rich({ text: "Trigger: ", bold: true }, "Playtest feedback: \"I can't tell which tiles are valid attack targets.\""),
  rich({ text: "Change: ", bold: true }, "Three additions:"),
  bullet("Highlight overlays use a flat diamond covering only the tile's top face — not the full cube — so the attack target no longer bleeds onto neighbouring cube sides."),
  bullet("Floating HP bars above every enemy and companion. Width shrinks left-to-right with HP, colour transitions green → yellow → red at 60% / 30%."),
  bullet("Hit-burst particles on every damage event: 9 procedural sprites that radiate, fade, and shrink. Yellow when an enemy is hit (positive feedback); red when Shoki is hit."),
  rich({ text: "Why: ", bold: true }, "Directly addresses the Playability / Accessibility / Juiciness DDG requirement. Combat reads as combat now — every action has a visual reaction."),

  h2("Iteration 6 — Smooth Path-Walking Movement"),
  rich({ text: "Trigger: ", bold: true }, "Animation prep + trap-timing readability."),
  rich({ text: "Change: ", bold: true }, "Replaced instant MoveTo with a coroutine-driven WalkPath. The walker walks the BFS path tile-by-tile, lerping position over ~0.16 seconds per step. Sprite flips at the start of each step to face the destination."),
  rich({ text: "Why: ", bold: true }, "Teleport movement was unreadable and broke trap timing — the player was \"already there\" before the eye could track the move. Walking creates a natural hook for sprite-animator integration and makes traps fire on visible arrival, which reads correctly. Same coroutine is now used by EnemyAI and CompanionAI for consistency."),

  h2("Iteration 7 — Stability & Reachability Fixes"),
  rich({ text: "Trigger: ", bold: true }, "Stability playtest revealed two bugs."),
  rich({ text: "Change A — move highlights: ", bold: true }, "ShowMoveRange previously highlighted tiles in a Chebyshev box (5×5 around Shoki for moveRange = 2), but pathfinding is cardinal-only — diagonal corner tiles need 3 BFS steps, so the walker would stop short on them. Replaced the box check with GridManager.GetReachableTiles, a depth-limited BFS that returns only what the walker can actually reach in N cardinal steps."),
  rich({ text: "Change B — turn-order desync: ", bold: true }, "TurnManager.UnregisterEntity removed the dead entity from _turnOrder but didn't adjust _currentIndex for the list shift. When an enemy died during their own turn (typically walking into a pit), the next AdvanceTurn would either skip an entity or walk off the list and wedge the loop. Fixed by decrementing _currentIndex when the removed entity sat at or before the current slot, plus a guard in ProcessCurrentTurn for the briefly-negative index."),
  rich({ text: "Why: ", bold: true }, "Both bugs were found by the kind of playtesting the DDG rubric explicitly wants documented. Fixing them cleanly was straightforward once each was traced — and they're now noted in code comments so they don't regress."),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const lectureLearnings = [
  h1("Lecture Learnings — Coverage Checklist"),
  p("The DDG brief requires the GDD to demonstrate at least 5 of the 9 lecture learnings. This GDD addresses 8."),
  table([4680, 4680], [
    ["Learning",                                "Where it lives in this GDD"],
    ["Core loop diagram (micro/macro/meta)",    "Gameplay & Mechanics → Figure 1 (visual diagram)"],
    ["Intended fun & engagement",               "Game Overview → LeBlanc Fun Type"],
    ["GDD (mandatory)",                         "This entire document"],
    ["Multiple prototype iterations",           "Playtest Feedback Log + Iteration History (7 entries)"],
    ["Interesting choices & their evolution",   "Interesting Choices + Iteration 2 (companion system)"],
    ["Balancing work",                          "Iteration 3 + Wave Progression Curve table"],
    ["Randomness & changes",                    "Randomness & Balancing section"],
    ["Narrative aspects",                       "Story, Setting & Character"],
    ["Visual gamestate",                        "Interface → Visual Gamestate + Playability/Accessibility/Juiciness section"],
    ["Design patterns",                         "Technology → Design Patterns Used"],
  ]),
  p("The one item not explicitly covered as a standalone section is \"paper prototype iterations\" — the project pivoted directly to a digital prototype because of the Unity-first nature of the brief. Iteration 1 (concept pivot) and Iteration 2 (system swap) are documented as digital design iterations that serve the same purpose."),

  new Paragraph({ spacing: { before: 200, after: 0 }, children: [] }),
];

const asset_list = [
  h1("Appendix — Asset List"),
  h2("Art"),
  table([3120, 2340, 3900], [
    ["Asset",              "Status",   "Notes"],
    ["shoki.png",          "Done",     "32×32, bottom-center pivot"],
    ["drone.png",          "Done",     "Cyan glass-cannon companion"],
    ["sniper.png",         "Done",     "Camo-green enemy"],
    ["heavy.png",          "Done",     "Navy steel enemy"],
    ["soldier.png",        "Planned",  "Grey gunmetal grunt — fallback square in current build"],
    ["brawler.png",        "Planned",  "Amber mech-suit companion"],
    ["trickster.png",      "Planned",  "Magenta saboteur companion"],
    ["ground.png",         "Done",     "Tan isometric cube tile"],
    ["ground_alt.png",     "Done",     "Purple variant for visual rhythm"],
    ["VFX / animations",   "Planned",  "Sprite animations to hook into WalkToTileSmooth coroutine"],
  ]),

  rich({ text: "Sound: ", bold: true }, "Out of scope within the Week 6 prototype timeframe. Visual juiciness substitutes (particles, HP bars, highlights, animation) carry the feedback load."),
];

// ── Build document ───────────────────────────────────────────────────────────
const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 19 } } },  // 9.5pt body — fits 12-page DDG limit
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 30, bold: true, font: "Arial", color: "1A1A1A" },
        paragraph: { spacing: { before: 240, after: 120 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font: "Arial", color: "262626" },
        paragraph: { spacing: { before: 160, after: 80 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 22, bold: true, font: "Arial", color: "404040" },
        paragraph: { spacing: { before: 120, after: 60 }, outlineLevel: 2 } },
    ],
  },
  numbering: {
    config: [{
      reference: "bullets",
      levels: [{
        level: 0, format: LevelFormat.BULLET, text: "•",
        alignment: AlignmentType.LEFT,
        style: { paragraph: { indent: { left: 720, hanging: 360 } } },
      }],
    }],
  },
  sections: [{
    properties: {
      page: {
        size: { width: 12240, height: 15840 },
        margin: { top: 1080, right: 1080, bottom: 1080, left: 1080 },  // 0.75" margins
      },
    },
    children: [
      ...coverPage,
      ...versionHistory,
      ...gameOverview,
      ...gameplay,
      ...interactiveElements,
      ...interestingChoices,
      ...paj,
      ...story,
      ...interface_,
      ...ai,
      ...gameArt,
      ...technology,
      ...playtestLog,
      ...iterationHistory,
      ...lectureLearnings,
      ...asset_list,
    ],
  }],
});

Packer.toBuffer(doc).then(buffer => {
  const out = "/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/GDD/Shokis_Adventure_GDD.docx";
  fs.writeFileSync(out, buffer);
  console.log("Wrote:", out, `(${buffer.length} bytes)`);
});
