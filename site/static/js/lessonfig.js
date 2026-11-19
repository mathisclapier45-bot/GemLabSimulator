// The animated drawings of the lessons: a port of the game's LessonFigures (Unity, 480 x 340 board, origin in the middle, y up) on a 2D canvas.
// This file holds the small drawing toolkit and the player; the figures themselves are in lessonfig_figs.js.
(function () {
  "use strict";
  var SELF = document.currentScript;
  var W = 480, H = 340;

  // ---- Maths and colours, as in Unity (colours are [r, g, b, a] between 0 and 1, points are [x, y]) ----
  var M = {
    clamp01: function (v) { return Math.max(0, Math.min(1, v)); },
    lerp: function (a, b, t) { t = M.clamp01(t); return a + (b - a) * t; },
    smoothStep: function (from, to, t) {
      t = M.clamp01((t - from) / (to - from));
      t = -2 * t * t * t + 3 * t * t;
      return from + (to - from) * t;
    },
    repeat: function (t, len) { return t - Math.floor(t / len) * len; },
    deg: Math.PI / 180
  };
  function C(r, g, b, a) { return [r, g, b, a === undefined ? 1 : a]; }
  function alpha(c, a) { return [c[0], c[1], c[2], a]; }
  function mix(a, b, t) { t = M.clamp01(t); return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t, a[3] + (b[3] - a[3]) * t]; }
  function brighter(c, d) { return [c[0] + d, c[1] + d, c[2] + d, c[3]]; }
  function css(c) {
    var k = function (v) { return Math.round(M.clamp01(v) * 255); };
    return "rgba(" + k(c[0]) + "," + k(c[1]) + "," + k(c[2]) + "," + M.clamp01(c[3]) + ")";
  }
  function V(x, y) { return [x, y]; }
  function add(a, b) { return [a[0] + b[0], a[1] + b[1]]; }
  function mul(a, k) { return [a[0] * k, a[1] * k]; }
  function rot(v, degrees) {
    var a = degrees * M.deg, c = Math.cos(a), s = Math.sin(a);
    return [v[0] * c - v[1] * s, v[0] * s + v[1] * c];
  }
  var WHITE = C(1, 1, 1, 1);

  // ---- The board ----
  function Board(ctx) { this.g = ctx; }
  Board.prototype.rect = function (center, size, color, angle) {
    var g = this.g;
    g.save();
    g.translate(W / 2 + center[0], H / 2 - center[1]);
    if (angle) g.rotate(-angle * M.deg);
    g.fillStyle = css(color);
    g.fillRect(-size[0] / 2, -size[1] / 2, size[0], size[1]);
    g.restore();
  };
  Board.prototype.line = function (from, to, thickness, color) {
    var dx = to[0] - from[0], dy = to[1] - from[1];
    this.rect([(from[0] + to[0]) / 2, (from[1] + to[1]) / 2], [Math.sqrt(dx * dx + dy * dy), thickness], color, Math.atan2(dy, dx) / M.deg);
  };
  Board.prototype.circle = function (center, radius, color) {
    var g = this.g;
    if (radius <= 0) return;
    g.fillStyle = css(color);
    g.beginPath();
    g.arc(W / 2 + center[0], H / 2 - center[1], radius, 0, Math.PI * 2);
    g.fill();
  };
  Board.prototype.picture = function (image, center, size) {
    if (!image) return;
    this.g.drawImage(image, W / 2 + center[0] - size[0] / 2, H / 2 - center[1] - size[1] / 2, size[0], size[1]);
  };
  Board.prototype.label = function (text, center, size, color, width) {
    if (!text) return;
    text = kit.t(String(text));
    var g = this.g;
    width = width || 220;
    g.font = size + "px system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";
    g.textAlign = "center";
    g.textBaseline = "middle";
    var lines = [];
    String(text).split("\n").forEach(function (para) {
      var words = para.split(" "), line = "";
      words.forEach(function (w) {
        var test = line ? line + " " + w : w;
        if (line && g.measureText(test).width > width) { lines.push(line); line = w; } else line = test;
      });
      lines.push(line);
    });
    var lh = size * 1.18, y0 = H / 2 - center[1] - (lines.length - 1) * lh / 2;
    g.fillStyle = css(color);
    for (var i = 0; i < lines.length; i++) g.fillText(lines[i], W / 2 + center[0], y0 + i * lh);
  };

  // ---- Spectrum strips and interference figures (images built once and drawn on the board) ----
  var spectra = {}, stripCache = {};
  var STOPS = [
    [400, C(0.07, 0.02, 0.18)], [415, C(0.13, 0.06, 0.40)], [432, C(0.19, 0.12, 0.60)], [450, C(0.15, 0.22, 0.78)],
    [462, C(0.18, 0.36, 0.85)], [472, C(0.40, 0.72, 0.88)], [490, C(0.38, 0.72, 0.70)], [510, C(0.30, 0.62, 0.22)],
    [535, C(0.32, 0.60, 0.12)], [560, C(0.62, 0.75, 0.10)], [575, C(0.90, 0.82, 0.10)], [590, C(0.96, 0.60, 0.10)],
    [610, C(0.90, 0.28, 0.14)], [630, C(0.82, 0.13, 0.12)], [660, C(0.78, 0.10, 0.10)], [685, C(0.50, 0.05, 0.05)],
    [700, C(0.20, 0.02, 0.02)]
  ];
  function waveColor(nm) {
    if (nm <= STOPS[0][0]) return STOPS[0][1];
    for (var i = 1; i < STOPS.length; i++) {
      if (nm > STOPS[i][0]) continue;
      return mix(STOPS[i - 1][1], STOPS[i][1], (nm - STOPS[i - 1][0]) / (STOPS[i][0] - STOPS[i - 1][0]));
    }
    return STOPS[STOPS.length - 1][1];
  }
  function darkness(lines, nm) {
    var d = 0;
    (lines || []).forEach(function (ln) {
      var half = Math.max(ln.widthNm, 1) * 0.5, dist = Math.abs(nm - ln.wavelengthNm);
      if (dist >= half) return;
      var edge = ln.edgeNm > 0 ? ln.edgeNm : Math.max(1.5, Math.min(12, ln.widthNm * 0.35));
      d = Math.max(d, M.clamp01((half - dist) / Math.min(edge, half)) * ln.depth);
    });
    return d;
  }
  // The strip of a species ("" = plain white light), like SpectrumRenderer.BuildSpectrumSprite.
  function strip(key) {
    if (stripCache[key]) return stripCache[key];
    var w = 300, h = 40, cv = document.createElement("canvas");
    cv.width = w; cv.height = h;
    var g = cv.getContext("2d");
    for (var x = 0; x < w; x++) {
      var nm = 400 + 300 * x / (w - 1);
      g.fillStyle = css(mix(waveColor(nm), C(0, 0, 0, 1), darkness(spectra[key], nm)));
      g.fillRect(x, 0, 1, h);
    }
    stripCache[key] = cv;
    return cv;
  }

  var interfCache = {};
  function channel(ret, wave) { var s = Math.sin(Math.PI * ret / wave); return s * s; }
  // The pixel model of InterferenceImages.cs: one optic axis (c1) or two (c1, c2); bulls = the bull's eye of the quartz.
  function interference(key, size, c1, c2, biaxial, bulls, edge) {
    var cached = interfCache[key];
    if (cached && cached.k === JSON.stringify([c1, c2, edge])) return cached.cv;
    var cv = cached ? cached.cv : document.createElement("canvas");
    cv.width = cv.height = size;
    var g = cv.getContext("2d"), img = g.createImageData(size, size), px = img.data;
    for (var y = 0; y < size; y++) {
      for (var x = 0; x < size; x++) {
        var u = (x + 0.5) / size * 2 - 1, v = ((size - 1 - y) + 0.5) / size * 2 - 1, r = Math.sqrt(u * u + v * v), o = (y * size + x) * 4;
        if (r >= 1) { px[o + 3] = 0; continue; }
        var r1x = u - c1[0], r1y = v - c1[1], ret, psi;
        if (!biaxial) { ret = edge * (r1x * r1x + r1y * r1y); psi = Math.atan2(r1y, r1x); }
        else {
          var r2x = u - c2[0], r2y = v - c2[1];
          ret = edge * Math.hypot(r1x, r1y) * Math.hypot(r2x, r2y);
          psi = (Math.atan2(r1y, r1x) + Math.atan2(r2y, r2x)) * 0.5;
        }
        var brush = Math.pow(Math.sin(2 * psi), 2);
        var cr = channel(ret, 650) * brush, cg = channel(ret, 550) * brush, cb = channel(ret, 450) * brush;
        if (bulls && !biaxial) {
          var spot = Math.exp(-(r1x * r1x + r1y * r1y) / 0.012);
          cr = Math.max(cr, spot * 0.80); cg = Math.max(cg, spot * 0.72); cb = Math.max(cb, spot * 0.78);
        }
        px[o] = M.clamp01(cr * 1.2 + 0.02) * 255; px[o + 1] = M.clamp01(cg * 1.2 + 0.02) * 255; px[o + 2] = M.clamp01(cb * 1.2 + 0.03) * 255;
        px[o + 3] = M.clamp01((1 - r) * size * 0.5) * 255;
      }
    }
    g.putImageData(img, 0, 0);
    interfCache[key] = { cv: cv, k: JSON.stringify([c1, c2, edge]) };
    return cv;
  }

  var figs = {}, dict = {};
  var kit = {
    // The texts of the drawings are written in French in the code; a translation is a dictionary { "texte français": "translated text" }
    // loaded from /static/i18n/figures.<lang>.json (see tools/extract_figure_texts.py). Unknown texts stay as they are.
    t: function (text) { return Object.prototype.hasOwnProperty.call(dict, text) && dict[text] ? dict[text] : text; },
    M: M, C: C, alpha: alpha, mix: mix, brighter: brighter, V: V, add: add, mul: mul, rot: rot, WHITE: WHITE,
    strip: strip, interference: interference, bands: [["Violet", 400, 450], ["Bleu", 450, 490], ["Vert", 490, 560], ["Jaune", 560, 590], ["Orange", 590, 610], ["Rouge", 610, 700]],
    register: function (id, fn) { figs[id] = fn; }
  };
  window.LessonKit = kit;

  // ---- The player: one canvas per step, animated while it is on screen ----
  var reduced = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  function motionOff() { return reduced || document.documentElement.getAttribute("data-motion") === "off"; }

  function setup(host) {
    var cv = host.querySelector("canvas"), btn = host.querySelector("button");
    var id = host.getAttribute("data-fig"), focus = parseInt(host.getAttribute("data-focus"), 10) || 0;
    var dpr = Math.min(window.devicePixelRatio || 1, 2);
    cv.width = W * dpr; cv.height = H * dpr;
    var g = cv.getContext("2d"), board = new Board(g), clock = 0, last = 0, visible = false, playing = !motionOff(), raf = 0;
    function frame(now) {
      raf = 0;
      if (playing && last) clock += Math.min((now - last) / 1000, 0.1);
      last = now;
      g.setTransform(dpr, 0, 0, dpr, 0, 0);
      g.fillStyle = "#14171f";
      g.fillRect(0, 0, W, H);
      var fn = figs[id];
      if (fn) fn(board, focus, motionOff() && !playing && clock === 0 ? 2.5 : clock);
      else board.label("Schéma à venir", [0, 0], 16, C(0.5, 0.55, 0.63));
      if (visible && playing) raf = requestAnimationFrame(frame);
    }
    function start() { if (!raf) { last = 0; raf = requestAnimationFrame(frame); } }
    if ("IntersectionObserver" in window) {
      new IntersectionObserver(function (entries) {
        visible = entries[0].isIntersecting;
        if (visible) start(); else if (raf) { cancelAnimationFrame(raf); raf = 0; }
      }).observe(host);
    } else { visible = true; start(); }
    if (btn) {
      btn.textContent = playing ? btn.getAttribute("data-pause") : btn.getAttribute("data-play");
      btn.addEventListener("click", function () {
        playing = !playing;
        btn.textContent = playing ? btn.getAttribute("data-pause") : btn.getAttribute("data-play");
        if (playing) start(); else frame(performance.now());
      });
    }
    // The global "pause the animations" switch (see motion.js) stops the drawing at once; the button of the figure can start it again.
    window.addEventListener("gl-motion", function () {
      if (motionOff() && playing) {
        playing = false;
        if (btn) btn.textContent = btn.getAttribute("data-play");
      }
    });
    frame(performance.now());
  }

  function init() {
    var hosts = document.querySelectorAll(".lessonfig");
    if (!hosts.length) return;
    var url = (SELF && SELF.getAttribute("data-spectra")) || "/static/data/lesson_spectra.json";
    var lang = (document.documentElement.getAttribute("lang") || "fr").slice(0, 2).toLowerCase();
    var load = function (u) { return fetch(u).then(function (r) { return r.ok ? r.json() : {}; }).catch(function () { return {}; }); };
    Promise.all([load(url), lang === "fr" ? Promise.resolve({}) : load("/static/i18n/figures." + lang + ".json")]).then(function (res) {
      spectra = res[0] || {};
      dict = res[1] || {};
      stripCache = {};
      hosts.forEach(setup);
    });
  }
  if (document.readyState === "complete") init(); else document.addEventListener("DOMContentLoaded", init);
})();
