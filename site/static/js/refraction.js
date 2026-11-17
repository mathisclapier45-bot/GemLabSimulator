// Animated lesson on the refractometer: light at a surface, critical angle, shadow edge.
// Every <div class="anim" data-mode="N"> gets its own animation; texts come from data-labels (translated by the server).
(function () {
  "use strict";
  var W = 640, H = 360, CX = 320, CY = 180, DEG = Math.PI / 180;
  var COL = { inc: "#ffe65a", ref: "#8fe6ff", refr: "#ff9a33", norm: "#8a94a8", glass: "rgba(120,170,255,.22)", stone: "rgba(200,120,255,.25)", air: "rgba(255,255,255,.04)" };

  function ray(g, x1, y1, x2, y2, color, width, alpha) {
    g.save(); g.globalAlpha = alpha === undefined ? 1 : alpha;
    g.strokeStyle = color; g.lineWidth = width || 3; g.shadowColor = color; g.shadowBlur = 8;
    g.beginPath(); g.moveTo(x1, y1); g.lineTo(x2, y2); g.stroke();
    var a = Math.atan2(y2 - y1, x2 - x1), mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
    g.fillStyle = color; g.beginPath(); g.moveTo(mx + 9 * Math.cos(a), my + 9 * Math.sin(a));
    g.lineTo(mx - 6 * Math.cos(a) + 6 * Math.sin(a), my - 6 * Math.sin(a) - 6 * Math.cos(a));
    g.lineTo(mx - 6 * Math.cos(a) - 6 * Math.sin(a), my - 6 * Math.sin(a) + 6 * Math.cos(a)); g.fill();
    g.restore();
  }
  function text(g, s, x, y, color, size, align) {
    g.fillStyle = color || "#dfe6f2"; g.font = (size || 15) + "px system-ui, sans-serif"; g.textAlign = align || "left"; g.fillText(s, x, y);
  }
  function media(g, n1Color, n2Color, L, top, bottom) {
    g.fillStyle = n1Color; g.fillRect(0, 0, W, CY); g.fillStyle = n2Color; g.fillRect(0, CY, W, H - CY);
    g.strokeStyle = "#cfd8ea"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, CY); g.lineTo(W, CY); g.stroke();
    g.setLineDash([6, 6]); g.strokeStyle = COL.norm; g.lineWidth = 1.5; g.beginPath(); g.moveTo(CX, 20); g.lineTo(CX, H - 20); g.stroke(); g.setLineDash([]);
    text(g, top, 12, 24, "#cfe0ff"); text(g, bottom, 12, H - 12, "#e6d4ff"); text(g, L.normal, CX + 8, 34, COL.norm, 13);
  }
  function crit(n1, n2) { return n1 > n2 ? Math.asin(n2 / n1) / DEG : 90; }
  function sweep(t, lo, hi, period) { return lo + (hi - lo) * (0.5 - 0.5 * Math.cos(t * 2 * Math.PI / period)); }
  function rayEnds(th, len) { return [CX - len * Math.sin(th * DEG), CY - len * Math.cos(th * DEG)]; }
  function arcLabel(g, th, color, label) {
    g.strokeStyle = color; g.lineWidth = 2; g.beginPath(); g.arc(CX, CY, 70, -Math.PI / 2 - th * DEG, -Math.PI / 2); g.stroke();
    text(g, label, CX - 70 * Math.sin(th * DEG / 2) - 40, CY - 76 * Math.cos(th * DEG / 2), color, 14);
  }

  // ---- steps 0..3: rays at a surface
  function surface(g, t, L, mode) {
    var n1 = mode <= 1 ? 1.0 : 1.81, n2 = mode <= 1 ? 1.5 : 1.54, tc = crit(n1, n2);
    var th = mode === 3 ? sweep(t, 15, 82, 9) : mode === 2 ? sweep(t, 10, tc - 1, 6) : sweep(t, 12, 72, 7);
    media(g, mode <= 1 ? COL.air : COL.glass, mode <= 1 ? COL.glass : COL.stone,
      L, mode <= 1 ? L.air : L.glass, mode <= 1 ? L.glass15 : L.stone + " (n = 1,54)");
    var s2 = n1 / n2 * Math.sin(th * DEG), total = s2 >= 1, e = rayEnds(th, 135);
    ray(g, e[0], e[1], CX, CY, COL.inc, 4);
    text(g, L.incident, e[0] - 10, e[1] + 4, COL.inc, 14, "right");
    if (!total) {
      var th2 = Math.asin(s2) / DEG;
      ray(g, CX, CY, CX + 135 * Math.sin(th2 * DEG), CY + 135 * Math.cos(th2 * DEG), COL.refr, 4);
      text(g, L.refracted, CX + 145 * Math.sin(th2 * DEG), CY + 155 * Math.cos(th2 * DEG), COL.refr, 14, "left");
      text(g, "θ₁ = " + th.toFixed(0) + "°   θ₂ = " + th2.toFixed(0) + "°", W - 12, 26, "#dfe6f2", 15, "right");
    } else {
      text(g, L.total, W - 12, 26, "#ff8f8f", 17, "right");
    }
    if (mode === 1 || mode >= 2) {
      var weak = mode === 1 ? 0.55 : (total ? 1 : 0.28);
      ray(g, CX, CY, CX + 135 * Math.sin(th * DEG), CY - 135 * Math.cos(th * DEG), COL.ref, total ? 5 : 3, weak);
      text(g, L.reflected, CX + 140 * Math.sin(th * DEG), CY - 145 * Math.cos(th * DEG), COL.ref, 14, "left");
    }
    if (mode >= 2) {
      arcLabel(g, tc, "#ffb0b0", L.critical + " = " + tc.toFixed(1) + "°");
    }
  }

  // ---- eyepiece disc with a scale; edges = list of indices (dark below the edge on the left)
  function eyepiece(g, cx, cy, r, edgeIdx, label) {
    var lo = 1.40, hi = 1.81;
    g.save(); g.beginPath(); g.arc(cx, cy, r, 0, 2 * Math.PI); g.clip();
    g.fillStyle = "#f4e9b8"; g.fillRect(cx - r, cy - r, 2 * r, 2 * r);
    var x = function (n) { return cx - r + (n - lo) / (hi - lo) * 2 * r; };
    var edges = edgeIdx.slice().sort(function (a, b) { return a - b; });
    // dark zone for indices below the lowest edge, mid-grey between two edges
    g.fillStyle = "#16151c"; g.fillRect(cx - r, cy - r, x(edges[0]) - (cx - r), 2 * r);
    if (edges.length > 1) { g.fillStyle = "#8d8660"; g.fillRect(x(edges[0]), cy - r, x(edges[1]) - x(edges[0]), 2 * r); }
    g.fillStyle = "#e9e6d8"; g.fillRect(cx - r, cy + r - 32, 2 * r, 32);   // light strip behind the scale
    g.restore();
    g.strokeStyle = "#cfd8ea"; g.lineWidth = 2; g.beginPath(); g.arc(cx, cy, r, 0, 2 * Math.PI); g.stroke();
    g.strokeStyle = "#222"; g.lineWidth = 1;
    for (var n = 1.40; n <= 1.81; n += 0.05) {
      g.beginPath(); g.moveTo(x(n), cy + r - 6); g.lineTo(x(n), cy + r - (Math.abs(n * 10 % 1) < 0.01 ? 18 : 12)); g.stroke();
    }
    g.fillStyle = "#222"; g.font = "11px system-ui"; g.textAlign = "center";
    [1.4, 1.5, 1.6, 1.7, 1.8].forEach(function (n) { g.fillText(n.toFixed(1), x(n), cy + r - 22); });
    edges.forEach(function (n) { g.strokeStyle = "#ff5a3c"; g.lineWidth = 2; g.beginPath(); g.moveTo(x(n), cy - r); g.lineTo(x(n), cy + r - 26); g.stroke(); });
    text(g, label, cx, cy + r + 22, "#dfe6f2", 14, "center");
  }

  function edgeScene(g, t, L) {
    var n1 = 1.81, n2 = sweep(t, 1.45, 1.72, 8), tc = crit(n1, n2);
    g.fillStyle = COL.glass; g.fillRect(0, 0, 330, CY); g.fillStyle = COL.stone; g.fillRect(0, CY, 330, H - CY);
    g.strokeStyle = "#cfd8ea"; g.lineWidth = 2; g.beginPath(); g.moveTo(0, CY); g.lineTo(330, CY); g.stroke();
    text(g, L.glass, 8, 20, "#cfe0ff", 13); text(g, L.stone + " (n = " + n2.toFixed(2).replace(".", ",") + ")", 8, H - 10, "#e6d4ff", 13);
    for (var a = 10; a <= 80; a += 10) {
      var e = [200 - 150 * Math.sin(a * DEG), CY - 150 * Math.cos(a * DEG)];
      var over = a >= tc;
      ray(g, e[0], e[1], 200, CY, over ? COL.inc : "#7a7440", 2.5, over ? 1 : 0.7);
      if (over) ray(g, 200, CY, 200 + 130 * Math.sin(a * DEG), CY - 130 * Math.cos(a * DEG), COL.ref, 2.5, 0.9);
      else ray(g, 200, CY, 200 + 90 * Math.sin(Math.asin(Math.min(1, n1 / n2 * Math.sin(a * DEG)))) , CY + 90 * Math.cos(Math.asin(Math.min(1, n1 / n2 * Math.sin(a * DEG)))), COL.refr, 2, 0.35);
    }
    text(g, L.critical + " = " + tc.toFixed(1) + "°", 12, 44, "#ffb0b0", 14);
    eyepiece(g, 500, 160, 105, [n2], L.scale);
    text(g, L.dark, 440, 140, "#cfd8ea", 12, "center"); text(g, L.bright, 560, 140, "#333", 12, "center");
  }

  function liquidScene(g, t, L) {
    var withLiquid = (Math.floor(t / 3.5) % 2) === 1, th = 50, n1 = 1.81;
    g.fillStyle = COL.glass; g.fillRect(0, 0, W, CY - 14);
    g.fillStyle = withLiquid ? "rgba(120,230,180,.45)" : "rgba(255,255,255,.04)"; g.fillRect(0, CY - 14, W, 28);
    g.fillStyle = COL.stone; g.fillRect(0, CY + 14, W, H - CY - 14);
    text(g, L.glass, 12, 24, "#cfe0ff"); text(g, withLiquid ? L.liquid : L.air, W - 12, CY + 5, "#bff5dc", 13, "right"); text(g, L.stone, 12, H - 12, "#e6d4ff");
    var e = [CX - 150 * Math.sin(th * DEG), CY - 14 - 150 * Math.cos(th * DEG)];
    ray(g, e[0], e[1], CX, CY - 14, COL.inc, 4);
    if (withLiquid) {
      var th2 = Math.asin(n1 / 1.54 * Math.sin(th * DEG) > 1 ? 1 : n1 / 1.54 * Math.sin(th * DEG)) / DEG;
      ray(g, CX, CY - 14, CX + 120 * Math.sin(th2 * DEG), CY - 14 + 120 * Math.cos(th2 * DEG), COL.refr, 4, 0.9);
      text(g, L.with, W / 2, 30, "#9fffd0", 17, "center");
    } else {
      ray(g, CX, CY - 14, CX + 150 * Math.sin(th * DEG), CY - 14 - 150 * Math.cos(th * DEG), COL.ref, 5);
      text(g, L.without, W / 2, 30, "#ff9f9f", 17, "center");
    }
  }

  function twoScene(g, t, L) {
    var rot = (t * 40) % 360, ne = 1.55 + 0.10 * Math.abs(Math.sin(rot * DEG));
    eyepiece(g, 175, 170, 120, [1.62], L.iso);
    eyepiece(g, 465, 170, 120, [1.55, ne + 0.02], L.aniso);
  }

  var DRAW = [
    function (g, t, L) { surface(g, t, L, 0); }, function (g, t, L) { surface(g, t, L, 1); },
    function (g, t, L) { surface(g, t, L, 2); }, function (g, t, L) { surface(g, t, L, 3); },
    edgeScene, liquidScene, twoScene];

  function setup(box) {
    var mode = parseInt(box.getAttribute("data-mode"), 10), L = JSON.parse(box.getAttribute("data-labels"));
    var canvas = box.querySelector("canvas"), g = canvas.getContext("2d"), btn = box.querySelector("button");
    var dpr = window.devicePixelRatio || 1; canvas.width = W * dpr; canvas.height = H * dpr; g.scale(dpr, dpr);
    var playing = true, visible = false, t0 = null, tPause = 0, tNow = 0;
    function frame(ts) {
      if (!visible) { t0 = null; return; }
      if (t0 === null) t0 = ts - tNow * 1000;
      if (playing) tNow = (ts - t0) / 1000; else t0 = ts - tNow * 1000;
      g.clearRect(0, 0, W, H); g.fillStyle = "#0f1420"; g.fillRect(0, 0, W, H);
      DRAW[mode](g, tNow, L);
      requestAnimationFrame(frame);
    }
    btn.addEventListener("click", function () { playing = !playing; btn.textContent = playing ? L.pause : L.play; });
    if ("IntersectionObserver" in window) {
      new IntersectionObserver(function (entries) {
        var v = entries[0].isIntersecting;
        if (v && !visible) { visible = true; requestAnimationFrame(frame); } else if (!v) { visible = false; }
      }).observe(box);
    } else { visible = true; requestAnimationFrame(frame); }
  }

  document.querySelectorAll(".anim[data-mode]").forEach(setup);
})();
