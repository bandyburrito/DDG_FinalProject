const pptxgen = require("pptxgenjs");

const pres = new pptxgen();
pres.layout = "LAYOUT_16x9";
pres.title = "Shoki's Adventure — DDG Presentation";

// ── Palette ────────────────────────────────────────────────────────────────
const C = {
  dark:   "12112A",
  mid:    "1E1D42",
  cyan:   "00C8D7",
  orange: "FF6B35",
  light:  "F4F4F8",
  card:   "FFFFFF",
  border: "E0E0EA",
  text:   "1A1A2E",
  muted:  "6B6B8A",
  white:  "FFFFFF",
  amber:  "F59E0B",
  green:  "22C55E",
};

const makeShadow = () => ({ type: "outer", blur: 8, offset: 2, angle: 135, color: "000000", opacity: 0.08 });

// ── Layout constants ───────────────────────────────────────────────────────
// Slide: 10" × 5.625"
// 3-col: w=3.0", x = COL3_START + i*COL3_STEP (gap = 0.3")
const COL3_START = 0.25, COL3_W = 3.0, COL3_STEP = 3.3;
// 2-col: x1=0.3, x2=5.3, w=4.4, gap=0.3"
const COL2_X1 = 0.3, COL2_X2 = 5.3, COL2_W = 4.4;
// Header bar
const HDR_H = 0.8;
// Card content area top (after header)
const CARD_TOP = HDR_H + 0.25;    // = 1.05
// Available height for cards: 5.625 - CARD_TOP - 0.35 (bottom margin) = 4.275
const CARD_BOT_MARGIN = 0.35;
const MAX_CARD_H = 5.625 - CARD_TOP - CARD_BOT_MARGIN; // 4.275

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 1 — TITLE
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.dark };

  // Cyan left accent bar
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.12, h: 5.625, fill: { color: C.cyan }, line: { color: C.cyan } });

  // Game title — vertically centred on slide
  s.addText("SHOKI'S ADVENTURE", {
    x: 0.5, y: 1.2, w: 6.5, h: 1.1,
    fontSize: 44, bold: true, fontFace: "Arial Black",
    color: C.white, margin: 0,
  });

  // Thin cyan underline
  s.addShape(pres.shapes.RECTANGLE, { x: 0.5, y: 2.36, w: 4.0, h: 0.06, fill: { color: C.cyan }, line: { color: C.cyan } });

  // Subtitle
  s.addText("DDG Module — Week 6 Early Presentation", {
    x: 0.5, y: 2.55, w: 6.5, h: 0.4,
    fontSize: 16, color: C.cyan, fontFace: "Arial", margin: 0,
  });

  // Tagline
  s.addText("Grid-based tactical combat  ·  10 waves  ·  Survive or be captured", {
    x: 0.5, y: 3.1, w: 6.5, h: 0.35,
    fontSize: 13, color: "9090B8", fontFace: "Arial", italic: true, margin: 0,
  });

  // Right info box — vertically centred beside title block
  s.addShape(pres.shapes.RECTANGLE, { x: 7.5, y: 1.5, w: 2.15, h: 2.3, fill: { color: C.mid }, line: { color: "3A3A6A" } });
  s.addText([
    { text: "Engine\n",   options: { bold: true, color: C.cyan, breakLine: true } },
    { text: "Unity 6\n\n",options: { color: C.white, breakLine: true } },
    { text: "Genre\n",    options: { bold: true, color: C.cyan, breakLine: true } },
    { text: "Tactics / Roguelite\n\n", options: { color: C.white, breakLine: true } },
    { text: "Deadline\n", options: { bold: true, color: C.cyan, breakLine: true } },
    { text: "Week 6",     options: { color: C.white } },
  ], { x: 7.62, y: 1.6, w: 1.9, h: 2.1, fontSize: 12, fontFace: "Arial", valign: "top" });

  // Slide number
  s.addText("1", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: "4040A0", align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 2 — GAME CONCEPT
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.cyan }, line: { color: C.cyan } });
  s.addText("GAME CONCEPT", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  // Pitch box
  s.addShape(pres.shapes.RECTANGLE, { x: 0.3, y: 0.95, w: 9.4, h: 0.85, fill: { color: C.dark }, line: { color: C.dark }, shadow: makeShadow() });
  s.addText("Shoki is an alien crash-landed on Earth. Survive 10 waves of government soldiers on an 8×8 isometric grid — repair your ship and escape home.", {
    x: 0.45, y: 0.98, w: 9.1, h: 0.79, fontSize: 14, color: C.white, fontFace: "Arial", italic: true, valign: "middle",
  });

  // 3 inspiration cards — sized to content
  const CARD_H = 2.8;
  const cards = [
    { title: "Into the Breach", body: "Enemy telegraph system — enemies reveal their attack targets before acting. Player can react and dodge.", col: C.cyan },
    { title: "Mewgenics",       body: "Entity-driven emergent behaviour. Characters act independently based on their own internal logic.", col: C.orange },
    { title: "Shovel Knight",   body: "2D pixel art aesthetic. Clear readable sprites on a clean grid. Chunky, expressive characters.", col: C.amber },
  ];
  cards.forEach((c, i) => {
    const x = COL3_START + i * COL3_STEP;
    const y = 2.0;
    s.addShape(pres.shapes.RECTANGLE, { x, y, w: COL3_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
    s.addShape(pres.shapes.RECTANGLE, { x, y, w: COL3_W, h: 0.42, fill: { color: c.col }, line: { color: c.col } });
    s.addText(c.title, { x: x + 0.1, y: y + 0.04, w: COL3_W - 0.2, h: 0.34, fontSize: 12, bold: true, color: C.dark, fontFace: "Arial", valign: "middle", margin: 0 });
    s.addText(c.body,  { x: x + 0.12, y: y + 0.55, w: COL3_W - 0.24, h: 2.1, fontSize: 12, color: C.text, fontFace: "Arial", valign: "top" });
  });

  s.addText("2", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 3 — TWO INTERACTIVE ELEMENTS
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.orange }, line: { color: C.orange } });
  s.addText("TWO INTERACTIVE ELEMENTS", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.9;
  const CARD_Y = CARD_TOP;

  // Element 1 — Enemy AI
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: 0.5, fill: { color: C.cyan }, line: { color: C.cyan } });
  s.addText("ELEMENT 1 — Enemy AI", { x: COL2_X1 + 0.12, y: CARD_Y + 0.05, w: COL2_W - 0.24, h: 0.4, fontSize: 13, bold: true, color: C.dark, fontFace: "Arial", valign: "middle", margin: 0 });

  const aiRows = [
    { label: "SEE",      body: "Detects Shoki's grid position at the start of each round" },
    { label: "PROCESS",  body: "Computes move path + attack target, broadcasts intent via yellow telegraph tiles on the board" },
    { label: "ACT",      body: "Executes its telegraphed plan — moves first, then attacks the committed tile (even if Shoki has moved away)" },
  ];
  aiRows.forEach((r, i) => {
    const y = CARD_Y + 0.68 + i * 1.03;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1 + 0.12, y, w: 0.75, h: 0.65, fill: { color: C.cyan }, line: { color: C.cyan } });
    s.addText(r.label, { x: COL2_X1 + 0.12, y, w: 0.75, h: 0.65, fontSize: 9, bold: true, color: C.dark, align: "center", valign: "middle", margin: 0 });
    s.addText(r.body, { x: COL2_X1 + 0.98, y: y + 0.05, w: COL2_W - 1.1, h: 0.55, fontSize: 11, color: C.text, fontFace: "Arial", valign: "middle" });
  });
  s.addText("3 types: Soldier (rushes melee), Sniper (keeps distance, ranged), Heavy (slow, high HP)", {
    x: COL2_X1 + 0.12, y: CARD_Y + CARD_H - 0.38, w: COL2_W - 0.24, h: 0.3, fontSize: 10, color: C.muted, italic: true,
  });

  // Element 2 — Traps
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: 0.5, fill: { color: C.orange }, line: { color: C.orange } });
  s.addText("ELEMENT 2 — Environmental Traps", { x: COL2_X2 + 0.12, y: CARD_Y + 0.05, w: COL2_W - 0.24, h: 0.4, fontSize: 13, bold: true, color: C.dark, fontFace: "Arial", valign: "middle", margin: 0 });

  const trapRows = [
    { label: "COLLISION", body: "Any entity stepping onto a trap tile activates it — player OR enemy" },
    { label: "PROCESS",   body: "Trap type determines effect: Spike (10 dmg), Pit (instant kill), Slow Zone (–1 move range next turn)" },
    { label: "ACT",       body: "Effect applied immediately on entry — damage dealt, movement reduced, or entity removed" },
  ];
  trapRows.forEach((r, i) => {
    const y = CARD_Y + 0.68 + i * 1.03;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2 + 0.12, y, w: 0.82, h: 0.65, fill: { color: C.orange }, line: { color: C.orange } });
    s.addText(r.label, { x: COL2_X2 + 0.12, y, w: 0.82, h: 0.65, fontSize: 8, bold: true, color: C.white, align: "center", valign: "middle", margin: 0 });
    s.addText(r.body, { x: COL2_X2 + 1.04, y: y + 0.05, w: COL2_W - 1.16, h: 0.55, fontSize: 11, color: C.text, fontFace: "Arial", valign: "middle" });
  });
  s.addText("Key: enemies can be BAITED into traps — the two elements interact directly.", {
    x: COL2_X2 + 0.12, y: CARD_Y + CARD_H - 0.38, w: COL2_W - 0.24, h: 0.3, fontSize: 10, color: C.orange, bold: true, italic: true,
  });

  s.addText("3", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 4 — LEBLANC: CHALLENGE
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.dark };

  // Amber left accent bar
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.12, h: 5.625, fill: { color: C.amber }, line: { color: C.amber } });

  // Left side — label + big word (tighter layout)
  s.addText("LeBlanc's 8 Types of Fun", {
    x: 0.3, y: 0.45, w: 5.2, h: 0.35,
    fontSize: 11, color: "8080B0", italic: true, fontFace: "Arial", margin: 0,
  });
  // FIX: was 56pt in 4.5" box — "CHALLENGE" overflowed. Now 44pt in 5.2" box.
  s.addText("CHALLENGE", {
    x: 0.3, y: 0.85, w: 5.2, h: 1.1,
    fontSize: 44, bold: true, fontFace: "Arial Black",
    color: C.amber, margin: 0,
  });
  s.addText("Every turn is a puzzle. No two turns are the same.", {
    x: 0.3, y: 2.05, w: 5.0, h: 0.4,
    fontSize: 14, color: "B0B0D0", italic: true, fontFace: "Arial", margin: 0,
  });

  // Bullets on left side below subtitle
  const leftItems = [
    "Read enemy telegraph tiles before moving",
    "Positioning determines what you can attack",
    "Bait enemies into traps with smart movement",
  ];
  leftItems.forEach((b, i) => {
    s.addText([{ text: "  " + b, options: { bullet: true } }], {
      x: 0.3, y: 2.6 + i * 0.45, w: 5.0, h: 0.4,
      fontSize: 12, color: "C0C0DC", fontFace: "Arial",
    });
  });

  // Right column cards — 5 challenge aspects, fixed gutter
  const bullets = [
    { head: "Read the board",    body: "Yellow tiles show where enemies will attack next turn." },
    { head: "Positioning",       body: "Stand on the wrong tile after moving and take avoidable damage." },
    { head: "Melee vs. ranged",  body: "Choose attack mode each turn based on range and upgrade build." },
    { head: "Trap baiting",      body: "Route enemies through Spike or Pit tiles — use their telegraphed movement." },
    { head: "Build your run",    body: "Stacking multipliers over 10 waves forces long-term commitment choices." },
  ];
  bullets.forEach((b, i) => {
    const y = 0.55 + i * 0.95;
    s.addShape(pres.shapes.RECTANGLE, { x: 5.8, y, w: 3.9, h: 0.82, fill: { color: C.mid }, line: { color: "3A3A6A" }, shadow: makeShadow() });
    s.addText(b.head, { x: 5.95, y: y + 0.07, w: 3.6, h: 0.25, fontSize: 12, bold: true, color: C.amber, fontFace: "Arial", margin: 0 });
    s.addText(b.body, { x: 5.95, y: y + 0.36, w: 3.6, h: 0.38, fontSize: 11, color: "C0C0DC", fontFace: "Arial", margin: 0 });
  });

  s.addText("4", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: "5050A0", align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 5 — CORE LOOPS
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.green }, line: { color: C.green } });
  s.addText("CORE LOOPS", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.9;
  const CARD_Y = CARD_TOP;

  const loops = [
    {
      label: "MICRO", sub: "One turn", color: C.green,
      steps: ["Move up to 2 tiles on the grid", "Trap triggers if you enter a trap tile", "Attack — melee (8 tiles) or ranged (2–4 tiles)", "End turn → enemies execute their telegraphed plans"],
      note: "Repeats for each entity per initiative order",
    },
    {
      label: "MACRO", sub: "One wave", color: C.cyan,
      steps: ["Survive all enemies in the wave", "Heal 50% of damage taken this wave", "Choose upgrade: +25% Melee OR Ranged dmg", "Waves 3 / 6 / 9: choose a Companion"],
      note: "Loops 10 times per run",
    },
    {
      label: "META", sub: "Full run", color: C.orange,
      steps: ["HP carries over — no reset between waves", "Stack damage multipliers across all 10 waves", "Build a Companion team (max 3, permadeath)", "Survive wave 10 to escape — WIN"],
      note: "One run, no save states",
    },
  ];

  loops.forEach((l, i) => {
    const x = COL3_START + i * COL3_STEP;
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: 0.62, fill: { color: l.color }, line: { color: l.color } });
    s.addText(l.label, { x: x + 0.1, y: CARD_Y + 0.04, w: 1.6, h: 0.32, fontSize: 16, bold: true, fontFace: "Arial Black", color: C.dark, valign: "middle", margin: 0 });
    s.addText(l.sub,   { x: x + 0.1, y: CARD_Y + 0.36, w: COL3_W - 0.2, h: 0.22, fontSize: 10, color: C.dark, italic: true, margin: 0 });
    // 4 bullets at 0.62" spacing
    l.steps.forEach((step, j) => {
      s.addText([{ text: "  " + step, options: { bullet: true } }], {
        x: x + 0.1, y: CARD_Y + 0.78 + j * 0.62, w: COL3_W - 0.2, h: 0.56,
        fontSize: 11, color: C.text, fontFace: "Arial",
      });
    });
    // Note caption — outside card but with clearance
    s.addText(l.note, { x: x + 0.1, y: CARD_Y + CARD_H + 0.08, w: COL3_W - 0.2, h: 0.25, fontSize: 9, color: C.muted, italic: true });
  });

  s.addText("5", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 6 — INTERESTING CHOICES
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.cyan }, line: { color: C.cyan } });
  s.addText("INTERESTING CHOICES", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.9;
  const CARD_Y = CARD_TOP;

  const choices = [
    {
      when: "Every turn", title: "Melee vs. Ranged", color: C.cyan,
      points: [
        "Melee: hits ALL 8 adjacent tiles — high damage, risky positioning required",
        "Ranged: single target, 2–4 tiles away — safe but weaker",
        "Each upgraded independently — commits you to a build early",
      ],
    },
    {
      when: "Post-wave", title: "Upgrade Pick", color: C.amber,
      points: [
        "+25% Melee damage multiplier",
        "+25% Ranged damage multiplier",
        "Multipliers compound — ×1.75 by wave 7 if you focus one path",
      ],
    },
    {
      when: "Waves 3, 6, 9", title: "Companion Pick", color: C.orange,
      points: [
        "DRONE — glass cannon ranged (HP 20, 2–4 tile range)",
        "BRAWLER — tanky melee (HP 40, soaks damage)",
        "TRICKSTER — acts TWICE per turn (HP 25, flexible)",
        "Permadeath — companion is gone permanently if killed",
      ],
    },
  ];

  choices.forEach((c, i) => {
    const x = COL3_START + i * COL3_STEP;
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: 0.72, fill: { color: c.color }, line: { color: c.color } });
    s.addText(c.when,  { x: x + 0.1, y: CARD_Y + 0.04, w: COL3_W - 0.2, h: 0.22, fontSize: 9, color: C.dark, italic: true, margin: 0 });
    s.addText(c.title, { x: x + 0.1, y: CARD_Y + 0.28, w: COL3_W - 0.2, h: 0.4, fontSize: 15, bold: true, fontFace: "Arial Black", color: C.dark, margin: 0 });
    c.points.forEach((p, j) => {
      s.addText([{ text: "  " + p, options: { bullet: true } }], {
        x: x + 0.1, y: CARD_Y + 0.85 + j * 0.73, w: COL3_W - 0.2, h: 0.66,
        fontSize: 11, color: C.text, fontFace: "Arial",
      });
    });
  });

  s.addText("6", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 7 — RANDOMNESS & BALANCING
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.amber }, line: { color: C.amber } });
  s.addText("RANDOMNESS & BALANCING", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.6;
  const CARD_Y = CARD_TOP;

  // Left — Randomness Sources
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: 0.48, fill: { color: C.amber }, line: { color: C.amber } });
  s.addText("Randomness Sources", { x: COL2_X1 + 0.12, y: CARD_Y + 0.06, w: COL2_W - 0.24, h: 0.36, fontSize: 14, bold: true, color: C.dark, valign: "middle", margin: 0 });

  const randItems = [
    { head: "Initiative order", body: "d20 + speed stat rolled each round — player and enemies act in result order" },
    { head: "Room layout",      body: "Full 8×8 grid regenerates after every wave with new obstacle and trap positions" },
    { head: "Trap placement",   body: "Fixed counts (1 spike, 1 slow zone, 2 obstacles) at valid random positions" },
    { head: "Enemy spawns",     body: "Edge-based spawn pool with minimum 3-tile separation from Shoki's start" },
  ];
  randItems.forEach((r, i) => {
    const y = CARD_Y + 0.65 + i * 0.71;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1 + 0.12, y: y + 0.07, w: 0.06, h: 0.5, fill: { color: C.amber }, line: { color: C.amber } });
    s.addText(r.head, { x: COL2_X1 + 0.28, y: y + 0.04, w: COL2_W - 0.4, h: 0.24, fontSize: 12, bold: true, color: C.text, margin: 0 });
    s.addText(r.body, { x: COL2_X1 + 0.28, y: y + 0.3, w: COL2_W - 0.4, h: 0.32, fontSize: 10, color: C.muted, margin: 0 });
  });

  // Right — Balancing Decisions
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: 0.48, fill: { color: C.orange }, line: { color: C.orange } });
  s.addText("Balancing Decisions", { x: COL2_X2 + 0.12, y: CARD_Y + 0.06, w: COL2_W - 0.24, h: 0.36, fontSize: 14, bold: true, color: C.dark, valign: "middle", margin: 0 });

  const balItems = [
    { head: "Enemy scaling",   body: "+1 enemy every 2–3 waves — keeps early waves learnable" },
    { head: "HP persistence",  body: "Player HP carries over — every wave has real consequences" },
    { head: "Companion cap",   body: "Max 3 companions + permadeath prevents late-game snowballing" },
    { head: "Multiplier start",body: "Upgrades start at ×1.0 — power is earned, not given" },
  ];
  balItems.forEach((r, i) => {
    const y = CARD_Y + 0.65 + i * 0.71;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2 + 0.12, y: y + 0.07, w: 0.06, h: 0.5, fill: { color: C.orange }, line: { color: C.orange } });
    s.addText(r.head, { x: COL2_X2 + 0.28, y: y + 0.04, w: COL2_W - 0.4, h: 0.24, fontSize: 12, bold: true, color: C.text, margin: 0 });
    s.addText(r.body, { x: COL2_X2 + 0.28, y: y + 0.3, w: COL2_W - 0.4, h: 0.32, fontSize: 10, color: C.muted, margin: 0 });
  });

  s.addText("7", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 8 — VISUAL GAMESTATE + PAJ
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.green }, line: { color: C.green } });
  s.addText("VISUAL GAMESTATE  ·  PLAYABILITY  ·  ACCESSIBILITY  ·  JUICINESS", {
    x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 15, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0,
  });

  const CARD_Y = CARD_TOP;

  // Left — tile legend (sized to exactly fit 6 tiles)
  const LEGEND_H = 3.6;
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: LEGEND_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: 0.48, fill: { color: C.green }, line: { color: C.green } });
  s.addText("Board reads state at a glance", { x: COL2_X1 + 0.12, y: CARD_Y + 0.06, w: COL2_W - 0.24, h: 0.36, fontSize: 13, bold: true, color: C.dark, valign: "middle", margin: 0 });

  const tiles = [
    { color: "3399FF", label: "Blue tile",             desc: "Valid move destination" },
    { color: "FF6B35", label: "Orange tile",            desc: "Valid attack target" },
    { color: "FFD700", label: "Yellow tile (pulsing)",  desc: "Enemy will attack here next turn" },
    { color: "BBBBBB", label: "Grey outline tile",      desc: "Enemy will move here next turn" },
    { color: "FF8C00", label: "Amber tile",             desc: "Spike trap (10 dmg on entry)" },
    { color: "151520", label: "Dark tile",              desc: "Pit trap — instant kill" },
  ];
  tiles.forEach((t, i) => {
    const y = CARD_Y + 0.6 + i * 0.49;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1 + 0.15, y: y + 0.06, w: 0.3, h: 0.3, fill: { color: t.color }, line: { color: "AAAAAA" } });
    s.addText(t.label, { x: COL2_X1 + 0.55, y: y + 0.03, w: COL2_W - 0.67, h: 0.22, fontSize: 11, bold: true, color: C.text, margin: 0 });
    s.addText(t.desc,  { x: COL2_X1 + 0.55, y: y + 0.26, w: COL2_W - 0.67, h: 0.2,  fontSize: 9,  color: C.muted, margin: 0 });
  });

  // Right — PAJ cards (3 cards, equal height, with gutter)
  const PAJ_H = 1.12;
  const PAJ_GAP = 0.12;
  const paj = [
    { label: "PLAYABILITY",   color: C.cyan,   body: "Highlights show all valid actions before clicking. No hidden information. Controls reminder on-screen at all times." },
    { label: "ACCESSIBILITY", color: C.amber,  body: "Three inputs only: mouse click, Q (toggle mode), Space (end turn). Turn-based — zero time pressure. High-contrast UI." },
    { label: "JUICINESS",     color: C.orange, body: "Telegraph tiles pulse with alpha animation. HP bar updates live. Heal feedback on post-wave screen. Highlights clear instantly on state change." },
  ];
  paj.forEach((p, i) => {
    const y = CARD_Y + i * (PAJ_H + PAJ_GAP);
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y, w: COL2_W, h: PAJ_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y, w: 0.08, h: PAJ_H, fill: { color: p.color }, line: { color: p.color } });
    s.addText(p.label, { x: COL2_X2 + 0.2, y: y + 0.08, w: COL2_W - 0.3, h: 0.26, fontSize: 12, bold: true, color: C.text, fontFace: "Arial", margin: 0 });
    s.addText(p.body,  { x: COL2_X2 + 0.2, y: y + 0.38, w: COL2_W - 0.3, h: 0.68, fontSize: 10, color: C.muted, fontFace: "Arial" });
  });

  s.addText("8", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 9 — DESIGN ITERATIONS
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.orange }, line: { color: C.orange } });
  s.addText("DESIGN ITERATIONS  (2 completed)", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.9;
  const CARD_Y = CARD_TOP;

  const iters = [
    {
      num: "V1", title: "Top-down grid", status: "CHANGED", statusColor: C.amber,
      what:   "Started as a flat top-down orthographic grid.",
      why:    "Depth and positioning were unclear — hard to read spatial relationships at a glance.",
      result: "Pivoted to isometric. Rebuilt tile math, highlight system, and entity sort ordering.",
    },
    {
      num: "V2", title: "Pokémon type system", status: "DROPPED", statusColor: "EF4444",
      what:   "Early design had type advantages — attacks countered specific enemy types.",
      why:    "Added complexity without improving the Challenge fun. Obscured positioning decisions.",
      result: "Removed. Kept 3 distinct enemy types with clear visual roles instead.",
    },
    {
      num: "V3", title: "Current prototype", status: "ACTIVE", statusColor: C.green,
      what:   "Isometric 8×8 grid, telegraph system, companions, damage multiplier upgrades.",
      why:    "All major loops playable. Tile highlights, AI turns, traps all functional.",
      result: "Presenting this build. Planned additions: sound, sprite polish, difficulty tuning.",
    },
  ];

  iters.forEach((it, i) => {
    const x = COL3_START + i * COL3_STEP;
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
    // Header bar
    s.addShape(pres.shapes.RECTANGLE, { x, y: CARD_Y, w: COL3_W, h: 0.62, fill: { color: C.dark }, line: { color: C.dark } });
    s.addText(it.num, { x: x + 0.1, y: CARD_Y + 0.04, w: 0.55, h: 0.54, fontSize: 22, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });
    // Status pill
    s.addShape(pres.shapes.RECTANGLE, { x: x + 0.72, y: CARD_Y + 0.17, w: 0.9, h: 0.28, fill: { color: it.statusColor }, line: { color: it.statusColor } });
    s.addText(it.status, { x: x + 0.72, y: CARD_Y + 0.17, w: 0.9, h: 0.28, fontSize: 8, bold: true, color: C.white, align: "center", valign: "middle", margin: 0 });
    s.addText(it.title, { x: x + 0.1, y: CARD_Y + 0.7, w: COL3_W - 0.2, h: 0.32, fontSize: 13, bold: true, color: C.text, fontFace: "Arial", margin: 0 });

    const rows = [
      { label: "WHAT",    body: it.what },
      { label: "WHY",     body: it.why  },
      { label: "OUTCOME", body: it.result },
    ];
    rows.forEach((r, j) => {
      const y = CARD_Y + 1.12 + j * 0.9;
      s.addText(r.label, { x: x + 0.1, y, w: COL3_W - 0.2, h: 0.22, fontSize: 8, bold: true, color: it.statusColor, margin: 0 });
      s.addText(r.body,  { x: x + 0.1, y: y + 0.24, w: COL3_W - 0.2, h: 0.6, fontSize: 10, color: C.text, fontFace: "Arial" });
    });
  });

  s.addText("9", { x: 9.6, y: 5.3, w: 0.3, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 10 — NARRATIVE + ASSET LIST
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.light };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 10, h: HDR_H, fill: { color: C.dark }, line: { color: C.dark } });
  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.08, h: HDR_H, fill: { color: C.cyan }, line: { color: C.cyan } });
  s.addText("NARRATIVE + ASSET LIST", { x: 0.25, y: 0, w: 9.5, h: HDR_H, fontSize: 20, bold: true, fontFace: "Arial Black", color: C.white, valign: "middle", margin: 0 });

  const CARD_H = 3.8;
  const CARD_Y = CARD_TOP;

  // Left — Narrative
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1, y: CARD_Y, w: COL2_W, h: 0.48, fill: { color: C.cyan }, line: { color: C.cyan } });
  s.addText("Narrative", { x: COL2_X1 + 0.12, y: CARD_Y + 0.06, w: COL2_W - 0.24, h: 0.36, fontSize: 14, bold: true, color: C.dark, valign: "middle", margin: 0 });

  const narPoints = [
    { head: "Protagonist", body: "Shoki — alien, sympathetic underdog stranded on Earth after crash" },
    { head: "Antagonist",  body: "Government forces closing in — soldiers, snipers, heavy units" },
    { head: "Goal",        body: "Survive 10 waves → repair ship → escape. Each wave = new ambush" },
    { head: "Environment", body: "Crash site — traps are industrial debris and electrical hazards" },
    { head: "Companions",  body: "Survivors Shoki rescues; they fight alongside him and can die permanently" },
  ];
  narPoints.forEach((n, i) => {
    const y = CARD_Y + 0.62 + i * 0.64;
    s.addShape(pres.shapes.RECTANGLE, { x: COL2_X1 + 0.12, y: y + 0.09, w: 0.06, h: 0.42, fill: { color: C.cyan }, line: { color: C.cyan } });
    s.addText(n.head, { x: COL2_X1 + 0.28, y: y + 0.05, w: COL2_W - 0.4, h: 0.22, fontSize: 11, bold: true, color: C.text, margin: 0 });
    s.addText(n.body, { x: COL2_X1 + 0.28, y: y + 0.29, w: COL2_W - 0.4, h: 0.3,  fontSize: 10, color: C.muted, margin: 0 });
  });

  // Right — Asset list (table style with matching card height)
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: CARD_H, fill: { color: C.card }, line: { color: C.border }, shadow: makeShadow() });
  s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2, y: CARD_Y, w: COL2_W, h: 0.48, fill: { color: C.orange }, line: { color: C.orange } });
  s.addText("Asset List", { x: COL2_X2 + 0.12, y: CARD_Y + 0.06, w: COL2_W - 0.24, h: 0.36, fontSize: 14, bold: true, color: C.dark, valign: "middle", margin: 0 });

  const assets = [
    { cat: "Player",     items: "Shoki sprite (Aseprite PNG)" },
    { cat: "Enemies",    items: "Soldier, Sniper, Heavy sprites" },
    { cat: "Companions", items: "Drone, Brawler, Trickster sprites" },
    { cat: "Tiles",      items: "Ground iso1, Ground iso2 (32×32 iso)" },
    { cat: "Overlays",   items: "Move HL, Attack HL, Telegraph ×2" },
    { cat: "Traps",      items: "Spike, Pit, Slow Zone tile sprites" },
    { cat: "Audio",      items: "SFX: attack, move, hurt (planned)" },
    { cat: "UI",         items: "HP bar, wave HUD, upgrade screen, end screens" },
  ];
  assets.forEach((a, i) => {
    const y = CARD_Y + 0.6 + i * 0.39;
    // Alternating row shade
    if (i % 2 === 0) {
      s.addShape(pres.shapes.RECTANGLE, { x: COL2_X2 + 0.08, y: y + 0.02, w: COL2_W - 0.16, h: 0.34, fill: { color: "F8F8FC" }, line: { color: "F8F8FC" } });
    }
    s.addText(a.cat,   { x: COL2_X2 + 0.12, y: y + 0.06, w: 1.2, h: 0.3, fontSize: 10, bold: true, color: C.text, margin: 0 });
    s.addText(a.items, { x: COL2_X2 + 1.38, y: y + 0.06, w: COL2_W - 1.5, h: 0.3, fontSize: 10, color: C.muted, margin: 0 });
  });

  s.addText("10", { x: 9.5, y: 5.3, w: 0.4, h: 0.2, fontSize: 9, color: C.muted, align: "right" });
}

// ─────────────────────────────────────────────────────────────────────────────
// SLIDE 11 — REQUIREMENTS CHECKLIST
// ─────────────────────────────────────────────────────────────────────────────
{
  const s = pres.addSlide();
  s.background = { color: C.dark };

  s.addShape(pres.shapes.RECTANGLE, { x: 0, y: 0, w: 0.12, h: 5.625, fill: { color: C.green }, line: { color: C.green } });
  s.addText("REQUIREMENTS CHECKLIST", {
    x: 0.3, y: 0.2, w: 9.4, h: 0.52, fontSize: 22, bold: true, fontFace: "Arial Black",
    color: C.white, margin: 0,
  });

  // Two balanced columns of 6 items each — pad shorter col with blank to match
  const checks = [
    // Left column (6)
    { ok: true,  req: "Two interactive elements (Enemy AI + Traps interact with each other)" },
    { ok: true,  req: "LeBlanc fun type — Challenge" },
    { ok: true,  req: "GDD using HSLU template" },
    { ok: true,  req: "Core loop diagram (micro / macro / meta)" },
    { ok: true,  req: "Interesting choices — melee vs ranged, upgrades, companions" },
    { ok: true,  req: "Playability + Accessibility + Juiciness addressed" },
    // Right column (6)
    { ok: true,  req: "Randomness — initiative, room layout, spawns, trap placement" },
    { ok: true,  req: "Visual gamestate — tile colour coding + telegraph system" },
    { ok: true,  req: "Narrative — Shoki's crash-landing escape story" },
    { ok: "wip", req: "2 iterations documented (top-down → iso, type system dropped)" },
    { ok: "wip", req: "Design patterns to be listed in final GDD" },
    { ok: "wip", req: "Balancing document to be expanded post-playtesting" },
  ];

  const COL = 6; // items per column
  checks.forEach((c, i) => {
    const col = i < COL ? 0 : 1;
    const row = i < COL ? i : i - COL;
    const x = 0.3 + col * 4.9;
    const y = 0.9 + row * 0.73;
    const dotColor = c.ok === true ? C.green : C.amber;
    // Dot centred on first text line (approx 0.13" from top of row)
    s.addShape(pres.shapes.OVAL, { x: x, y: y + 0.1, w: 0.26, h: 0.26, fill: { color: dotColor }, line: { color: dotColor } });
    s.addText(c.req, {
      x: x + 0.38, y: y, w: 4.38, h: 0.58,
      fontSize: 11, color: c.ok === true ? "C0C0DC" : "F0C060",
      fontFace: "Arial", valign: "middle",
    });
  });

  // Legend
  s.addShape(pres.shapes.OVAL, { x: 0.3, y: 5.22, w: 0.18, h: 0.18, fill: { color: C.green }, line: { color: C.green } });
  s.addText("Done", { x: 0.56, y: 5.19, w: 0.8, h: 0.24, fontSize: 10, color: "88D888" });
  s.addShape(pres.shapes.OVAL, { x: 1.5, y: 5.22, w: 0.18, h: 0.18, fill: { color: C.amber }, line: { color: C.amber } });
  s.addText("In progress", { x: 1.76, y: 5.19, w: 1.2, h: 0.24, fontSize: 10, color: "E0B050" });

  s.addText("11", { x: 9.5, y: 5.3, w: 0.4, h: 0.2, fontSize: 9, color: "5050A0", align: "right" });
}

// ── Write file ─────────────────────────────────────────────────────────────
const outPath = "/home/ammarrexhaj/Documents/UnityProjects/aBondisJourney_Rogueslop/GDD/Shokis_Adventure_Presentation.pptx";
pres.writeFile({ fileName: outPath })
  .then(() => console.log("Saved:", outPath))
  .catch(err => { console.error(err); process.exit(1); });
