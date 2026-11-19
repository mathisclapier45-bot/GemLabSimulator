// Figures of the lessons, second half (port of LessonFigures.cs): tenacity, stability, loupe, carat balance, hydrostatic balance, crystal systems, polariscope.
(function () {
  "use strict";
  var K = window.LessonKit, M = K.M, C = K.C, V = K.V, alpha = K.alpha, mix = K.mix, WHITE = K.WHITE, add = K.add, mul = K.mul;
  var P = K.palette, Ink = P.Ink, Dim = P.Dim, Gold = P.Gold, Cyan = P.Cyan, Stone = P.Stone, StoneLight = P.StoneLight, Dark = P.Dark, Wood = P.Wood, Water = P.Water;
  var sin = Math.sin, cos = Math.cos, PI = Math.PI;

  // ---- Tenacity ----
  var TenacityNames = ["Exceptionnelle", "Très bonne", "Bonne", "Moyenne", "Faible"];
  var TenacityExamples = ["jade néphrite", "jadéite, corindon", "diamant, quartz", "émeraude, topaze", "opale, fluorite, kunzite"];
  K.register("tenacity", function (c, focus, t) {
    var i;
    if (focus === 3) {
      for (i = 0; i < 5; i++) {
        var y = 100 - i * 48, width = 260 - i * 40;
        c.label(TenacityNames[i], V(-165, y), 13, Ink, 110);
        c.rect(V(-95 + width / 2, y), V(width, 30), mix(C(0.35, 0.75, 0.45), C(0.85, 0.35, 0.35), i / 4));
        c.label(TenacityExamples[i], V(-95 + width / 2, y), 12, WHITE, width - 6);
      }
      c.label("Plus la barre est longue, plus la pierre encaisse les chocs", V(0, -145), 13, Gold, 460);
      return;
    }
    var cycle = t % 4.5, stonePos = V(0, -40), hit = cycle > 0.7 && cycle < 4, baseColor = focus === 4 ? C(0.30, 0.62, 0.42) : Stone;
    var hammerY = cycle < 0.7 ? M.lerp(135, 9, cycle / 0.7) : M.lerp(9, 135, M.clamp01((cycle - 0.9) / 0.9));
    if (focus === 4 && cycle >= 0.7) hammerY = M.lerp(9, 135, M.clamp01((cycle - 0.75) / 0.5));
    c.rect(V(0, hammerY + 60), V(10, 90), Wood);
    c.rect(V(0, hammerY), V(76, 28), C(0.65, 0.68, 0.74));

    if (focus === 1) {
      var shift = hit ? Math.min(10, (cycle - 0.7) * 40) : 0;
      c.rect(add(stonePos, V(-31 - shift, 0)), V(58, 70), baseColor);
      c.rect(add(stonePos, V(31 + shift, 0)), V(58, 70), baseColor);
      if (!hit) for (i = -2; i <= 2; i++) c.line(add(stonePos, V(i * 22, -34)), add(stonePos, V(i * 22, 34)), 1.5, C(1, 1, 1, 0.35));
      c.label("Clivage : la pierre se fend selon un plan lisse", V(0, -122), 14, Gold, 450);
      return;
    }
    c.rect(stonePos, V(120, 70), baseColor);
    if (hit && focus === 0) {
      var top = add(stonePos, V(0, 34));
      c.line(top, add(stonePos, V(-20, 5)), 2.5, Dark);
      c.line(add(stonePos, V(-20, 5)), add(stonePos, V(-34, -22)), 2.5, Dark);
      c.line(top, add(stonePos, V(18, 4)), 2.5, Dark);
      c.line(add(stonePos, V(18, 4)), add(stonePos, V(30, -24)), 2.5, Dark);
      c.label("Très dure, mais elle casse au choc", V(0, -122), 14, Gold, 450);
    } else if (hit && focus === 2) {
      var corner = add(stonePos, V(42, 12));
      for (var r = 30; r >= 8; r -= 7) { c.circle(corner, r, Dark); c.circle(corner, r - 2, baseColor); }
      c.label("Cassure en coquille : un éclat irrégulier", V(0, -122), 14, Gold, 450);
    } else if (focus === 4) {
      if (hit && cycle < 2.2) c.label("Rebond : rien ne se passe", V(0, 6), 12, Ink, 200);
      c.label("Une pierre tenace encaisse le choc sans casser", V(0, -122), 14, Gold, 450);
    } else if (focus === 0) {
      c.label("Dureté et ténacité ne sont pas la même chose", V(0, -122), 14, Gold, 450);
    } else if (focus === 2) {
      c.label("Cassure : la pierre se brise de façon irrégulière", V(0, -122), 14, Gold, 450);
    }
  });

  // ---- Stability ----
  K.register("stability", function (c, focus, t) {
    var i;
    if (focus === 3) {
      var titles = ["Lumière", "Chaleur", "Produits chimiques\net humidité"];
      var colors = [C(0.95, 0.80, 0.30), C(0.90, 0.45, 0.25), C(0.35, 0.65, 0.90)];
      for (i = 0; i < 3; i++) {
        var x = (i - 1) * 158;
        c.rect(V(x, 0), V(146, 220), alpha(colors[i], 0.25));
        c.circle(V(x, 50), 34, colors[i]);
        c.label(titles[i], V(x, -50), 15, Ink, 140);
      }
      c.label("La stabilité : tenir dans le temps", V(0, 140), 15, Gold, 440);
      return;
    }
    var stone = V(0, -25), phase = M.clamp01((t % 6) / 5), rich = Stone, pale = C(0.78, 0.74, 0.82);
    if (focus === 0) {
      var sun = V(-170, 100), yellow = C(1, 0.88, 0.35);
      c.circle(sun, 26, yellow);
      for (i = 0; i < 10; i++) {
        var a = i * 36 * M.deg, d = V(cos(a), sin(a));
        c.line(add(sun, mul(d, 34)), add(sun, mul(d, 48)), 3, yellow);
      }
      c.line(add(sun, V(30, -22)), add(stone, V(-30, 30)), 3, C(1, 0.95, 0.6, 0.5));
      c.rect(stone, V(100, 100), mix(rich, pale, phase), 45);
      c.rect(V(-150, -120), V(44, 30), rich);
      c.rect(V(150, -120), V(44, 30), mix(rich, pale, phase));
      c.label("avant", V(-150, -146), 12, Ink, 70);
      c.label("après", V(150, -146), 12, Ink, 70);
      c.label("La lumière fait pâlir certaines couleurs", V(0, 148), 14, Gold, 440);
    } else if (focus === 1) {
      c.rect(stone, V(100, 100), mix(rich, C(0.45, 0.30, 0.45), phase), 45);
      var flicker = 1 + 0.12 * sin(t * 14);
      c.circle(V(0, -100), 26 * flicker, C(0.95, 0.45, 0.15));
      c.circle(V(0, -94), 16 * flicker, C(1, 0.82, 0.30));
      if (phase > 0.6) {
        c.line(add(stone, V(-10, 40)), add(stone, V(6, 5)), 3, Dark);
        c.line(add(stone, V(6, 5)), add(stone, V(-4, -25)), 3, Dark);
      }
      c.rect(V(150, -20), V(16, 150), C(0.85, 0.85, 0.90));
      c.rect(V(150, -95 + 75 * phase), V(10, 150 * phase), C(0.90, 0.25, 0.20));
      c.label("La chaleur ternit, fend ou ramollit", V(0, 148), 14, Gold, 440);
    } else {
      c.rect(stone, V(100, 100), rich, 45);
      var dropY = M.lerp(140, 20, M.clamp01((t % 3) / 1.2));
      if ((t % 3) < 1.4) c.circle(V(0, dropY), 10, C(0.35, 0.75, 0.95));
      var damage = M.clamp01(((t % 6) - 1.2) / 3.5);
      for (i = 0; i < 6; i++) {
        var off = V(sin(i * 2.1) * 28, cos(i * 1.7) * 24 - 25);
        c.circle(off, 2 + 6 * damage, C(0.88, 0.86, 0.80, 0.85));
      }
      c.label("Acides, parfums, nettoyants, eau : la surface est attaquée", V(0, 148), 14, Gold, 450);
    }
  });

  // ---- The loupe ----
  var Smudges = [V(-22, 18), V(12, 26), V(26, -6), V(-8, -14), V(-30, -22), V(6, 4), V(22, -28)];
  K.register("loupe", function (c, focus, t) {
    var lens = focus === 2, stonePos = lens ? V(-125, 10) : V(0, 10), scale = lens ? 0.6 : 1, spin = focus === 3 ? t * 45 : 0, i;
    c.rect(stonePos, V(110 * scale, 110 * scale), Stone, 45 + spin);
    c.rect(stonePos, V(70 * scale, 70 * scale), StoneLight, 45 - spin);
    c.rect(stonePos, V(26 * scale, 26 * scale), Stone, 45 + spin * 2);
    var dirt = 0;
    if (focus === 0) dirt = 1; else if (focus === 1) dirt = M.clamp01(1 - (t % 4) / 3);
    for (i = 0; i < Smudges.length; i++) c.circle(add(stonePos, mul(Smudges[i], scale)), (11 + (i % 3) * 3) * scale, C(1, 1, 1, 0.55 * dirt));
    if (focus === 1) {
      var wipe = sin(t * 6) * 34;
      c.rect(add(stonePos, V(wipe, 8)), V(72, 36), C(0.92, 0.76, 0.34), 8);
      c.rect(add(stonePos, V(wipe, 8)), V(72, 5), C(0.70, 0.54, 0.20), 8);
      c.label("Chamoisine", V(0, -100), 14, Gold);
    } else if (focus === 0) c.label("Pierre sale : voile blanchâtre en surface", V(0, -100), 14, Gold, 380);
    else if (focus === 3) c.label("On tourne la pierre dans tous les sens", V(0, -100), 14, Gold, 380);
    if (lens) {
      var lc = V(75, 10), radius = 105;
      c.circle(lc, radius, C(0.75, 0.90, 1, 0.16));
      for (i = 0; i < 36; i++) {
        var a = i * 10 * M.deg;
        c.circle(add(lc, V(cos(a) * radius, sin(a) * radius)), 3.5, Gold);
      }
      c.line(add(lc, V(radius * 0.7, -radius * 0.7)), add(lc, V(radius * 1.25, -radius * 1.25)), 8, Wood);
      c.rect(lc, V(150, 150), Stone, 45);
      c.rect(lc, V(98, 98), StoneLight, 45);
      c.line(add(lc, V(-40, 18)), add(lc, V(18, 34)), 2, C(0.30, 0.15, 0.40));
      c.line(add(lc, V(-10, -30)), add(lc, V(30, -8)), 2, C(1, 1, 1, 0.8));
      c.circle(add(lc, V(22, 22)), 4, C(0.20, 0.10, 0.30));
      c.circle(add(lc, V(-24, -12)), 3, WHITE);
      c.label("Loupe 10×", V(75, -118), 14, Gold, 200);
    }
  });

  // ---- The carat balance ----
  K.register("carat", function (c, focus, t) {
    var pivot = V(0, 20), tilt = 12 * Math.exp(-0.6 * (t % 7)) * cos(t * 5), dir = V(cos(tilt * M.deg), sin(tilt * M.deg));
    var left = add(pivot, mul(dir, -130)), right = add(pivot, mul(dir, 130));
    c.rect(V(0, -95), V(170, 14), Wood);
    c.rect(V(0, -40), V(10, 110), Wood);
    c.line(left, right, 6, C(0.75, 0.70, 0.55));
    c.circle(pivot, 8, Gold);
    [left, right].forEach(function (end) {
      var pan = add(end, V(0, -62));
      c.line(end, add(pan, V(-38, 6)), 1.5, Dim);
      c.line(end, add(pan, V(38, 6)), 1.5, Dim);
      c.rect(pan, V(88, 8), C(0.80, 0.80, 0.85));
    });
    var leftPan = add(left, V(0, -62)), rightPan = add(right, V(0, -62));
    if (focus === 0) {
      c.circle(add(leftPan, V(0, 12)), 10, C(0.40, 0.24, 0.12));
      c.circle(add(rightPan, V(0, 12)), 14, Stone);
      c.label("graine de caroube", add(leftPan, V(0, 44)), 12, Gold, 130);
      c.label("1 carat", add(rightPan, V(0, 44)), 12, Gold, 130);
      c.label("1 ct  =  0,2 g", V(0, -135), 18, Ink, 300);
    } else {
      c.circle(add(leftPan, V(0, 20)), 18, Stone);
      c.rect(add(rightPan, V(0, 12)), V(18, 12), Dim);
      c.rect(V(0, -135), V(150, 32), Dark);
      var showGrams = focus >= 2 && (t % 3) > 1.5;
      c.label(showGrams ? "0,470 g" : "2,35 ct", V(0, -135), 18, C(0.40, 1, 0.55), 150);
      if (focus >= 2) c.label("1 g = 5 ct", V(150, -135), 14, Gold, 140);
    }
  });

  // ---- The hydrostatic balance ----
  K.register("hydro", function (c, focus, t) {
    var airWeight = 2.00, waterWeight = 1.45, green = C(0.40, 1, 0.55);
    c.rect(V(70, -55), V(136, 140), Water);
    c.line(V(2, 20), V(2, -125), 3, Ink);
    c.line(V(138, 20), V(138, -125), 3, Ink);
    c.line(V(2, -125), V(138, -125), 3, Ink);
    c.line(V(2, 15), V(138, 15), 1.5, alpha(Cyan, 0.7));
    var progress = focus === 0 ? 0 : focus === 1 ? M.smoothStep(0, 1, M.clamp01((t % 6) / 3.5)) : 1;
    var y = M.lerp(70, -50, progress), x = 70;
    c.rect(V(x, 155), V(60, 12), Wood);
    c.line(V(x, 150), V(x, y + 18), 2, Dim);
    c.rect(V(x, y - 20), V(44, 5), C(0.80, 0.80, 0.85));
    c.circle(V(x, y), 16, Stone);
    var underwater = y < 15;
    if (underwater) {
      c.line(V(x + 40, y), V(x + 40, y + 40), 3, green);
      c.label("poussée de l'eau", V(x + 60, y + 62), 12, green, 120);
    }
    var shown = underwater ? waterWeight : airWeight;
    c.rect(V(-135, 60), V(130, 34), Dark);
    c.label(K.t(underwater ? "P eau" : "P air") + " = " + shown.toFixed(2) + " ct", V(-135, 60), 15, green, 130);
    if (focus >= 2) {
      c.label("d  =  P air  ÷  (P air − P eau)", V(-100, -30), 15, Ink, 200);
      c.label("d  =  2,00 ÷ 0,55  ≈  3,64", V(-100, -70), 15, Gold, 200);
    }
  });

  // ---- The crystal systems ----
  var SystemNames = ["Système cubique", "Système quadratique", "Système hexagonal", "Système trigonal", "Système orthorhombique", "Système monoclinique", "Système triclinique"];
  var SystemCaptions = ["a = b = c   ·   angles de 90°", "a = b ≠ c   ·   angles de 90°", "a₁ = a₂ = a₃ à 120°   ·   c perpendiculaire", "trois axes égaux, inclinés de la même façon",
    "a ≠ b ≠ c   ·   angles de 90°", "a ≠ b ≠ c   ·   un angle différent de 90°", "a ≠ b ≠ c   ·   aucun angle de 90°"];
  var AxisA = C(1, 0.45, 0.40), AxisB = C(0.45, 0.95, 0.50), AxisC = C(0.45, 0.70, 1);

  function project(p, spin, scale, origin) {
    var cs = cos(spin), sn = sin(spin), x = p[0] * cs - p[1] * sn, y = p[0] * sn + p[1] * cs;
    return V(origin[0] + x * scale, origin[1] + (y * 0.42 + p[2] * 0.92) * scale);
  }
  function v3add(a, b) { return [a[0] + b[0], a[1] + b[1], a[2] + b[2]]; }
  function v3mul(a, k) { return [a[0] * k, a[1] * k, a[2] * k]; }

  function parallelepiped(c, system, origin, spin, scale) {
    var a, b, d;
    switch (system) {
      case 1: a = [1, 0, 0]; b = [0, 1, 0]; d = [0, 0, 1.7]; break;
      case 3: a = [0.85, 0, 0.75]; b = [-0.425, 0.736, 0.75]; d = [-0.425, -0.736, 0.75]; break;
      case 4: a = [1.6, 0, 0]; b = [0, 1, 0]; d = [0, 0, 1.3]; break;
      case 5: a = [1.5, 0, 0]; b = [0, 1, 0]; d = [0.5, 0, 1.2]; break;
      case 6: a = [1.5, 0.25, 0.1]; b = [0.2, 1, 0.15]; d = [0.45, 0.3, 1.2]; break;
      default: a = [1.2, 0, 0]; b = [0, 1.2, 0]; d = [0, 0, 1.2];
    }
    var middle = v3mul(v3add(v3add(a, b), d), 0.5), edge = C(0.85, 0.92, 1, 0.9);
    for (var i = 0; i < 2; i++) for (var j = 0; j < 2; j++) for (var k = 0; k < 2; k++) {
      var from = v3add(v3add(v3add(v3mul(a, i), v3mul(b, j)), v3mul(d, k)), v3mul(middle, -1));
      if (i === 0) c.line(project(from, spin, scale, origin), project(v3add(from, a), spin, scale, origin), 2, edge);
      if (j === 0) c.line(project(from, spin, scale, origin), project(v3add(from, b), spin, scale, origin), 2, edge);
      if (k === 0) c.line(project(from, spin, scale, origin), project(v3add(from, d), spin, scale, origin), 2, edge);
    }
    var tipA = v3mul(a, 0.95), tipB = v3mul(b, 0.95), tipC = v3mul(d, 0.95), center = project([0, 0, 0], spin, scale, origin);
    c.line(center, project(tipA, spin, scale, origin), 3, AxisA);
    c.line(center, project(tipB, spin, scale, origin), 3, AxisB);
    c.line(center, project(tipC, spin, scale, origin), 3, AxisC);
    c.label("a", project(v3mul(tipA, 1.12), spin, scale, origin), 15, AxisA, 30);
    c.label("b", project(v3mul(tipB, 1.12), spin, scale, origin), 15, AxisB, 30);
    c.label("c", project(v3mul(tipC, 1.12), spin, scale, origin), 15, AxisC, 30);
  }

  function hexPrism(c, origin, radius, halfHeight, spin, scale, axes) {
    var edge = C(0.85, 0.92, 1, 0.9), k;
    for (k = 0; k < 6; k++) {
      var a0 = k * 60 * M.deg, a1 = (k + 1) * 60 * M.deg;
      var p0 = [cos(a0) * radius, sin(a0) * radius, 0], p1 = [cos(a1) * radius, sin(a1) * radius, 0], up = [0, 0, halfHeight], down = [0, 0, -halfHeight];
      c.line(project(v3add(p0, up), spin, scale, origin), project(v3add(p1, up), spin, scale, origin), 2, edge);
      c.line(project(v3add(p0, down), spin, scale, origin), project(v3add(p1, down), spin, scale, origin), 2, edge);
      c.line(project(v3add(p0, up), spin, scale, origin), project(v3add(p0, down), spin, scale, origin), 2, edge);
    }
    if (!axes) return;
    var center = project([0, 0, 0], spin, scale, origin), colors = [AxisA, AxisB, C(1, 0.75, 0.35)], names = ["a₁", "a₂", "a₃"];
    for (var i = 0; i < 3; i++) {
      var angle = i * 120 * M.deg, tip = [cos(angle) * radius * 1.2, sin(angle) * radius * 1.2, 0];
      // The axis goes right through the crystal: out on both sides (the far side is the negative end).
      c.line(project(v3mul(tip, -1), spin, scale, origin), project(tip, spin, scale, origin), 3, colors[i]);
      c.label(names[i], project(v3mul(tip, 1.15), spin, scale, origin), 15, colors[i], 40);
      c.label("\u2212" + names[i], project(v3mul(tip, -1.2), spin, scale, origin), 14, colors[i], 40);
    }
    var top = [0, 0, halfHeight * 1.35];
    c.line(project(v3mul(top, -1), spin, scale, origin), project(top, spin, scale, origin), 3, AxisC);
    c.label("c", project(v3mul(top, 1.12), spin, scale, origin), 15, AxisC, 30);
    c.label("\u2212c", project(v3mul(top, -1.15), spin, scale, origin), 14, AxisC, 30);
  }

  function habits(c, t) {
    var grow = M.smoothStep(0.25, 1, M.clamp01((t % 6) / 4)), spin = t * 0.6;
    hexPrism(c, V(-155, 5), 0.28, 1.35 * grow, spin, 62, false);
    hexPrism(c, V(0, 5), 0.55, 0.8 * grow, spin, 62, false);
    hexPrism(c, V(155, 5), 0.95, 0.18 * grow, spin, 62, false);
    c.label("aiguille", V(-155, -115), 15, Gold, 120);
    c.label("prisme", V(0, -115), 15, Gold, 120);
    c.label("tablette", V(155, -115), 15, Gold, 120);
    c.label("croissance rapide selon c", V(-155, -142), 11, AxisC, 150);
    c.label("croissance équilibrée", V(0, -142), 11, Dim, 150);
    c.label("croissance rapide selon a", V(155, -142), 11, AxisA, 150);
    c.label("La vitesse de croissance dépend de la direction", V(0, 148), 15, Ink, 440);
  }
  function families(c) {
    var titles = ["Isotrope", "Uniaxe", "Biaxe"];
    var systems = ["cubique", "quadratique\nhexagonal\ntrigonal", "orthorhombique\nmonoclinique\ntriclinique"];
    var indices = ["1 indice", "2 indices\n(nε, nω)", "3 indices\n(nα, nβ, nγ)"];
    var axes = ["aucun axe optique", "1 axe optique", "2 axes optiques"];
    var colors = [C(0.45, 0.85, 0.55), C(0.95, 0.75, 0.30), C(0.80, 0.55, 0.95)];
    for (var i = 0; i < 3; i++) {
      var x = (i - 1) * 158;
      c.rect(V(x, 0), V(146, 300), alpha(colors[i], 0.22));
      c.rect(V(x, 128), V(146, 38), colors[i]);
      c.label(titles[i], V(x, 128), 17, WHITE, 140);
      c.label(systems[i], V(x, 62), 13, Ink, 140);
      c.label(indices[i], V(x, -20), 13, Gold, 140);
      c.label(axes[i], V(x, -90), 13, Ink, 140);
    }
  }
  K.register("systems", function (c, focus, t) {
    var spin = t * 0.6;
    if (focus === 7) { habits(c, t); return; }
    if (focus === 8) { families(c); return; }
    var index = Math.max(0, Math.min(6, focus));
    if (focus < 0) index = 0;
    if (index === 2) hexPrism(c, V(0, 8), 0.8, 0.75, spin, 78, true);
    else parallelepiped(c, index, V(0, 8), spin, 78);
    c.label(focus < 0 ? "Les trois axes d'un cristal : a, b et c" : SystemNames[index], V(0, 150), 17, Gold, 440);
    c.label(focus < 0 ? "et les angles entre eux : α, β, γ" : SystemCaptions[index], V(0, -150), 14, Ink, 460);
  });

  // ---- The polariscope and the conoscope ----
  var RingColors = [C(0.95, 0.85, 0.30, 0.85), C(0.90, 0.35, 0.35, 0.85), C(0.35, 0.50, 0.95, 0.85), C(0.40, 0.85, 0.50, 0.85)];
  function conoscope(c, focus, t) {
    var shimmer = 0.04 * sin(t * 2), r;
    if (focus === 4) {
      c.circle(V(0, 0), 128, Dark);
      for (r = 118; r > 6; r -= 16) c.circle(V(0, 0), r * (1 + shimmer), RingColors[Math.floor(r / 16) % RingColors.length]);
      c.rect(V(0, 0), V(250, 14), Dark);
      c.rect(V(0, 0), V(14, 250), Dark);
      c.label("Uniaxe : croix noire et anneaux colorés", V(0, 150), 16, Ink, 460);
    } else {
      c.circle(V(0, 0), 128, Dark);
      [-52, 52].forEach(function (x) {
        for (var rr = 70; rr > 6; rr -= 14) c.circle(V(x, 0), rr * (1 + shimmer), RingColors[Math.floor(rr / 14) % RingColors.length]);
        c.rect(V(x, 0), V(10, 160), alpha(Dark, 0.95), 18);
      });
      c.rect(V(0, 0), V(250, 10), alpha(Dark, 0.9));
      c.label("Biaxe : deux « yeux », deux axes optiques", V(0, 150), 16, Ink, 460);
    }
    c.label("schéma simplifié", V(0, -148), 11, Dim, 200);
  }
  K.register("polariscope", function (c, focus, t) {
    if (focus >= 4) { conoscope(c, focus, t); return; }
    var angle = t * 40, stonePos = V(-20, 0), brightness = 0, i;
    if (focus === 2) brightness = Math.pow(sin(angle * 2 * M.deg), 2); else if (focus === 3) brightness = 0.8;
    c.circle(V(-205, 0), 15, C(1, 0.92, 0.45));
    c.label("Lumière", V(-205, -30), 12, Gold, 80);
    c.rect(V(-125, 0), V(10, 150), C(0.55, 0.75, 1, 0.55));
    for (i = -3; i <= 3; i++) c.rect(V(-125, i * 20), V(3, 14), Ink);
    c.label("Polariseur", V(-125, -92), 12, Gold, 90);
    c.rect(V(85, 0), V(10, 150), C(0.55, 0.75, 1, 0.55));
    for (i = -3; i <= 3; i++) c.rect(V(85, i * 20), V(14, 3), Ink);
    c.label("Analyseur", V(85, -92), 12, Gold, 90);
    var beam = C(1, 0.95, 0.6, 0.9);
    c.line(V(-190, 0), V(-125, 0), 3, beam);
    c.line(V(-125, 0), stonePos, 3, beam);
    if (focus >= 1) {
      c.rect(stonePos, V(60, 60), Stone, 45 + (focus >= 1 && focus <= 3 ? angle : 0));
      c.rect(stonePos, V(34, 34), StoneLight, 45 + angle);
    }
    c.line(stonePos, V(85, 0), 3, C(1, 0.95, 0.6, Math.max(0.9 * (focus === 0 ? 0.9 : 1), 0.3)));
    c.line(V(85, 0), V(150, 0), 3, C(1, 0.95, 0.6, brightness * 0.9));
    c.label("Ce que voit l'œil", V(190, -52), 12, Gold, 110);
    c.circle(V(190, 0), 38, C(0.55, 0.60, 0.62));
    c.circle(V(190, 0), 35, mix(Dark, C(0.95, 0.95, 0.85), brightness));
    var note = focus === 0 ? "Deux filtres croisés : tout est noir" : focus === 1 ? "Pierre isotrope : noire, quel que soit l'angle" :
      focus === 2 ? "Pierre anisotrope : 4 éclairs par tour" : "Agrégat : toujours claire";
    c.label(note, V(0, 140), 16, Ink, 440);
  });
})();
