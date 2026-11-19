// Figures of the lessons, first half (port of LessonFigures.cs): the gemstone, hardness, interference figures, spectroscope, Chelsea filter, UV cabinet, dichroscope.
(function () {
  "use strict";
  var K = window.LessonKit, M = K.M, C = K.C, V = K.V, alpha = K.alpha, mix = K.mix, WHITE = K.WHITE, rot = K.rot, add = K.add, mul = K.mul;
  var Ink = C(0.92, 0.94, 0.98), Dim = C(0.50, 0.55, 0.63), Gold = C(0.95, 0.85, 0.65), Cyan = C(0.45, 0.85, 1), Stone = C(0.66, 0.36, 0.86),
    StoneLight = C(0.82, 0.62, 0.96), Dark = C(0.02, 0.02, 0.03);
  K.palette = { Ink: Ink, Dim: Dim, Gold: Gold, Cyan: Cyan, Stone: Stone, StoneLight: StoneLight, Dark: Dark, Wood: C(0.55, 0.38, 0.22), Water: C(0.30, 0.55, 0.90, 0.40) };
  var sin = Math.sin, cos = Math.cos, PI = Math.PI;

  // ---- What a gemstone is ----
  var CriteriaNames = ["Beauté", "Rareté", "Durabilité", "Valeur", "Acceptabilité"];
  var CriteriaColors = [C(0.90, 0.45, 0.70), C(0.95, 0.75, 0.25), C(0.40, 0.70, 0.95), C(0.45, 0.85, 0.50), C(0.75, 0.55, 0.95)];
  K.register("criteria", function (c, focus, t) {
    var pulse = 0.5 + 0.5 * sin(t * 3);
    for (var i = 0; i < 5; i++) {
      var angle = (90 + 72 * i) * M.deg, pos = V(cos(angle) * 150, sin(angle) * 112);
      var selected = focus === i, shown = focus < 0 || selected;
      var color = shown ? CriteriaColors[i] : alpha(CriteriaColors[i], 0.22);
      c.line(V(0, 0), pos, selected ? 4 : 2, color);
      c.rect(pos, selected ? V(134 + 8 * pulse, 44 + 4 * pulse) : V(122, 38), color);
      c.label(CriteriaNames[i], pos, selected ? 16 : 14, shown ? WHITE : Dim, 130);
    }
    c.rect(V(0, 0), V(58, 58), Cyan, 45);
    c.rect(V(0, 0), V(30, 30), C(0.85, 0.97, 1), 45);
    c.label("GEMME", V(0, -52), 12, Gold, 100);
  });

  // ---- The family tree of the gemstones ----
  var NaturalColor = C(0.30, 0.62, 0.38), ArtificialColor = C(0.80, 0.25, 0.25);
  function treeNode(c, label, center, size, color, dim, fontSize) {
    c.rect(center, size, dim ? alpha(color, 0.18) : color);
    c.label(label, center, fontSize || 14, dim ? Dim : WHITE, size[0] - 8);
  }
  function treeLink(c, parent, parentHalf, child, childHalf, dim) {
    var color = dim ? C(0.5, 0.55, 0.63, 0.25) : C(0.80, 0.85, 0.92, 0.9);
    var from = V(parent[0] + parentHalf, parent[1]), to = V(child[0] - childHalf, child[1]), middle = (from[0] + to[0]) / 2;
    c.line(from, V(middle, from[1]), 2, color);
    c.line(V(middle, from[1]), V(middle, to[1]), 2, color);
    c.line(V(middle, to[1]), to, 2, color);
  }
  K.register("taxonomy", function (c, focus, t) {
    var reveal = focus === 0 ? t : 99, dimNatural = focus === 2, dimArtificial = focus === 1;
    var root = V(-205, 0);
    if (focus === 3) {
      var pulse = 0.5 + 0.5 * sin(t * 3);
      c.rect(V(0, 20), V(86, 86), Cyan, 45);
      c.rect(V(0, 20), V(46, 46), C(0.85, 0.97, 1), 45);
      c.rect(V(0, -70), V(150 + 8 * pulse, 40), Gold);
      c.label("GEMMES", V(0, -70), 20, C(0.2, 0.15, 0.05), 150);
      return;
    }
    var nat = V(-100, 73), non = V(-100, -78), cris = V(20, 105), noncris = V(20, 42), mineral = V(160, 105), amorph = V(160, 62),
      organic = V(160, 22), synth = V(20, -30), nocounter = V(20, -78), assembled = V(20, -126);
    if (reveal > 0.8) { treeLink(c, root, 42, nat, 52, dimNatural); treeLink(c, root, 42, non, 52, dimArtificial); }
    if (reveal > 1.6) {
      treeLink(c, nat, 52, cris, 62, dimNatural); treeLink(c, nat, 52, noncris, 62, dimNatural);
      treeLink(c, non, 52, synth, 62, dimArtificial); treeLink(c, non, 52, nocounter, 62, dimArtificial); treeLink(c, non, 52, assembled, 62, dimArtificial);
    }
    if (reveal > 2.4) {
      treeLink(c, cris, 62, mineral, 70, dimNatural); treeLink(c, noncris, 62, amorph, 70, dimNatural); treeLink(c, noncris, 62, organic, 70, dimNatural);
    }
    treeNode(c, "GEMMES", root, V(84, 34), Gold, false, 15);
    if (reveal > 0.8) {
      treeNode(c, "Naturelles", nat, V(104, 34), NaturalColor, dimNatural);
      treeNode(c, "Non naturelles", non, V(104, 34), ArtificialColor, dimArtificial);
    }
    if (reveal > 1.6) {
      treeNode(c, "Cristallines", cris, V(124, 32), NaturalColor, dimNatural);
      treeNode(c, "Non cristallines", noncris, V(124, 32), NaturalColor, dimNatural);
      treeNode(c, "Synthétiques", synth, V(124, 32), ArtificialColor, dimArtificial);
      treeNode(c, "Sans contrepartie dans la nature", nocounter, V(124, 46), ArtificialColor, dimArtificial, 12);
      treeNode(c, "Gemmes assemblées", assembled, V(124, 32), ArtificialColor, dimArtificial, 13);
    }
    if (reveal > 2.4) {
      treeNode(c, "Minéral", mineral, V(140, 30), NaturalColor, dimNatural);
      treeNode(c, "Amorphe", amorph, V(140, 30), NaturalColor, dimNatural);
      treeNode(c, "Matières organiques", organic, V(140, 30), NaturalColor, dimNatural, 13);
    }
    if (reveal > 3.2) {
      var ex = dimArtificial ? alpha(Dim, 0.4) : Ink;
      c.label("rubis de synthèse", V(160, -30), 12, ex, 140);
      c.label("YAG, cubic zirconia", V(160, -78), 12, ex, 140);
      c.label("doublets, triplets", V(160, -126), 12, ex, 140);
    }
  });

  // ---- Hardness ----
  var MohsNames = ["Talc", "Gypse", "Calcite", "Fluorite", "Apatite", "Orthose", "Quartz", "Topaze", "Corindon", "Diamant"];
  var Knoop = [1, 32, 135, 163, 430, 560, 820, 1340, 2100, 7000];

  function scratchRule(c, t) {
    var cycle = (t % 4) / 4, travel = M.clamp01(cycle / 0.6);
    for (var row = 0; row < 2; row++) {
      var y = row === 0 ? 70 : -75, scratches = row === 0, left = V(-110, y);
      c.rect(V(0, y - 22), V(260, 44), row === 0 ? C(0.62, 0.52, 0.78) : C(0.85, 0.35, 0.40));
      c.label(row === 0 ? "Quartz (7)" : "Corindon (9)", V(-170, y - 22), 12, Ink, 90);
      if (scratches && travel > 0) c.line(add(left, V(0, -3)), add(left, V(220 * travel, -3)), 3, C(1, 1, 1, 0.9));
      var tip = add(left, V(220 * travel, 0));
      c.rect(add(tip, V(0, 14)), V(14, 30), row === 0 ? C(0.85, 0.35, 0.40) : C(0.62, 0.52, 0.78), 0);
      c.circle(tip, 5, Ink);
      c.label(row === 0 ? "pointe de corindon (9)  →  raye" : "pointe de quartz (7)  →  ne raye pas", V(40, y + 34), 12, row === 0 ? Gold : Dim, 300);
    }
    c.label("La pierre la plus dure raye la plus tendre", V(0, 152), 15, Gold, 450);
  }

  function testPlaces(c, t) {
    var green = C(0.40, 0.90, 0.50), red = C(0.95, 0.35, 0.30), background = C(0.05, 0.06, 0.09), pulse = 0.5 + 0.5 * sin(t * 4);
    c.rect(V(-150, 20), V(70, 46), Stone, 18);
    c.rect(V(-138, 38), V(40, 34), StoneLight, -14);
    c.rect(V(-165, 6), V(34, 30), C(0.50, 0.28, 0.68), 30);
    c.circle(V(0, 14), 36, Stone);
    c.rect(V(0, -22), V(90, 44), background);
    c.rect(V(0, -5), V(76, 12), C(0.50, 0.28, 0.68));
    c.circle(V(-12, 30), 8, C(1, 1, 1, 0.35));
    c.rect(V(150, 15), V(70, 70), Stone, 45);
    c.rect(V(150, 15), V(40, 40), StoneLight, 45);
    for (var i = 0; i < 2; i++) {
      var x = -150 + i * 150;
      c.line(V(x - 14, -72), V(x - 4, -84), 5, green);
      c.line(V(x - 4, -84), V(x + 16, -60), 5, green);
    }
    c.circle(V(150, -72), 22 + 3 * pulse, alpha(red, 0.25));
    c.line(V(136, -58), V(164, -86), 5, red);
    c.line(V(136, -86), V(164, -58), 5, red);
    c.label("Brut", V(-150, 85), 15, Gold, 120);
    c.label("Cabochon", V(0, 85), 15, Gold, 120);
    c.label("Pierre taillée", V(150, 85), 15, Gold, 140);
    c.label("sur n'importe quelle face", V(-150, -108), 11, Ink, 130);
    c.label("sous la base", V(0, -108), 11, Ink, 130);
    c.label("jamais : elle serait rayée", V(150, -108), 11, red, 140);
    c.label("Un test destructif", V(0, 148), 16, Gold, 440);
  }

  function pencils(c, t) {
    var stoneHardness = 6.5, pencilHardness = [5, 6, 7, 8], cycle = t % 14;
    c.rect(V(0, -45), V(300, 92), C(0.62, 0.52, 0.78));
    c.label("Pierre à tester", V(-190, -45), 12, Ink, 70);
    var result = "";
    for (var k = 0; k < 4; k++) {
      var rowY = -17 - k * 21, start = k * 3, progress = M.clamp01((cycle - start) / 1.6), scratches = pencilHardness[k] > stoneHardness;
      if (cycle >= start) {
        var done = cycle >= start + 1.6;
        if (scratches && progress > 0) c.line(V(-130, rowY), V(-130 + 260 * progress, rowY), 3, C(1, 1, 1, 0.9));
        if (!done || cycle < start + 3) result = K.t("Crayon") + " " + pencilHardness[k] + " : " + K.t(scratches ? "raye la pierre" : "ne raye pas");
      }
      if (cycle >= start && cycle < start + 1.8) {
        var tip = V(-130 + 260 * progress, rowY), dir = V(cos(35 * M.deg), sin(35 * M.deg));
        c.rect(add(tip, mul(dir, 40)), V(80, 10), C(0.90, 0.72, 0.30), 35);
        c.rect(add(tip, mul(dir, 4)), V(10, 6), Dark, 35);
      }
    }
    for (var j = 0; j < 4; j++) {
      var active = cycle >= j * 3 && cycle < j * 3 + 3, x = -105 + j * 70;
      c.rect(V(x, 112), V(54, 30), active ? Gold : C(0.30, 0.33, 0.40));
      c.label(String(pencilHardness[j]), V(x, 112), 16, active ? Dark : Ink, 54);
    }
    c.label("Crayons de dureté", V(-190, 112), 11, Dim, 80);
    c.label(result, V(0, -122), 15, Gold, 440);
    if (cycle > 10.5) c.label("Le 6 ne raye pas, le 7 raye : la dureté est entre 6 et 7", V(0, -148), 13, Ink, 460);
  }

  K.register("hardness", function (c, focus, t) {
    if (focus === 2) { scratchRule(c, t); return; }
    if (focus === 3) { pencils(c, t); return; }
    if (focus === 4) { testPlaces(c, t); return; }
    var absolute = focus === 1, baseY = -112;
    for (var i = 0; i < 10; i++) {
      var x = -189 + i * 42;
      var full = absolute ? Math.max(3, Knoop[i] / 7000 * 215) : (i + 1) * 21.5;
      var grow = M.smoothStep(0, 1, M.clamp01(t * 1.2 - i * 0.06)), height = full * grow;
      var color = mix(C(0.45, 0.75, 0.95), C(0.95, 0.80, 0.35), i / 9);
      c.rect(V(x, baseY + height / 2), V(30, height), color);
      var label = absolute ? (i >= 6 ? String(Knoop[i]) : "") : String(i + 1);
      c.label(label, V(x, baseY + height + 11), 12, Ink, 46);
      c.label(MohsNames[i], V(x, baseY - 14), 10, Ink, 46);
    }
    c.label(absolute ? "Dureté absolue (Knoop, valeurs approchées)" : "Échelle de Mohs (relative : de 1 à 10)", V(0, 152), 15, Gold, 470);
    if (absolute) c.label("le diamant est environ 3 fois plus dur que le corindon", V(-30, 105), 12, Ink, 300);
    else c.label("chaque minéral raye ceux qui le précèdent", V(-70, 105), 12, Ink, 300);
  });

  // ---- The interference figures of the conoscope (focus 4 uniaxial, 5 biaxial turning, 6 bull's eye, 7 three views) ----
  var FAR = V(2.4, 0.9);
  K.register("interf", function (c, focus, t) {
    var disc = C(0.75, 0.78, 0.85);
    if (focus === 7) {
      var views = [
        K.interference("bi_a", 160, V(-0.42, 0), V(0.42, 0), true, false, 3200),
        K.interference("bi_b", 160, rot(V(-0.42, 0), 45), rot(V(0.42, 0), 45), true, false, 3200),
        K.interference("bi_c", 160, V(0.12, 0.05), FAR, true, false, 3200)
      ];
      var names = ["a  deux axes : une croix", "b  la « moustache »", "c  un seul axe visible"];
      for (var i = 0; i < 3; i++) {
        var x = (i - 1) * 158;
        c.circle(V(x, 10), 74, disc);
        c.picture(views[i], V(x, 10), V(144, 144));
        c.label(names[i], V(x, -86), 13, Ink, 150);
      }
      c.label("Les figures d'une pierre biaxe", V(0, 148), 16, Gold, 440);
      c.label("selon la position de la pierre sur la lentille", V(0, -140), 13, Dim, 440);
      return;
    }
    c.circle(V(0, 8), 124, disc);
    if (focus === 5) {
      var angle = Math.round(M.repeat(t * 14, 90) * 2) / 2;
      var sprite = K.interference("bi_anim", 200, rot(V(-0.42, 0), angle), rot(V(0.42, 0), angle), true, false, 3200);
      c.picture(sprite, V(0, 8), V(240, 240));
      c.label(Math.abs(angle - 45) < 22 ? "les branches s'écartent : la « moustache »" : "les branches se rejoignent : une croix", V(0, -140), 14, Gold, 440);
      c.label("Biaxe : on tourne la pierre", V(0, 150), 15, Gold, 440);
      return;
    }
    var quartz = focus === 6, wobble = 1 + 0.04 * sin(t * 1.5);
    var uni = K.interference(quartz ? "uni_quartz" : "uni", 200, V(0, 0), V(0, 0), false, quartz, 3600 * Math.round(wobble * 100) / 100);
    c.picture(uni, V(0, 8), V(240, 240));
    c.label(quartz ? "Quartz : l'œil-de-taureau (bull's eye)" : "Uniaxe : croix noire et anneaux colorés", V(0, 150), 15, Gold, 440);
    c.label(quartz ? "le centre de la croix est une tache ronde, pâle" : "la croix ne bouge pas quand on tourne la pierre", V(0, -140), 13, Ink, 440);
  });

  // ---- The spectroscope ----
  function spectroPrism(c, t) {
    var rays = [C(0.55, 0.30, 1), C(0.25, 0.35, 1), C(0.20, 0.85, 1), C(0.30, 1, 0.40), C(1, 0.95, 0.25), C(1, 0.60, 0.20), C(1, 0.20, 0.20)];
    var pulse = 0.85 + 0.15 * sin(t * 4);
    c.circle(V(-215, 45), 10, C(1, 0.95, 0.6));
    c.line(V(-205, 44), V(-75, 10), 5, C(1, 1, 1, pulse));
    var a = V(-95, -30), b = V(-35, -30), top = V(-65, 32);
    c.line(a, b, 3, Cyan); c.line(b, top, 3, Cyan); c.line(top, a, 3, Cyan);
    c.rect(V(-65, -8), V(30, 28), C(0.55, 0.85, 1, 0.25));
    for (var i = 0; i < 7; i++) c.line(V(-40, 0), V(150, 60 - i * 20), 4, rays[i]);
    c.rect(V(153, 0), V(6, 150), C(0.55, 0.58, 0.65));
    c.picture(K.strip(""), V(0, -112), V(330, 38));
    c.label("400 nm", V(-150, -140), 12, Ink, 70);
    c.label("700 nm", V(150, -140), 12, Ink, 70);
    c.label("violet", V(-150, -88), 11, Dim, 70);
    c.label("rouge", V(150, -88), 11, Dim, 70);
    c.label("Le prisme sépare la lumière blanche en toutes ses couleurs", V(0, 150), 14, Gold, 460);
  }
  function spectroAbsorb(c) {
    c.label("Lumière blanche", V(-140, 128), 13, Gold, 160);
    c.picture(K.strip(""), V(20, 105), V(300, 38));
    c.rect(V(-150, -5), V(46, 46), C(0.12, 0.62, 0.36), 45);
    c.rect(V(-150, -5), V(26, 26), C(0.45, 0.90, 0.62), 45);
    c.label("À travers une émeraude", V(-140, 38), 13, Gold, 190);
    c.picture(K.strip("Emeraude"), V(20, 12), V(300, 38));
    c.label("Des couleurs manquent : des raies et des bandes sombres", V(0, -45), 14, Ink, 460);
    c.label("Ce qui manque dépend de ce qui colore la pierre : chrome, fer, manganèse, vanadium...", V(0, -72), 12, Dim, 460);
    c.label("Le spectre est l'empreinte de la couleur", V(0, -120), 15, Gold, 440);
  }
  function spectroScale(c, t) {
    var left = -180, width = 360;
    c.picture(K.strip("Corindon_Rubis"), V(0, 20), V(width, 48));
    for (var nm = 400; nm <= 700; nm += 50) {
      var x = left + (nm - 400) / 300 * width;
      c.line(V(x, -8), V(x, -18), 2, Ink);
      c.label(String(nm), V(x, -30), 12, Ink, 40);
    }
    var bandColors = [C(0.50, 0.32, 0.95), C(0.25, 0.45, 1), C(0.32, 0.78, 0.30), C(0.95, 0.85, 0.20), C(1, 0.58, 0.15), C(0.92, 0.22, 0.20)];
    K.bands.forEach(function (band, i) {
      var x0 = left + (band[1] - 400) / 300 * width, x1 = left + (band[2] - 400) / 300 * width;
      c.rect(V((x0 + x1) / 2, -44), V(x1 - x0 - 2, 5), bandColors[i]);
      c.label(band[0], V((x0 + x1) / 2, -57), 10, bandColors[i], Math.max(30, x1 - x0));
    });
    c.label("longueur d'onde en nanomètres (nm)", V(0, -76), 12, Dim, 300);
    var marker = M.lerp(400, 700, 0.5 + 0.5 * sin(t * 0.8)), markerX = left + (marker - 400) / 300 * width;
    c.line(V(markerX, -2), V(markerX, 50), 2, Gold);
    c.label(Math.round(marker) + " nm", V(markerX, 68), 14, Gold, 80);
    var rubyX = left + (694 - 400) / 300 * width;
    c.line(V(rubyX, -2), V(rubyX, -90), 1.5, C(1, 0.5, 0.45, 0.7));
    c.label("694 nm : la raie du chrome du rubis", V(rubyX - 70, -102), 12, C(1, 0.65, 0.60), 230);
    c.label("Du violet (400 nm) au rouge (700 nm)", V(0, 150), 15, Gold, 440);
  }
  function spectroExamples(c) {
    var species = ["Corindon_Rubis", "Emeraude", "Peridot", "Grenat_Almandin"];
    var names = ["Rubis", "Émeraude", "Péridot", "Grenat almandin"];
    var notes = ["raies fines du chrome dans le rouge, large bande jaune-vert", "raies du chrome dans le rouge (664 et 683 nm)", "triplet du fer dans le bleu (451, 470, 494 nm)", "bandes du fer : jaune, et deux dans le vert"];
    for (var i = 0; i < 4; i++) {
      var y = 112 - i * 70;
      c.label(names[i], V(-165, y + 4), 14, Gold, 120);
      c.picture(K.strip(species[i]), V(40, y), V(250, 30));
      c.label(notes[i], V(40, y - 24), 11, Ink, 280);
    }
  }
  K.register("spectro", function (c, focus, t) {
    if (focus === 0) spectroPrism(c, t); else if (focus === 1) spectroAbsorb(c); else if (focus === 2) spectroScale(c, t); else spectroExamples(c);
  });

  // ---- The Chelsea filter ----
  function chelseaStones(c, t) {
    var names = ["Rubis", "Émeraude", "Grenat almandin"];
    var plain = [C(0.85, 0.10, 0.20), C(0.12, 0.62, 0.36), C(0.40, 0.08, 0.12)];
    var glow = 0.7 + 0.3 * sin(t * 3);
    var filtered = [C(1, 0.15 * glow, 0.15 * glow), C(0.78, 0.28 * glow, 0.34 * glow), C(0.26, 0.17, 0.17)];
    var notes = ["rouge très vif", "rouge vif ou rose", "grisâtre à rouge foncé"];
    c.label("À l'œil nu", V(-190, 70), 13, Gold, 70);
    c.label("Sous le filtre", V(-190, -50), 13, Gold, 80);
    for (var i = 0; i < 3; i++) {
      var x = -80 + i * 110;
      c.rect(V(x, 70), V(54, 54), plain[i], 45);
      c.rect(V(x, 70), V(30, 30), mix(plain[i], WHITE, 0.3), 45);
      c.rect(V(x, -50), V(54, 54), filtered[i], 45);
      c.rect(V(x, -50), V(30, 30), mix(filtered[i], WHITE, 0.18), 45);
      c.label(names[i], V(x, 12), 12, Ink, 110);
      c.label(notes[i], V(x, -92), 11, Dim, 110);
    }
    c.label("Le chrome rougit sous le filtre ; le fer, non", V(0, -130), 14, Gold, 440);
  }
  function chelseaBlue(c, t) {
    var names = ["Spinelle bleu synthétique (cobalt)", "Saphir bleu (fer)"];
    var plain = [C(0.15, 0.30, 0.85), C(0.14, 0.26, 0.78)];
    var glow = 0.7 + 0.3 * sin(t * 3);
    var filtered = [C(1, 0.28 * glow, 0.22 * glow), C(0.05, 0.24, 0.11)];
    var notes = ["rouge vif, orange-rosé ou rose", "généralement vert foncé"];
    c.label("À l'œil nu", V(-195, 70), 13, Gold, 70);
    c.label("Sous le filtre", V(-195, -45), 13, Gold, 80);
    for (var i = 0; i < 2; i++) {
      var x = -60 + i * 150;
      c.rect(V(x, 70), V(58, 58), plain[i], 45);
      c.rect(V(x, 70), V(32, 32), mix(plain[i], WHITE, 0.3), 45);
      c.rect(V(x, -45), V(58, 58), filtered[i], 45);
      c.rect(V(x, -45), V(32, 32), mix(filtered[i], WHITE, 0.18), 45);
      c.label(names[i], V(x, 20), 11, Ink, 140);
      c.label(notes[i], V(x, -92), 11, Dim, 140);
    }
    c.label("Le cobalt rougit, le fer non", V(0, -130), 14, Gold, 440);
  }
  function chelseaTable(c) {
    var rows = [
      ["Rubis", "rouge à rouge très vif", 1.0], ["Émeraude", "rouge vif ou rose à verdâtre", 0.75], ["Alexandrite", "rose à rouge vif", 0.8],
      ["Démantoïde", "rose à rouge vif", 0.8], ["Spinelle bleu synth.", "rouge vif, orange-rosé ou rose", 0.9], ["Verre bleu (cobalt)", "rouge intense à rose", 0.9],
      ["Grenat almandin", "grisâtre à rouge foncé", 0.3], ["Verre rouge", "rouge foncé", 0.6], ["Saphir bleu", "généralement vert foncé", 0.0]
    ];
    rows.forEach(function (r, i) {
      var y = 140 - i * 33, chip = mix(C(0.08, 0.22, 0.12), C(1, 0.15, 0.15), r[2]);
      c.label(r[0], V(-135, y), 13, Ink, 160);
      c.rect(V(-30, y), V(36, 22), chip);
      c.label(r[1], V(125, y), 12, Gold, 220);
    });
  }
  K.register("chelsea", function (c, focus, t) {
    if (focus === 1) { chelseaStones(c, t); return; }
    if (focus === 2) { chelseaTable(c); return; }
    if (focus === 3) { chelseaBlue(c, t); return; }
    var left = -180, scale = 1.2, pulse = 0.6 + 0.4 * sin(t * 3);
    c.picture(K.strip(""), V(0, 40), V(360, 48));
    var dark = C(0.02, 0.02, 0.04, 0.9);
    c.rect(V((left + left + (565 - 400) * scale) / 2, 40), V((565 - 400) * scale, 48), dark);
    c.rect(V((left + (585 - 400) * scale + left + (680 - 400) * scale) / 2, 40), V((680 - 585) * scale, 48), dark);
    c.rect(V(left + (575 - 400) * scale, 40), V(3, 56 * pulse), C(1, 1, 1, 0.6));
    c.rect(V(left + (690 - 400) * scale, 40), V(3, 56 * pulse), C(1, 0.5, 0.5, 0.7));
    c.label("jaune-vert", V(left + (575 - 400) * scale, -6), 12, Gold, 90);
    c.label("rouge profond", V(left + (690 - 400) * scale - 20, -6), 12, C(1, 0.55, 0.50), 110);
    c.label("Le filtre Chelsea ne laisse passer que deux fenêtres de couleur", V(0, 120), 14, Gold, 460);
    c.label("tout le reste du spectre est arrêté", V(0, -60), 13, Ink, 400);
    c.label("Une pierre qui renvoie du rouge profond y semblera rouge", V(0, -100), 13, Dim, 440);
  });

  // ---- The UV cabinet ----
  var UvViolet = C(0.55, 0.28, 0.95), UvLight = K.brighter(UvViolet, 0.3);
  function uvScale(c, t) {
    var left = -200, width = 400;
    function X(w) { return left + (w - 200) / 500 * width; }
    c.rect(V(X(300), 40), V(X(400) - X(200), 48), C(0.10, 0.05, 0.20));
    c.picture(K.strip(""), V((X(400) + X(700)) / 2, 40), V(X(700) - X(400), 48));
    var pulse = 0.55 + 0.45 * sin(t * 4);
    [254, 365].forEach(function (nm) {
      c.line(V(X(nm), 8), V(X(nm), 72), 3, alpha(UvViolet, pulse));
      c.label(nm + " nm", V(X(nm), -8), 12, UvLight, 70);
    });
    c.label("UV courts", V(X(254), -26), 12, Gold, 80);
    c.label("UV longs", V(X(365), -26), 12, Gold, 80);
    c.label("lumière visible", V((X(400) + X(700)) / 2, -8), 12, Ink, 130);
    c.label("ultraviolet : invisible", V(X(300) - 30, 82), 12, UvLight, 160);
    c.label("Des rayons que l'œil ne voit pas", V(0, 130), 15, Gold, 440);
    c.label("Ne jamais regarder une lampe UV en face", V(0, -80), 13, C(1, 0.55, 0.50), 400);
  }
  function uvGlow(c, t) {
    var phase = (t * 0.8) % 1, stone = V(0, 0);
    c.rect(stone, V(70, 70), C(0.18, 0.02, 0.05), 45);
    var glow = M.clamp01(sin(phase * PI)) * 0.7 + 0.3;
    c.rect(stone, V(70, 70), C(1, 0.15, 0.12, glow), 45);
    c.circle(stone, 12 + 6 * glow, C(1, 0.55, 0.50, 0.35 * glow));
    var x = M.lerp(-190, -45, phase);
    c.line(V(-190, 0), V(-45, 0), 2, alpha(UvViolet, 0.35));
    c.circle(V(x, 0), 7, UvViolet);
    for (var i = -1; i <= 1; i++) c.line(V(45, i * 12), V(150, i * 48), 2, C(1, 0.25, 0.2, 0.5 * glow));
    c.label("UV invisibles", V(-120, -34), 13, UvLight, 120);
    c.label("lueur visible (ici, rouge)", V(115, -86), 13, C(1, 0.55, 0.50), 190);
    c.label("La pierre absorbe l'UV et le rend en lumière visible", V(0, 130), 14, Gold, 460);
    c.label("c'est la fluorescence", V(0, -130), 14, Ink, 300);
  }
  function uvLamps(c) {
    var names = ["Saphir bleu naturel", "Saphir bleu de synthèse"], dark = C(0.04, 0.05, 0.14), green = C(0.30, 1, 0.48);
    var longs = [dark, dark], shorts = [dark, green];
    c.label("UV longs", V(-95, 120), 13, Gold, 90);
    c.label("UV courts", V(65, 120), 13, Gold, 90);
    for (var i = 0; i < 2; i++) {
      var y = 60 - i * 100;
      c.label(names[i], V(-165, y), 12, Ink, 100);
      c.rect(V(-95, y), V(54, 54), longs[i], 45);
      c.rect(V(65, y), V(54, 54), shorts[i], 45);
    }
    c.label("rien", V(-95, -108), 12, Dim, 100);
    c.label("vert : synthèse", V(65, -108), 12, Dim, 150);
    c.label("Essayez toujours les deux lampes", V(0, -135), 14, Gold, 440);
  }
  function uvAfterglow(c, t) {
    var cycle = (t * 0.4) % 1, on = cycle < 0.5, sinceOff = on ? 0 : (cycle - 0.5) * 2;
    var fluo = on ? 1 : 0, phos = on ? 1 : Math.pow(1 - sinceOff, 2);
    c.label(on ? "Lampe allumée" : "Lampe éteinte", V(0, 128), 15, on ? UvLight : Dim, 300);
    var labels = ["Fluorescence", "Phosphorescence"], values = [fluo, phos], colors = [C(0.30, 0.50, 1), C(0.78, 0.88, 1)];
    for (var i = 0; i < 2; i++) {
      var x = -80 + i * 160;
      c.rect(V(x, 20), V(70, 70), mix(C(0.03, 0.03, 0.06), colors[i], values[i]), 45);
      c.label(labels[i], V(x, -50), 14, Ink, 150);
      c.label(i === 0 ? "s'arrête aussitôt" : "continue quelques secondes", V(x, -72), 11, Dim, 170);
    }
    c.label("Diamant naturel : rarement phosphorescent", V(0, -112), 13, Gold, 440);
    c.label("Diamant HPHT : souvent", V(0, -132), 13, Gold, 440);
  }
  function uvExamples(c) {
    var rows = [
      ["Rubis (naturel ou synth.)", "rouge, aux deux lampes", C(1, 0.16, 0.10)], ["Saphir bleu naturel", "aucune réaction", C(0.04, 0.05, 0.14)],
      ["Saphir bleu synthétique", "vert aux UV courts", C(0.30, 1, 0.48)], ["Diamant", "variable ; bleu le plus commun", C(0.30, 0.50, 1)],
      ["Fluorite", "bleu ou violet", C(0.45, 0.40, 1)], ["Spinelle bleu synth.", "rouge aux UV longs", C(1, 0.16, 0.10)],
      ["Émeraude", "rouge", C(1, 0.23, 0.16)], ["Kunzite", "orange aux UV longs", C(1, 0.55, 0.12)]
    ];
    rows.forEach(function (r, i) {
      var y = 135 - i * 35;
      c.label(r[0], V(-135, y), 13, Ink, 160);
      c.rect(V(-30, y), V(36, 22), r[2]);
      c.label(r[1], V(125, y), 12, Gold, 210);
    });
  }
  K.register("uv", function (c, focus, t) {
    if (focus === 0) uvScale(c, t); else if (focus === 1) uvGlow(c, t); else if (focus === 2) uvLamps(c); else if (focus === 3) uvAfterglow(c, t); else uvExamples(c);
  });

  // ---- The dichroscope ----
  var RubyA = C(0.82, 0.10, 0.20), RubyB = C(0.95, 0.42, 0.18), Blue = C(0.18, 0.28, 0.85), Violet = C(0.55, 0.28, 0.80), RedBrown = C(0.55, 0.25, 0.20),
    PaleYellow = C(0.90, 0.85, 0.55);
  function windowBox(c, center, size, color, label) {
    c.rect(center, V(size[0] + 6, size[1] + 6), C(0.75, 0.78, 0.85));
    c.rect(center, size, color);
    if (label) c.label(label, add(center, V(-size[0] / 2 - 16, 0)), 14, Ink, 24);
  }
  function pleoCrystal(c, t) {
    var spin = 0.5 + 0.5 * sin(t * 1.1), color = mix(RubyA, RubyB, spin), width = M.lerp(50, 96, spin);
    c.circle(V(-175, 0), 18, Ink);
    c.circle(V(-170, 0), 7, Dark);
    c.label("l'œil", V(-175, -34), 12, Dim, 60);
    c.line(V(-150, 0), V(-70, 0), 2, C(1, 1, 1, 0.5));
    c.rect(V(0, 0), V(width, 150), color);
    c.rect(V(0, 0), V(width * 0.55, 120), mix(color, WHITE, 0.25));
    c.label(spin < 0.5 ? "vue dans cette direction : rouge" : "vue dans cette direction : rouge orangé", V(140, 40), 14, Gold, 200);
    c.label("Le cristal tourne : sa couleur change", V(0, 148), 15, Gold, 440);
    c.label("c'est le pléochroïsme", V(0, -140), 14, Ink, 300);
  }
  function calciteSplit(c, t) {
    var stone = V(-190, 0), lens = V(10, 0), view = V(165, 0), line = C(0.12, 0.14, 0.20), s = 0.5 + 0.5 * sin(t * 1.3);
    var colorA = mix(RubyA, RubyB, s), colorB = mix(RubyB, RubyA, s), black = C(0.10, 0.11, 0.14);
    c.rect(add(stone, V(0, -4)), V(80, 80), C(1, 1, 0.92));
    c.rect(stone, V(40, 40), Stone, 45);
    c.label("pierre sur la lampe", add(stone, V(0, -58)), 12, Ink, 130);
    c.line(add(stone, V(42, 12)), add(lens, V(-40, 28)), 3, colorA);
    c.line(add(stone, V(42, -12)), add(lens, V(-40, -28)), 3, colorB);
    c.circle(lens, 60, black);
    c.circle(lens, 55, C(0.80, 0.84, 0.90));
    for (var y = 10; y <= 46; y += 12) c.rect(add(lens, V(0, y)), V(2 * Math.sqrt(55 * 55 - y * y) - 6, 2), line);
    for (var x = -42; x <= 42; x += 14) {
      var h = Math.sqrt(55 * 55 - x * x) - 6;
      c.rect(add(lens, V(x, -h / 2 - 2)), V(2, h), line);
    }
    c.rect(lens, V(110, 3), black);
    c.label("filtre ↔", add(lens, V(0, 76)), 12, Gold, 90);
    c.label("filtre ↕", add(lens, V(0, -76)), 12, Gold, 90);
    c.circle(view, 52, black);
    c.circle(view, 48, colorA);
    for (var yy = -2; yy > -48; yy -= 4) c.rect(add(view, V(0, yy - 2)), V(2 * Math.sqrt(48 * 48 - yy * yy), 4), colorB);
    c.rect(view, V(100, 3), black);
    c.label("A", add(view, V(70, 24)), 14, Ink, 24);
    c.label("B", add(view, V(70, -24)), 14, Ink, 24);
    c.label("ce que voit l'œil", add(view, V(0, -72)), 12, Gold, 130);
    c.label("Deux filtres polarisants collés à angle droit", V(0, 148), 15, Gold, 450);
    c.label("Chaque moitié montre l'une des deux couleurs de la pierre", V(0, -140), 13, Ink, 460);
  }
  function pleoFamilies(c, t) {
    var titles = ["Isotrope", "Uniaxe", "Biaxe"], notes = ["la même couleur", "2 couleurs", "3 couleurs"];
    var wave = 0.5 + 0.5 * sin(t * 1.6), step = Math.floor(t / 1.2) % 3, smooth = M.smoothStep(0, 1, M.repeat(t / 1.2, 1));
    var three = [Blue, Violet, RedBrown];
    for (var i = 0; i < 3; i++) {
      var x = (i - 1) * 158, a, b;
      if (i === 0) { a = RubyA; b = RubyA; }
      else if (i === 1) { a = mix(RubyA, RubyB, wave); b = mix(RubyB, RubyA, wave); }
      else { a = mix(three[step], three[(step + 1) % 3], smooth); b = mix(three[(step + 1) % 3], three[(step + 2) % 3], smooth); }
      c.rect(V(x, 0), V(146, 250), C(1, 1, 1, 0.07));
      c.label(titles[i], V(x, 100), 17, Gold, 140);
      windowBox(c, V(x + 10, 40), V(60, 46), a, "");
      windowBox(c, V(x + 10, -20), V(60, 46), b, "");
      c.label(notes[i], V(x, -88), 14, Ink, 140);
    }
    c.label("Cubique : aucune   ·   uniaxe : dichroïsme   ·   biaxe : trichroïsme", V(0, -140), 12, Dim, 470);
  }
  function pleoRotation(c, t) {
    var angle = t * 40, s = 0.5 + 0.5 * sin(t * 1.4), colorA = mix(RubyA, RubyB, s), colorB = mix(RubyB, RubyA, s);
    c.rect(V(-135, 0), V(96, 96), Stone, 45 + angle);
    c.rect(V(-135, 0), V(54, 54), StoneLight, 45 + angle);
    c.circle(V(-135, 0), 6, Dark);
    c.label("on tourne la pierre", V(-135, -85), 13, Gold, 150);
    c.line(V(-70, 0), V(80, 0), 2, C(1, 1, 1, 0.4));
    windowBox(c, V(150, 34), V(64, 56), colorA, "A");
    windowBox(c, V(150, -34), V(64, 56), colorB, "B");
    var different = Math.abs(s - 0.5) > 0.3;
    c.label(different ? "A et B sont différentes : pléochroïsme !" : "A et B se ressemblent : on continue à tourner", V(10, 125), 14, different ? Gold : Dim, 440);
    c.label("Regardez dans plusieurs directions, sur un tour complet", V(0, -140), 13, Ink, 460);
  }
  function pleoDevices(c) {
    var steel = C(0.60, 0.64, 0.72), black = C(0.10, 0.11, 0.14), rhomb = C(0.55, 0.85, 1, 0.6), lens = V(-95, 62);
    c.rect(V(25, 62), V(190, 52), black, -8);
    c.circle(lens, 46, black);
    c.circle(lens, 38, C(0.80, 0.84, 0.90));
    c.rect(add(lens, V(0, -20)), V(76, 36), C(0.58, 0.62, 0.70));
    for (var i = -2; i <= 2; i++) c.rect(add(lens, V(i * 13, -20)), V(2, 30), black);
    for (var j = 0; j < 3; j++) c.rect(add(lens, V(0, 12 + j * 9)), V(54 - j * 12, 2), black);
    c.line(add(lens, V(-38, 0)), add(lens, V(38, 0)), 2, WHITE);
    c.circle(V(100, 66), 5, steel);
    c.label("deux filtres polarisants collés à angle droit", V(40, 112), 13, Gold, 340);
    c.label("Dichroscope de Londres : celui du laboratoire", V(0, 14), 16, Gold, 440);
    c.rect(V(-110, -62), V(110, 24), steel);
    c.circle(V(-160, -62), 13, C(0.30, 0.33, 0.40));
    c.rect(V(-110, -62), V(30, 14), rhomb, 18);
    c.rect(V(-55, -62), V(10, 18), C(0.80, 0.90, 1));
    c.label("Dichroscope à calcite (un cristal, deux images)", V(-95, -96), 12, Ink, 230);
    c.rect(V(135, -66), V(52, 40), C(1, 1, 0.92));
    c.rect(V(135, -92), V(64, 8), black);
    c.label("lampe du laboratoire", V(135, -112), 12, Ink, 150);
    c.label("Même but : séparer les deux directions de vibration", V(0, -140), 13, Dim, 460);
  }
  function pleoExamples(c) {
    var rows = [["Rubis", "r", "o"], ["Saphir bleu", "b", "g"], ["Émeraude", "g2", "y"], ["Tanzanite", "b", "v", "rb"], ["Iolite", "b", "v", "py"], ["Spinelle rouge", "r"]];
    var map = { r: RubyA, o: RubyB, b: Blue, g: C(0.15, 0.55, 0.65), g2: C(0.10, 0.60, 0.50), y: C(0.60, 0.75, 0.20), v: Violet, rb: RedBrown, py: PaleYellow };
    rows.forEach(function (row, i) {
      var y = 125 - i * 46;
      c.label(row[0], V(-135, y), 14, Ink, 140);
      for (var k = 1; k < row.length; k++) c.rect(V(-20 + (k - 1) * 52, y), V(44, 30), map[row[k]] || PaleYellow);
      if (row.length === 2) c.label("aucun changement", V(95, y), 12, Dim, 140);
    });
  }
  K.register("dichro", function (c, focus, t) {
    if (focus === 0) pleoCrystal(c, t); else if (focus === 1) calciteSplit(c, t); else if (focus === 2) pleoFamilies(c, t);
    else if (focus === 3) pleoRotation(c, t); else if (focus === 4) pleoDevices(c); else pleoExamples(c);
  });
})();
