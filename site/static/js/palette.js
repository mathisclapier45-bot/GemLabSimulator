// Colour chart of a species: click a colour, pick a shape and a cut, see the stone (drawn here, as an original SVG).
(function () {
  "use strict";
  var root = document.querySelector(".colors");
  if (!root) return;
  var data = JSON.parse(document.getElementById("pal-data").textContent);
  var L = JSON.parse(root.getAttribute("data-labels"));
  var state = { variant: "natural", index: 0, shape: root.getAttribute("data-shape"), cut: root.getAttribute("data-cut") };
  var stoneBox = root.querySelector(".stone"), info = root.querySelector(".cinfo");
  var SLUG = { Rond: "rond", Ovale: "ovale", Coussin: "coussin", Carre: "carre", Rectangle: "rectangle", Emeraude: "rectangle-a-pans-coupes", Octogone: "octogone",
    Baguette: "baguette", Poire: "poire", Marquise: "marquise", Shield: "shield", Triangle: "triangle", Coeur: "coeur" };
  var view = window.GemView3D ? window.GemView3D.create(stoneBox) : null;
  var allowed = null;       // from the exported cuts: which sizes each shape accepts
  if (view) view.index().then(function (ix) { allowed = ix.allowed; fixCut(); refresh(); });
  function cutAllowed(cut, shape) {
    if (cut === "cabochon") return true;
    if (L.phys.cabonly) return false;
    if (cut === "mixte" || !allowed) return true;
    return (allowed[cut] || []).indexOf(SLUG[shape]) >= 0;
  }
  function fixCut() { if (!cutAllowed(state.cut, state.shape)) state.cut = "cabochon"; }
  var NS = "http://www.w3.org/2000/svg";

  // ---------- colour maths
  function hsv(h, s, v) {
    h = ((h % 360) + 360) % 360 / 60; var c = v * s, x = c * (1 - Math.abs(h % 2 - 1)), m = v - c, r = 0, g = 0, b = 0;
    if (h < 1) { r = c; g = x; } else if (h < 2) { r = x; g = c; } else if (h < 3) { g = c; b = x; } else if (h < 4) { g = x; b = c; } else if (h < 5) { r = x; b = c; } else { r = c; b = x; }
    return [r + m, g + m, b + m];
  }
  function css(rgb, k) { k = k || 1; return "rgb(" + rgb.map(function (v) { return Math.round(Math.max(0, Math.min(1, v * k)) * 255); }).join(",") + ")"; }
  function facetOf(c) { return hsv(c.h, c.s * 0.62, Math.min(1, c.b * 1.12 + 0.14)); }

  // ---------- outlines (points around a stone, in a 200 x 200 box)
  function poly(pts) { return pts; }
  function circle(rx, ry, n) { var o = []; for (var i = 0; i < n; i++) { var a = i / n * 2 * Math.PI - Math.PI / 2; o.push([100 + rx * Math.cos(a), 100 + ry * Math.sin(a)]); } return o; }
  function superellipse(r, p, n) { var o = []; for (var i = 0; i < n; i++) { var a = i / n * 2 * Math.PI, c = Math.cos(a), s = Math.sin(a); o.push([100 + r * Math.sign(c) * Math.pow(Math.abs(c), 2 / p), 100 + r * Math.sign(s) * Math.pow(Math.abs(s), 2 / p)]); } return o; }
  function cutRect(w, h, cut) { var x = 100 - w / 2, y = 100 - h / 2, X = 100 + w / 2, Y = 100 + h / 2; return [[x + cut, y], [X - cut, y], [X, y + cut], [X, Y - cut], [X - cut, Y], [x + cut, Y], [x, Y - cut], [x, y + cut]]; }
  function pear(n) { var o = []; for (var i = 0; i < n; i++) { var t = i / n * 2 * Math.PI, y = -Math.cos(t), hw = 64 * (0.3 + 0.7 * ((y + 1) / 2)) * Math.abs(Math.sin(t)); o.push([100 + (Math.sin(t) < 0 ? -hw : hw), 100 + 86 * y]); } return o; }
  function marquise(n) { var o = []; for (var i = 0; i < n; i++) { var t = i / n * 2 * Math.PI, y = -Math.cos(t), hw = 54 * (1 - Math.pow(Math.abs(y), 1.7)); o.push([100 + (Math.sin(t) < 0 ? -hw : hw), 100 + 88 * y]); } return o; }
  function heart(n) { var o = []; for (var i = 0; i < n; i++) { var t = i / n * 2 * Math.PI, x = 16 * Math.pow(Math.sin(t), 3), y = 13 * Math.cos(t) - 5 * Math.cos(2 * t) - 2 * Math.cos(3 * t) - Math.cos(4 * t); o.push([100 + x * 5.2, 96 - y * 5.6 + 6]); } return o; }
  function resample(pts, n) {
    var d = [0], i; for (i = 1; i <= pts.length; i++) { var a = pts[i - 1], b = pts[i % pts.length]; d.push(d[i - 1] + Math.hypot(b[0] - a[0], b[1] - a[1])); }
    var total = d[d.length - 1], out = [], j = 0;
    for (i = 0; i < n; i++) { var pos = total * i / n; while (d[j + 1] < pos) j++; var a2 = pts[j % pts.length], b2 = pts[(j + 1) % pts.length], f = (pos - d[j]) / (d[j + 1] - d[j] || 1); out.push([a2[0] + (b2[0] - a2[0]) * f, a2[1] + (b2[1] - a2[1]) * f]); }
    return out;
  }
  var OUTLINES = {
    Rond: function () { return circle(82, 82, 32); }, Ovale: function () { return circle(62, 84, 32); },
    Coussin: function () { return resample(superellipse(80, 3.4, 160), 32); }, Carre: function () { return resample(cutRect(150, 150, 6), 32); },
    Rectangle: function () { return resample(cutRect(112, 152, 6), 32); }, Emeraude: function () { return resample(cutRect(112, 152, 26), 32); },
    Octogone: function () { return resample(cutRect(150, 150, 44), 32); }, Baguette: function () { return resample(cutRect(66, 152, 8), 32); },
    Poire: function () { return resample(pear(160), 32); }, Marquise: function () { return resample(marquise(160), 32); },
    Shield: function () { return resample([[34, 30], [166, 30], [166, 96], [100, 176], [34, 96]], 32); },
    Triangle: function () { return resample([[100, 24], [172, 160], [28, 160]], 32); }, Coeur: function () { return resample(heart(200), 32); }
  };

  function el(name, attrs, parent) { var e = document.createElementNS(NS, name); for (var k in attrs) e.setAttribute(k, attrs[k]); if (parent) parent.appendChild(e); return e; }
  function scaled(pts, k, cx, cy) { return pts.map(function (p) { return [cx + (p[0] - cx) * k, cy + (p[1] - cy) * k]; }); }
  function path(pts) { return "M" + pts.map(function (p) { return p[0].toFixed(1) + " " + p[1].toFixed(1); }).join("L") + "Z"; }
  function centroid(pts) { var x = 0, y = 0; pts.forEach(function (p) { x += p[0]; y += p[1]; }); return [x / pts.length, y / pts.length]; }

  // ---------- drawing the stone
  function draw() {
    var c = data[state.variant][state.index], body = hsv(c.h, c.s, c.b), facet = facetOf(c);
    if (view) {
      var ph = L.phys;
      view.set({ shape: SLUG[state.shape], style: state.cut, body: body, ior: ph.ior, disp: ph.disp, depth: ph.depth, milky: ph.milky, dims: ph.dims });
      return;
    }
    var cutKind = state.cut === "cabochon" ? "cabochon" : (state.cut === "a-degres" || state.cut === "ascher" ? "step" : (state.cut === "mixte" ? "mixed" : "brilliant"));
    var make = OUTLINES[state.shape] || OUTLINES.Ovale, out = make(), cen = centroid(out), n = out.length;
    var svg = document.createElementNS(NS, "svg"); svg.setAttribute("viewBox", "0 0 200 200"); svg.setAttribute("class", "gemview");
    var defs = el("defs", {}, svg);
    var rg = el("radialGradient", { id: "gv-rg", cx: "38%", cy: "30%", r: "75%" }, defs);
    el("stop", { offset: "0%", "stop-color": css(facet) }, rg); el("stop", { offset: "55%", "stop-color": css(body) }, rg); el("stop", { offset: "100%", "stop-color": css(body, 0.55) }, rg);
    el("ellipse", { cx: 100, cy: 188, rx: 70, ry: 7, fill: "rgba(0,0,0,.16)" }, svg);
    if (cutKind === "cabochon") {
      el("path", { d: path(out), fill: "url(#gv-rg)", stroke: css(body, 0.45), "stroke-width": 1.5 }, svg);
      var hl = el("ellipse", { cx: cen[0] - 16, cy: cen[1] - 22, rx: 18, ry: 10, fill: "rgba(255,255,255,.55)", transform: "rotate(-30 " + (cen[0] - 16) + " " + (cen[1] - 22) + ")" }, svg);
    } else {
      var rings = cutKind === "step" ? [1, 0.82, 0.64, 0.46] : (cutKind === "mixed" ? [1, 0.74, 0.5] : [1, 0.72, 0.5]);
      var light = Math.PI * 1.25;
      el("path", { d: path(out), fill: css(body, 0.8) }, svg);
      for (var r = 0; r < rings.length - 1; r++) {
        var a = scaled(out, rings[r], cen[0], cen[1]), b = scaled(out, rings[r + 1], cen[0], cen[1]);
        for (var i = 0; i < n; i++) {
          var j = (i + 1) % n, pts = [a[i], a[j], b[j], b[i]];
          var mid = [(a[i][0] + a[j][0]) / 2 - cen[0], (a[i][1] + a[j][1]) / 2 - cen[1]], ang = Math.atan2(mid[1], mid[0]);
          var k = 0.72 + 0.42 * Math.max(0, Math.cos(ang - light)) + (cutKind !== "step" && (i + r) % 2 ? 0.1 : 0) - r * 0.04;
          var fill = k > 1 ? css(facet, 0.9 + (k - 1)) : css(mix(body, facet, Math.max(0, (k - 0.7) / 0.4)), k > 0.95 ? 1 : 0.85 + k * 0.15);
          el("path", { d: path(pts), fill: fill, stroke: "rgba(255,255,255,.28)", "stroke-width": 0.6 }, svg);
        }
      }
      var table = scaled(out, rings[rings.length - 1], cen[0], cen[1]);
      var tg = el("linearGradient", { id: "gv-tg", x1: "0", y1: "0", x2: "1", y2: "1" }, defs);
      el("stop", { offset: "0%", "stop-color": css(facet, 1.05) }, tg); el("stop", { offset: "100%", "stop-color": css(body, 0.9) }, tg);
      el("path", { d: path(table), fill: "url(#gv-tg)", stroke: "rgba(255,255,255,.5)", "stroke-width": 0.8 }, svg);
      if (cutKind !== "step") for (var q = 0; q < n; q += 2) el("line", { x1: table[q][0], y1: table[q][1], x2: cen[0], y2: cen[1], stroke: "rgba(255,255,255,.18)", "stroke-width": 0.6 }, svg);
      el("path", { d: path(scaled(table, 0.5, cen[0] - 12, cen[1] - 14)), fill: "rgba(255,255,255,.22)" }, svg);
      el("path", { d: path(out), fill: "none", stroke: css(body, 0.4), "stroke-width": 1.4 }, svg);
    }
    stoneBox.innerHTML = ""; stoneBox.appendChild(svg);
  }
  function mix(a, b, t) { return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]; }

  function pct(v, d) { return v.toFixed(d).replace(".", ","); }
  function describe() {
    var c = data[state.variant][state.index];
    var neutral = data[state.variant].length === 1;
    var text = L.info.replace("{name}", c.name).replace("{h}", Math.round(c.h)).replace("{s}", Math.round(c.s * 100)).replace("{b}", Math.round(c.b * 100));
    info.innerHTML = "";
    var strong = document.createElement("strong"); strong.textContent = c.name; info.appendChild(strong);
    info.appendChild(document.createTextNode(text.slice(c.name.length)));
    if (c.share > 0 && !neutral) { var p = document.createElement("span"); p.className = "small"; p.textContent = L.share.replace("{share}", pct(c.share * 100, c.share < 0.01 ? 1 : 0)); info.appendChild(document.createElement("br")); info.appendChild(p); }
    if (neutral) { var q = document.createElement("span"); q.className = "small"; q.textContent = L.neutral; info.appendChild(document.createElement("br")); info.appendChild(q); }
  }
  function mark() {
    root.querySelectorAll(".sw").forEach(function (b) { b.classList.toggle("on", b.getAttribute("data-v") === state.variant && +b.getAttribute("data-i") === state.index); });
    root.querySelectorAll("[data-shape]").forEach(function (b) { if (b.tagName === "BUTTON") b.classList.toggle("on", b.getAttribute("data-shape") === state.shape); });
    root.querySelectorAll("[data-cut]").forEach(function (b) {
      if (b.tagName !== "BUTTON") return;
      b.classList.toggle("on", b.getAttribute("data-cut") === state.cut);
      b.disabled = !cutAllowed(b.getAttribute("data-cut"), state.shape);
    });
  }
  function refresh() { draw(); describe(); mark(); }

  function best(variant) { var arr = data[variant], bi = 0; arr.forEach(function (c, i) { if (c.share > arr[bi].share) bi = i; }); return bi; }
  root.addEventListener("click", function (e) {
    var b = e.target.closest("button"); if (!b || !root.contains(b)) return;
    if (b.classList.contains("sw")) { state.variant = b.getAttribute("data-v"); state.index = +b.getAttribute("data-i"); }
    else if (b.hasAttribute("data-shape")) { state.shape = b.getAttribute("data-shape"); fixCut(); }
    else if (b.hasAttribute("data-cut")) state.cut = b.getAttribute("data-cut");
    else if (b.hasAttribute("data-variant")) {
      state.variant = b.getAttribute("data-variant"); state.index = best(state.variant);
      root.querySelectorAll(".grid-wrap").forEach(function (g) { g.hidden = g.getAttribute("data-variant") !== state.variant; });
      root.querySelectorAll(".seg [data-variant]").forEach(function (x) { x.classList.toggle("on", x === b); });
    } else return;
    refresh();
  });
  state.index = best("natural");
  fixCut();
  refresh();
})();
