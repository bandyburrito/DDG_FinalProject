#!/usr/bin/env python3
"""
Inject our GDD prose into a copy of the HSLU template.

Strategy: find each known section heading in document.xml by its text content,
then insert one or more body paragraphs right after that heading's closing </w:p>.
This preserves the template's exact styles, headers, footers, page layout, and TOC.

For sections we don't populate, we leave the empty heading as-is — the DDG brief
explicitly allows omitting parts of the template if it's reasoned (we add a brief
"out of scope" note to make that explicit where useful).
"""
import re
import sys
import shutil

UNPACKED = "/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/GDD/template_unpacked"
DOC_XML  = f"{UNPACKED}/word/document.xml"

# ── Paragraph builders ──────────────────────────────────────────────────────

def p_body(text, bold=False, italics=False):
    """Normal body paragraph."""
    rpr = ""
    if bold:    rpr += "<w:b/>"
    if italics: rpr += "<w:i/>"
    rpr_xml = f"<w:rPr>{rpr}</w:rPr>" if rpr else ""
    text_esc = (text.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;"))
    return f'<w:p><w:r>{rpr_xml}<w:t xml:space="preserve">{text_esc}</w:t></w:r></w:p>'

def p_rich(*parts):
    """Mixed-formatting paragraph. Each part is a string or (text,) or (text, opts)."""
    runs = []
    for part in parts:
        if isinstance(part, str):
            text, opts = part, {}
        elif isinstance(part, tuple) and len(part) == 1:
            text, opts = part[0], {}
        else:
            text, opts = part[0], part[1]
        rpr = ""
        if opts.get("bold"):    rpr += "<w:b/>"
        if opts.get("italics"): rpr += "<w:i/>"
        rpr_xml = f"<w:rPr>{rpr}</w:rPr>" if rpr else ""
        text_esc = (text.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;"))
        runs.append(f'<w:r>{rpr_xml}<w:t xml:space="preserve">{text_esc}</w:t></w:r>')
    return f'<w:p>{"".join(runs)}</w:p>'

def p_bullet(text):
    """Bullet via leading character (template lacks a bullet numbering def)."""
    text_esc = (text.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;"))
    return (f'<w:p><w:pPr><w:ind w:left="567" w:hanging="283"/></w:pPr>'
            f'<w:r><w:t xml:space="preserve">•  {text_esc}</w:t></w:r></w:p>')

def p_oos(reason):
    """Out-of-scope marker."""
    return p_rich(("Out of scope for the Week 6 prototype — ", {"italics": True}),
                  (reason, {"italics": True}))

# Simple bordered table builder. cols = list of widths in DXA; rows = list of lists.
def make_table(col_widths, rows):
    total = sum(col_widths)
    tbl_pr = (
        f'<w:tblPr><w:tblW w:w="{total}" w:type="dxa"/>'
        f'<w:tblBorders>'
        f'<w:top w:val="single" w:sz="4" w:color="999999"/>'
        f'<w:left w:val="single" w:sz="4" w:color="999999"/>'
        f'<w:bottom w:val="single" w:sz="4" w:color="999999"/>'
        f'<w:right w:val="single" w:sz="4" w:color="999999"/>'
        f'<w:insideH w:val="single" w:sz="4" w:color="CCCCCC"/>'
        f'<w:insideV w:val="single" w:sz="4" w:color="CCCCCC"/>'
        f'</w:tblBorders></w:tblPr>'
    )
    grid = "<w:tblGrid>" + "".join(f'<w:gridCol w:w="{w}"/>' for w in col_widths) + "</w:tblGrid>"
    rows_xml = []
    for ri, row in enumerate(rows):
        cells_xml = []
        for ci, txt in enumerate(row):
            txt_esc = (txt.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;"))
            shade = '<w:shd w:val="clear" w:color="auto" w:fill="EEEEEE"/>' if ri == 0 else ""
            rpr = "<w:rPr><w:b/></w:rPr>" if ri == 0 else ""
            cells_xml.append(
                f'<w:tc><w:tcPr><w:tcW w:w="{col_widths[ci]}" w:type="dxa"/>{shade}</w:tcPr>'
                f'<w:p><w:r>{rpr}<w:t xml:space="preserve">{txt_esc}</w:t></w:r></w:p></w:tc>'
            )
        rows_xml.append(f'<w:tr>{"".join(cells_xml)}</w:tr>')
    return f'<w:tbl>{tbl_pr}{grid}{"".join(rows_xml)}</w:tbl>'

# ── Content blocks per heading ──────────────────────────────────────────────

CONTENT = {

# ── Game Overview ────────────────────────────────────────────────────────────
"Game Concept": [
    p_body("Shoki is an alien whose spacecraft has crash-landed on Earth. Government human forces are closing in to capture him. The player must survive 10 escalating waves of human soldiers on an 8×8 isometric grid, repair the ship between rounds, and escape home."),
    p_body("The game blends the readable telegraph system of Into the Breach with the build-craft progression of a roguelite. Every turn is a small puzzle: where to move, melee or ranged, bait into a trap or avoid it, save HP for later."),
],
"Feature Set": [
    p_bullet("Turn-based tactical combat on an isometric 8×8 grid"),
    p_bullet("Initiative-based turn order (d20 + speed)"),
    p_bullet("3 enemy archetypes — Soldier (melee grunt), Sniper (ranged), Heavy (high-HP melee)"),
    p_bullet("3 recruitable companions — Drone (glass cannon), Brawler (tank), Trickster (acts twice)"),
    p_bullet("Enemy telegraph system — every attack and move shown one turn in advance"),
    p_bullet("Environmental traps that affect both player and enemies"),
    p_bullet("Permadeath companions + damage-multiplier upgrades stacked across the run"),
    p_bullet("Procedural room layout regenerated each wave"),
],
"Genre": [p_body("Turn-based tactical roguelite.")],
"Target Audience": [p_body("Players who enjoy Into the Breach, Mewgenics, FTL, and Shovel Knight — readable systems, high replayability, short runs (~20 minutes).")],
"Game Flow": [
    p_rich(("Micro (one turn): ", {"bold": True}), ("Move ≤ moveRange tiles → trap check on arrival → optional attack → end turn → enemies execute telegraphed plans.",)),
    p_rich(("Macro (one wave): ", {"bold": True}), ("Survive all enemies → 75% of wave damage heals → +25% melee or ranged upgrade → at waves 3, 6, 9 choose a companion → new room generates.",)),
    p_rich(("Meta (one run): ", {"bold": True}), ("Survive all 10 waves. HP carries across waves; damage multipliers compound; companion roster grows.",)),
],
"Look and Feel": [
    p_body("2D pixel art, low-resolution (16-pixel base unit). Inspired by Shovel Knight (palette + chunky pixels), Into the Breach (telegraph clarity), Mewgenics (creature design). Isometric 2:1 diamond projection with tiles that have a 3D-cube appearance."),
    p_rich(("LeBlanc fun type — Challenge: ", {"bold": True}), ("every turn is a readable but non-trivial decision. No hidden information; outcome rests on the player reading the board correctly.",)),
],
"Number of locations": [p_body("One — a single crash-site room, procedurally regenerated each wave with new void cluster shapes, obstacles, traps, and spawn points.")],
"Number of levels": [p_body("10 waves. Each wave is a complete combat encounter; the room regenerates between waves.")],
"Number of NPC’s": [p_body("3 enemy archetypes (Soldier, Sniper, Heavy) and 3 companion archetypes (Drone, Brawler, Trickster) — 6 distinct AI behaviours.")],
"Number of weapons": [p_body("Two attack modes per character (melee and ranged) toggleable each turn. Damage values are character-specific and modified by per-run +25% upgrades.")],

# ── Gameplay and Mechanics ───────────────────────────────────────────────────
"Gameplay": [p_body("Each turn the player moves up to moveRange tiles, may trigger a trap on arrival, and chooses one attack (melee adjacent or ranged 2–4 tiles). Enemies acted in initiative order, committing to their telegraphed plans (yellow tile = attack target, grey = move target). The player can outplay the AI by reading the telegraphs and stepping off attack tiles or baiting enemies onto traps.")],
"Game Progression": [
    p_body("Difficulty scales over 10 waves through enemy composition. Soldier counts grow modestly; Sniper introduced at wave 4, Heavy at wave 6. HP carries across waves with a 75% post-wave heal. Damage multipliers from upgrades compound for the full run."),
    make_table([800, 1100, 1100, 1100, 4000], [
        ["Wave", "Soldiers", "Snipers", "Heavies", "Notes"],
        ["1",  "2", "0", "0", "Onboarding — pure melee rush"],
        ["2",  "2", "0", "0", "Same composition, learning curve"],
        ["3",  "3", "0", "0", "First Companion pick"],
        ["4",  "2", "1", "0", "Sniper introduced — ranged threat"],
        ["5",  "2", "1", "0", "Practice mixed composition"],
        ["6",  "2", "1", "1", "Heavy introduced + second Companion pick"],
        ["7",  "2", "2", "0", "Double sniper — positioning matters"],
        ["8",  "1", "2", "1", "Hardest single composition"],
        ["9",  "2", "1", "1", "Third Companion pick"],
        ["10", "2", "1", "2", "Boss-feel finale — two heavies"],
    ]),
],
"Mission/challenge Structure": [p_body("Each wave is a self-contained combat encounter. Win condition: defeat all enemies on the board. There are no sub-objectives — just survive.")],
"Puzzle Structure": [p_body("Each turn is a micro-puzzle in itself: the telegraph system shows next-turn enemy actions, so the player solves \"where can I stand to avoid damage AND get an attack off?\" That's the core minute-to-minute puzzle.")],
"Objectives": [
    p_rich(("Win condition: ", {"bold": True}), ("Survive all 10 waves.",)),
    p_rich(("Lose condition: ", {"bold": True}), ("Shoki's HP reaches 0 (companion losses do not end the run).",)),
],
"Play Flow": [
    p_body("MainMenu → Combat (wave 1) → UpgradeScreen → [CompanionScreen at waves 3/6/9] → Combat (wave N+1) → ... → Win or GameOver. Restart returns to MainMenu."),
],
"Mechanics": [p_body("Every mechanic listed below maps to a class in the codebase (GridManager, TurnManager, CombatSystem, TelegraphSystem, TrapSystem, etc.) — see the Technology section for the system overview.")],
"Physics": [p_body("None — grid-based discrete movement. No simulated physics, no momentum, no continuous collision. Tile occupancy is binary.")],
"Movement": [p_body("Cardinal-only pathfinding via 4-direction BFS. Player moveRange = 2 tiles. Move highlights show only tiles reachable in those steps via the same BFS — so highlights always match what the walker can actually reach. Movement plays as a smooth ~0.16-second tile-to-tile lerp; traps fire on visual arrival.")],
"Objects": [
    p_rich(("Spike trap: ", {"bold": True}), ("10 damage on entry, then consumed (tile reverts to ground).",)),
    p_rich(("Pit trap: ", {"bold": True}), ("Instant kill (9999 damage); does not consume.",)),
    p_rich(("Slow zone: ", {"bold": True}), ("Applies −1 moveRange for one turn; does not consume.",)),
    p_rich(("Obstacles: ", {"bold": True}), ("Block movement and pathfinding. Do not damage.",)),
    p_rich(("Void tiles: ", {"bold": True}), ("Missing ground (Iteration 4) — block pathfinding and render as nothing. Used to chip corners and drop interior holes.",)),
],
"Actions": [
    p_rich(("Melee attack: ", {"bold": True}), ("Hits all 8 adjacent tiles. Higher damage. Forces close engagement.",)),
    p_rich(("Ranged attack: ", {"bold": True}), ("Single target at Chebyshev distance 2–4. Lower damage. Safer positioning.",)),
    p_rich(("Mode toggle (Q): ", {"bold": True}), ("Switches between melee and ranged. Each mode upgrades independently.",)),
    p_rich(("End turn (Space): ", {"bold": True}), ("Only available after moving — prevents accidental skipped turns.",)),
],
"Combat": [
    p_body("Two interactive elements interact here — see the dedicated section below. Enemy stats:"),
    make_table([1500, 900, 900, 1200, 1400, 3460], [
        ["Enemy",   "HP", "Speed", "MoveRange", "Damage", "Behaviour"],
        ["Soldier", "30", "1", "2", "8 melee",    "Path toward Shoki, attack adjacent"],
        ["Sniper",  "20", "2", "2", "5 ranged",   "Keep 2–4 tiles from Shoki"],
        ["Heavy",   "60", "0", "1", "12 melee",   "Slow approach, heavy hit"],
    ]),
    p_body("Companion stats:"),
    make_table([1500, 900, 900, 1200, 1400, 3460], [
        ["Companion", "HP", "Speed", "MoveRange", "Damage", "Niche"],
        ["Drone",     "20", "5", "3", "6 ranged",  "Glass cannon — keeps 2–4 tile range"],
        ["Brawler",   "40", "1", "2", "12 melee",  "Tank — soaks hits"],
        ["Trickster", "25", "6", "3", "5 ranged",  "Acts twice per turn"],
    ]),
],
"Economy": [p_body("No in-game currency. Progression is gated by post-wave upgrade choices (+25% melee OR +25% ranged) and companion recruitment at waves 3, 6, and 9. \"Spending\" is a one-way choice with no recovery.")],

# ── Screen Flow ──────────────────────────────────────────────────────────────
"Screen Descriptions": [
    p_bullet("Main Menu — single \"Start\" button; minimal."),
    p_bullet("Combat — main gameplay state; HUD overlays board."),
    p_bullet("Upgrade Screen — post-wave; +25% melee or +25% ranged choice; shows heal amount."),
    p_bullet("Companion Screen — at waves 3/6/9; three-card pick (Drone/Brawler/Trickster)."),
    p_bullet("Win / GameOver — restart-only end screens."),
],
"Game Options": [p_oos("no settings menu in the prototype; controls are fixed (left click + Q + Space).")],
"Replaying and Saving": [p_oos("no save system; runs are session-only. Replayability comes from randomised room layouts, varied enemy compositions, and companion-pick branches.")],
"Cheats and Easter Eggs": [p_oos("none planned.")],

# ── Story ────────────────────────────────────────────────────────────────────
"Story and Narrative": [p_body("Shoki crash-landed on Earth and government forces have surrounded the wreckage. To escape, he must hold his ground across 10 waves while repairing the ship's drive between rounds. Light pulp sci-fi tone; underdog framing — the player is the alien, the antagonists are very-grounded humans.")],
"Back story": [p_body("Shoki is a routine surveyor from a peaceful civilisation. A meteoroid collision forced an emergency landing on Earth — drawing the attention of a government rapid-response unit that has standing orders to capture any non-terrestrial life. The 10 waves represent successive deployments converging on the crash site as Shoki repairs his drive.")],
"Plot Elements": [
    p_bullet("Inciting incident: the crash (begins before play)"),
    p_bullet("Rising action: waves 1–6 escalate; companions join at 3 and 6"),
    p_bullet("Climax: wave 9 introduces the third companion pick and the hardest pre-finale composition"),
    p_bullet("Resolution: wave 10 finale; on win, Shoki escapes; on loss, captured"),
],
"Game Progression__story": [p_body("Reused from Gameplay section — the 10-wave structure carries the entire arc, no cutscenes required.")],
"License Considerations": [p_oos("student project; no third-party licensed content used. Sprites are original Aseprite work.")],
"Cut Scenes": [p_oos("none — the 10-wave countdown carries the entire narrative arc.")],

# ── Game World ───────────────────────────────────────────────────────────────
"General look and feel of world": [p_body("Industrial crash-site debris on an open Earth plain. Tile palette uses warm tans (default ground) and cool purples (alternate) for visual rhythm. Traps are framed as crash-site hazards: live wires (spike), debris pits (pit), spilled coolant (slow zone).")],
"Area #1": [p_body("The crash site — the only area in the game. An 8×8 isometric room with procedurally varied silhouettes (corner chips, interior holes from Iteration 4) and randomized trap/obstacle placement.")],

# ── Characters ───────────────────────────────────────────────────────────────
"Character #1": [
    p_rich(("Shoki — the alien protagonist. ", {"bold": True}),
           ("HP 100, moveRange 2, melee 15 / ranged 8 base damage. Versatile (melee/ranged toggle), unremarkable mobility. His advantage is the player's ability to read the board — never raw stats.",)),
],

# ── Interface ────────────────────────────────────────────────────────────────
"Visual System": [
    p_body("The board communicates every relevant state without text. The visual gamestate decoder:"),
    make_table([2340, 7020], [
        ["Visual cue",                "Meaning"],
        ["Blue diamond on tile top",  "Valid move target this turn"],
        ["Orange diamond on tile top","Valid attack target"],
        ["Yellow pulsing diamond",    "Enemy will attack this tile next turn"],
        ["Grey ghost diamond",        "Enemy will move to this tile next turn"],
        ["Floating bar (green→red)",  "HP fraction of an enemy or companion"],
        ["Particle burst",            "Hit landed — yellow = player attacks; red = player takes damage"],
        ["Missing ground tile",       "Void — impassable, falls outside the room silhouette"],
    ]),
],
"HUD - What controls": [
    p_bullet("Top-left: Shoki HP bar and numeric value"),
    p_bullet("Top-right: Wave counter + companion roster (each with HP)"),
    p_bullet("Bottom-left: Damage multipliers (melee × ranged ×)"),
    p_bullet("Bottom-right: Current attack mode (Melee/Ranged) with [Q] hint"),
    p_bullet("Top-center: Controls reminder + telegraph legend"),
],
"Menus": [p_body("Minimal — Main Menu (single Start), Upgrade Screen (2 cards), Companion Screen (3 cards), Win/GameOver (restart). All built via OnGUI for prototype speed.")],
"Rendering System": [p_body("Unity Universal Render Pipeline (URP), 2D renderer. Sprites are point-filtered. Sort order is Y-based with explicit ranges: tiles −100..−1, highlights −50, entities 100+.")],
"Camera": [p_body("Single orthographic camera positioned at (0, 4, −10), orthographicSize = 6. Fixed framing of the 8×8 grid; no panning or zoom. Background colour is a deep purple to match the sci-fi tone.")],
"Lighting Models": [p_oos("no lighting — sprites use their baked-in colour.")],
"Control System": [
    make_table([1500, 7860], [
        ["Input",      "Action"],
        ["Left click", "Move to highlighted tile, then attack on a second click"],
        ["Q",          "Toggle Melee / Ranged attack mode"],
        ["Space",      "End turn (only available after moving)"],
    ]),
],

# ── Audio ────────────────────────────────────────────────────────────────────
"Music":        [p_oos("audio is out of scope for the Week 6 prototype.")],
"Sound Effects":[p_oos("planned post-prototype. Visual juiciness (hit-burst particles, HP bars, telegraph pulses) substitutes for the feedback load.")],
"Help System":  [p_body("On-screen HUD reminder always shows current controls. The telegraph legend appears top-centre explaining the yellow tile meaning. No tutorial; first wave is the onboarding (2 Soldiers, no traps elsewhere).")],

# ── AI ───────────────────────────────────────────────────────────────────────
"Opponent AI": [p_body("At the start of every round each enemy runs ComputePlan() — pick the nearest player-aligned target, determine move destination, determine attack tile (if in range after the planned move). The plan is rendered as telegraph overlays (yellow for attack target, grey for move target). On the enemy's turn they COMMIT to the telegraphed plan, even if Shoki has moved. This gives the player a fair information advantage they can act on.")],
"Enemy AI – Villains and Monsters": [
    make_table([1800, 7560], [
        ["Type",    "Decision logic"],
        ["Soldier", "Pathfind toward Shoki via BFS. Plan attack if adjacent after move (Chebyshev ≤ 1)."],
        ["Sniper",  "Maintain Manhattan distance 2–4. Retreat if too close, approach if too far. Plan ranged attack if in range after move."],
        ["Heavy",   "Same as Soldier but moveRange = 1 — slower threat, harder to dodge once close."],
    ]),
],
"Non-combat Characters": [p_oos("none — every entity is either Shoki, an enemy, or a companion. All combatants.")],
"Friendly Characters": [
    p_bullet("Drone — pathfind to keep Manhattan distance 2–4 from nearest enemy, ranged attack"),
    p_bullet("Brawler — pathfind to nearest enemy, melee attack adjacent"),
    p_bullet("Trickster — executes a Drone-like turn TWICE per round"),
],
"Support AI": [p_body("Companion AI runs the same plan/execute pattern as enemies but targets enemies instead of Shoki. Companions are persisted across waves via DontDestroyOnLoad and re-registered on each new room.")],
"Player and Collision Detection": [p_body("Tile-based — no continuous collision. Entity.MoveTo() sets occupancy on arrival and invokes TrapSystem.ResolveTrap. Two entities cannot occupy the same tile; BFS pathfinding respects occupancy.")],
"Pathfinding": [p_body("4-direction BFS in GridManager.FindPath. Treats Obstacle and Void tiles as blocked. GridManager.GetReachableTiles uses depth-limited BFS for the player's move-highlight system so the highlights match actual reachability (Iteration 7).")],

# ── Technology ───────────────────────────────────────────────────────────────
"Target Hardware": [p_body("Desktop — Linux (primary development target) and Windows. Mouse + keyboard input; no controller support in the prototype.")],
"Development hardware and software": [
    p_bullet("Unity 6 (6000.3.2f1) — IDE and runtime"),
    p_bullet("VS Code / Rider — C# editing"),
    p_bullet("Aseprite — sprite authoring"),
    p_bullet("Git — version control"),
],
"Game Engine": [p_body("Unity 6 with Universal Render Pipeline (URP) configured for 2D. No third-party engine plugins. Procedural placeholder graphics generate missing sprites at runtime (Aseprite PNGs override them when available).")],
"Network": [p_body("Single-player only. No networking; no multiplayer planned for the prototype.")],
"Scripting Language": [p_body("C# only. No visual scripting, no external state machines. Coroutines drive turn execution and animation.")],

# ── Game Art ─────────────────────────────────────────────────────────────────
"Concept Art": [p_body("Concept passes are integrated directly into Aseprite — there is no separate concept-art stage. Sprite iterations live as Aseprite layers/versions on disk.")],
"Style Guides": [p_body("16-pixel base unit, low palette count, no anti-aliasing, no outlines except for cube tile depth. Iso projection is 2:1 (diamond width:height). Sprite pivots: tiles at (0.5, 0.75) so the diamond top aligns with the grid position; entities at (0.5, 0) so they stand on the tile.")],
"Characters__art": [p_body("Done: shoki.png, drone.png, sniper.png, heavy.png. Planned: soldier.png, brawler.png, trickster.png (current build uses tinted procedural squares as fallbacks).")],
"Environments": [p_body("Two tile sprites in rotation: ground.png (tan) and ground_alt.png (purple). RoomGenerator carves void tiles per wave for silhouette variation.")],
"Equipment": [p_body("None — combat actions are character-intrinsic. No weapon items to pick up.")],

# ── Management ───────────────────────────────────────────────────────────────
"Detailed Schedule": [
    p_bullet("Weeks 1–2: Concept lock + GDD first draft (Iteration 1: top-down → isometric pivot)"),
    p_bullet("Week 3: Core systems — grid, turns, BFS pathfinding, traps (Iteration 2: telegraphs + companion design)"),
    p_bullet("Week 4: Enemy AI, companion AI, combat resolution (Iteration 3: damage rebalance)"),
    p_bullet("Week 5: Visual juiciness — HP bars, hit particles, smooth walking (Iterations 4–6)"),
    p_bullet("Week 6: Stability + intermediate playtest + presentation (Iteration 7)"),
],
"Budget":            [p_oos("educational project; no monetary budget. All tools are free or already owned.")],
"Risk Analysis": [
    p_bullet("Scope creep — mitigated by hard 10-wave cap and \"out of scope\" notes on optional template sections"),
    p_bullet("Art bottleneck — addressed by procedural fallback sprites so gameplay can be tuned without finished art"),
    p_bullet("Difficulty tuning — addressed by Iteration 3 damage rebalance and the wave progression curve table"),
    p_bullet("Turn-order desyncs — addressed by Iteration 7 stability fix and code comments to prevent regression"),
],
"Localization Plan":  [p_oos("English only.")],
"Test Plan": [p_body("Iterative playtest cycles (see Iteration History below) drive design changes. Each cycle observes specific failure modes (wave 2 deaths, repetitive rooms, unclear highlights, weightless hits, broken turn order) and ships a targeted fix.")],

# ── Appendix: Asset List ─────────────────────────────────────────────────────
"Asset List": [p_body("Master list of all in-game assets, current and planned. Subsection tables below.")],
"Art__appendix": [
    make_table([2800, 1800, 4760], [
        ["Asset",              "Status",   "Notes"],
        ["shoki.png",          "Done",     "32×32, bottom-center pivot"],
        ["drone.png",          "Done",     "Cyan glass-cannon companion"],
        ["sniper.png",         "Done",     "Camo-green enemy"],
        ["heavy.png",          "Done",     "Navy steel enemy"],
        ["soldier.png",        "Planned",  "Grey gunmetal grunt — procedural fallback in current build"],
        ["brawler.png",        "Planned",  "Amber mech-suit companion"],
        ["trickster.png",      "Planned",  "Magenta saboteur companion"],
        ["ground.png",         "Done",     "Tan isometric cube tile"],
        ["ground_alt.png",     "Done",     "Purple variant for visual rhythm"],
        ["VFX / animations",   "Planned",  "Sprite animations to hook into the WalkToTileSmooth coroutine"],
    ]),
],
"Sound": [p_oos("audio is out of scope for the Week 6 prototype.")],
"Music__appendix": [p_oos("planned post-prototype.")],
"Voice": [p_oos("none — text-free, no voice work needed.")],

}

# Sections we want to APPEND as new H1 sections at the end (not present in the
# template but required by the DDG brief).
APPENDED_SECTIONS = [
    ("Two Interactive Elements", [
        p_body("The DDG brief requires at least two interactive elements (beyond the player character) that follow the sensory-input → processing → action pattern, and that can interact with each other. Shoki's Adventure meets this with two systems that explicitly form a feedback loop."),
        p_rich(("Element 1 — Enemy AI. ", {"bold": True}), ("See: queries TurnManager for player-faction entities; processing: ComputePlan() determines move destination and attack tile; action: commits to telegraphed plan on the enemy's turn.",)),
        p_rich(("Element 2 — Environmental Traps. ", {"bold": True}), ("Collision: Entity.MoveTo invokes TrapSystem.ResolveTrap on arrival; processing: switch on trap type; action: deals damage, applies status, or rewrites tile state.",)),
        p_rich(("Interaction: ", {"bold": True}), ("Enemies COMMIT to telegraphed plans and their pathfinder treats trap tiles as walkable. The player can bait them onto pits — e.g., a Heavy telegraphed to walk south through a Pit will step in and die. This is the system-on-system interaction the rubric asks for.",)),
    ]),
    ("Playability, Accessibility & Juiciness", [
        p_rich(("Playability: ", {"bold": True}), ("every legal action is visualised before commitment (move + attack highlights); no hidden information (telegraphs + HP bars); minimal controls (click + Q + Space).",)),
        p_rich(("Accessibility: ", {"bold": True}), ("one-handed input; high-contrast UI; HP encoded with colour AND length so colour-blind players parse correctly; no reflex requirements (turn-based).",)),
        p_rich(("Juiciness: ", {"bold": True}), ("hit-burst particles on every damage event; floating HP bars that shrink and colour-shift; smooth tile-to-tile walking; pulsing yellow telegraph tiles; post-wave heal feedback.",)),
    ]),
    ("Playtest Feedback Log", [
        p_body("DDG-required documentation of what each playtest cycle observed and what was changed in response."),
        make_table([1100, 3500, 4760], [
            ["Cycle",            "Observation",                                                                                  "Decision"],
            ["Concept review",   "30×30 top-down grid was too large to read at a glance.",                                       "→ Iteration 1: pivot to isometric 8×8, 2-tile cardinal movement."],
            ["Design review",    "Pokémon-type counters added memorisation tax without making the puzzle harder.",               "→ Iteration 2: drop type counters; add Companion system + telegraphs."],
            ["Playtest 1",       "\"I died on wave 2 every time.\" Worst-case round = 63 damage.",                              "→ Iteration 3: enemy damage cut ~45%; heal raised 50%→75%."],
            ["Playtest 2",       "\"All rooms look the same.\"",                                                                "→ Iteration 4: per-wave Void tile carving."],
            ["Playtest 3",       "\"I can't tell which tiles are valid attack targets.\"",                                       "→ Iteration 5a: top-face-only highlights."],
            ["Playtest 3",       "\"How much HP does this guy have?\"",                                                          "→ Iteration 5b: floating HP bars."],
            ["Playtest 3",       "\"Hits feel weightless.\"",                                                                    "→ Iteration 5c: hit-burst particles."],
            ["Playtest 4",       "\"Shoki just teleports.\"",                                                                    "→ Iteration 6: smooth path-walking."],
            ["Stability",        "\"Clicked a diagonal blue tile, Shoki stopped one short.\"",                                   "→ Iteration 7a: BFS-reachable highlights."],
            ["Stability",        "\"Game froze when an enemy walked into a pit.\"",                                              "→ Iteration 7b: UnregisterEntity index fix."],
        ]),
    ]),
    ("Iteration History", [
        p_body("Detailed reasoning for each of the seven iterations the prototype has gone through."),
        p_rich(("Iteration 1 — Top-down 30×30 → Isometric 8×8. ", {"bold": True}),
               ("Trigger: concept review. Why: 30×30 was unreadable; 8×8 keeps decisions tight. Required rewriting all spatial systems.",)),
        p_rich(("Iteration 2 — Drop type counters; add Companions + Telegraphs. ", {"bold": True}),
               ("Trigger: design review. Why: type counters added cognitive load without depth. Companions add permadeath stakes; telegraphs eliminate hidden information.",)),
        p_rich(("Iteration 3 — Damage rebalance. ", {"bold": True}),
               ("Trigger: playtest 1. Why: wave-6 worst case 63 dmg = lethal in 2 rounds. New numbers cap at 33 dmg/round, recoverable with 75% heal.",)),
        p_rich(("Iteration 4 — Void tile carving. ", {"bold": True}),
               ("Trigger: playtest 2 (room fatigue). Why: static 8×8 rectangles felt repetitive. Per-wave silhouette variation changes tactical character.",)),
        p_rich(("Iteration 5 — Juiciness pass (highlights, HP bars, particles). ", {"bold": True}),
               ("Trigger: playtest 3 feedback. Why: directly addresses Playability/Accessibility/Juiciness rubric. Combat reads as combat now.",)),
        p_rich(("Iteration 6 — Smooth path-walking. ", {"bold": True}),
               ("Trigger: animation prep + trap-timing readability. Why: teleport broke trap timing; smooth walking creates an animation hook.",)),
        p_rich(("Iteration 7 — Stability + reachability fixes. ", {"bold": True}),
               ("Trigger: stability playtest. Why: BFS-reachable highlights stopped \"corner stop short\" bug; UnregisterEntity index fix stopped turn-order desync when an entity died mid-turn.",)),
    ]),
    ("Lecture Learnings Coverage", [
        p_body("DDG requires at least 5 of 9 lecture learnings to be addressed. This GDD covers 8."),
        make_table([4680, 4680], [
            ["Learning",                                "Where it lives in this GDD"],
            ["Core loop diagram (micro/macro/meta)",    "Game Flow section"],
            ["Intended fun & engagement",               "Look and Feel → LeBlanc fun type"],
            ["GDD (mandatory)",                         "This entire document"],
            ["Multiple prototype iterations",           "Playtest Feedback Log + Iteration History (7 entries)"],
            ["Interesting choices & their evolution",   "Actions + Iteration 2 (companion system)"],
            ["Balancing work",                          "Iteration 3 + Game Progression wave-curve table"],
            ["Randomness & changes",                    "Game Progression + Area #1"],
            ["Narrative aspects",                       "Story and Narrative + Back story"],
            ["Visual gamestate",                        "Visual System decoder table"],
            ["Design patterns",                         "Singleton, Observer, State machine, Component composition, Coroutines — used throughout the codebase"],
        ]),
    ]),
]

# ── Engine ──────────────────────────────────────────────────────────────────

def find_heading_paragraph(xml, heading_text):
    """Find a heading <w:p> by its text content. Returns end index of </w:p>, or None."""
    # Match a paragraph that uses any HeadingN style AND contains the target text.
    # Headings often span multiple runs, so concatenate w:t segments inside the paragraph.
    p_iter = re.finditer(r'<w:p\b[^>]*(?<!/)>.*?</w:p>', xml, re.DOTALL)
    for m in p_iter:
        para = m.group(0)
        # Must use a Heading style
        if not re.search(r'<w:pStyle w:val="Heading\d+"/>', para):
            continue
        # Concatenate text
        texts = re.findall(r'<w:t[^>]*>([^<]*)</w:t>', para)
        full_text = "".join(texts).strip()
        # Allow loose match
        if full_text.lower() == heading_text.lower():
            return m.end()
    return None

def inject_after_index(xml, idx, content_xml):
    """Insert content_xml at position idx in xml."""
    return xml[:idx] + content_xml + xml[idx:]

# Headings to REMOVE entirely from the template — too granular for a small
# prototype. The DDG brief explicitly allows omitting sections that aren't
# relevant. We replace the entire heading paragraph with a no-op.
HEADINGS_TO_REMOVE = [
    # TOC heading itself (the field content is stripped by remove_toc)
    "Table of Contents", "Contents",
    # Movement sub-categories
    "General Movement", "Other Movement",
    # Objects sub-categories
    "Picking Up Objects", "Moving Objects",
    # Actions sub-categories
    "Switches and Buttons", "Picking Up, Carrying and Dropping", "Talking", "Reading",
    # Screen Descriptions sub-categories
    "Main Menu Screen", "Options Screen",
    # Cut scene #1 details
    "Cut scene #1", "Actors", "Description", "Storyboard", "Script",
    # Area #1 details (one room covers it)
    "General Description", "Physical Characteristics", "Levels that use area", "Connections to other areas",
    # Character #1 sub-details
    "Personality", "Look", "Physical characteristics", "Animations", "Special Abilities",
    "Relevance to game story", "Relationship to other characters", "Statistics",
    # Levels deep sub-categories — single-room game, no per-level walkthrough
    "Levels", "Level #1", "Synopsis", "Introductory Material", "Physical Description",
    "Map", "Critical Path", "Encounters", "Level Walkthrough", "Closing Material",
    # Asset List voice sub
    "Voice",
]

def remove_heading_paragraph(xml, heading_text):
    """Find and remove the entire <w:p> element whose only text is the heading."""
    p_iter = list(re.finditer(r'<w:p\b[^>]*(?<!/)>.*?</w:p>', xml, re.DOTALL))
    for m in p_iter:
        para = m.group(0)
        if not re.search(r'<w:pStyle w:val="Heading\d+"/>', para):
            continue
        texts = re.findall(r'<w:t[^>]*>([^<]*)</w:t>', para)
        full_text = "".join(texts).strip()
        if full_text.lower() == heading_text.lower():
            return xml[:m.start()] + xml[m.end():]
    return xml

def remove_toc(xml):
    """Remove the TOC heading + all TOC paragraphs (now stale after our edits).
    A reader can regenerate it via Word's 'Update Field' if desired.
    """
    # All TOC-related paragraphs use TOCHeading, TOC1, TOC2, TOC3, Contents, or bookmark refs
    p_iter = list(re.finditer(r'<w:p\b[^>]*(?<!/)>.*?</w:p>', xml, re.DOTALL))
    start_idx = None
    end_idx = None
    for m in p_iter:
        para = m.group(0)
        is_toc_style = bool(re.search(r'<w:pStyle w:val="(TOCHeading|TOC\d+|Contents)"/>', para))
        if is_toc_style:
            if start_idx is None:
                start_idx = m.start()
            end_idx = m.end()
        elif start_idx is not None and is_toc_style is False:
            # Allow gap paragraphs (empty Normal style) between TOC entries
            if re.search(r'<w:pStyle w:val="(Heading\d+)"/>', para):
                break
            # Else continue (might be a blank line in TOC area)
            end_idx = m.end()
    if start_idx is not None and end_idx is not None:
        return xml[:start_idx] + xml[end_idx:]
    return xml

def main():
    with open(DOC_XML, "r", encoding="utf-8") as f:
        xml = f.read()

    # 0. Remove the static TOC (it's stale now and eats 2-3 pages)
    before_len = len(xml)
    xml = remove_toc(xml)
    print(f"TOC removal: {before_len - len(xml)} chars stripped.")

    # 0b. Strip empty self-closing paragraphs (vertical-space placeholders).
    # The TOC removal leaves several behind, plus the template has many decorative
    # blank paragraphs between sections.
    before = xml.count("<w:p ")
    xml = re.sub(r'<w:p\b[^>/]*/>', '', xml)
    after = xml.count("<w:p ")
    print(f"Stripped {before - after} empty self-closing paragraphs.")

    # 1. Remove headings we don't need
    removed = 0
    for h in HEADINGS_TO_REMOVE:
        new_xml = remove_heading_paragraph(xml, h)
        if new_xml != xml:
            removed += 1
            xml = new_xml
    print(f"Removed {removed} headings.")

    injected = 0
    skipped = []

    # ── Inject content under each existing template heading ────────────────
    for heading, paragraphs in CONTENT.items():
        # Strip any disambiguation suffix used for duplicate names (e.g. "Music__appendix")
        actual_heading = heading.split("__")[0]
        idx = find_heading_paragraph(xml, actual_heading)
        if idx is None:
            skipped.append(heading)
            continue
        content_xml = "".join(paragraphs)
        xml = inject_after_index(xml, idx, content_xml)
        injected += 1

    # ── Append new H1 sections required by DDG but not in template ─────────
    # Insert before the closing </w:body>
    body_end = xml.rfind("</w:body>")
    if body_end >= 0:
        appended_xml = ""
        for title, paragraphs in APPENDED_SECTIONS:
            title_esc = title.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
            heading_p = (
                f'<w:p><w:pPr><w:pStyle w:val="Heading1"/></w:pPr>'
                f'<w:r><w:t xml:space="preserve">{title_esc}</w:t></w:r></w:p>'
            )
            appended_xml += heading_p + "".join(paragraphs)
        # Insert before the sectPr that lives at the end of <w:body>
        # Find <w:sectPr inside body and insert before it
        sectPr_match = re.search(r'<w:sectPr\b', xml[:body_end])
        if sectPr_match:
            insert_at = sectPr_match.start()
        else:
            insert_at = body_end
        xml = xml[:insert_at] + appended_xml + xml[insert_at:]

    with open(DOC_XML, "w", encoding="utf-8") as f:
        f.write(xml)

    print(f"Injected {injected} sections.")
    if skipped:
        print(f"Skipped (heading not found in template): {skipped}")
    print(f"Appended {len(APPENDED_SECTIONS)} new H1 sections at end.")

if __name__ == "__main__":
    main()
