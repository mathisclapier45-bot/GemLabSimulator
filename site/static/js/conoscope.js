// Interference figures, drawn pixel by pixel with the same model as the game (InterferenceImages.cs): dark brushes where the vibration
// direction matches a polarizer, coloured rings where the light is retarded. Uniaxial: one optic axis; biaxial: two; quartz: bull's eye.
(function () {
  "use strict";
  function channel(ret, wave) { var s = Math.sin(Math.PI * ret / wave); return s * s; }
  function rotate(v, deg) { var a = deg * Math.PI / 180, c = Math.cos(a), s = Math.sin(a); return [v[0] * c - v[1] * s, v[0] * s + v[1] * c]; }
  function clamp01(v) { return Math.max(0, Math.min(1, v)); }

  function draw(canvas) {
    var size = canvas.width, g = canvas.getContext("2d"), img = g.createImageData(size, size), px = img.data;
    var biaxial = canvas.getAttribute("data-kind") === "bi", bulls = canvas.getAttribute("data-bulls") === "1";
    var c1 = biaxial ? rotate([-0.42, 0], 45) : [0, 0], c2 = biaxial ? rotate([0.42, 0], 45) : [0, 0], edge = biaxial ? 3200 : 3600;
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
        px[o] = clamp01(cr * 1.2 + 0.02) * 255; px[o + 1] = clamp01(cg * 1.2 + 0.02) * 255; px[o + 2] = clamp01(cb * 1.2 + 0.03) * 255;
        px[o + 3] = clamp01((1 - r) * size * 0.5) * 255;
      }
    }
    g.putImageData(img, 0, 0);
  }
  document.querySelectorAll("canvas.conoscope").forEach(draw);
})();
