// 3D viewer of a cut stone, a port of the game's "realistic" renderer: the same facet geometry (exported from GemMesh.cs) and the same
// ray tracing through the planes of the stone (GemRealistic.shader): refraction, total internal reflection, dispersion, a studio of lights.
// Drag to turn the stone. Needs WebGL2; window.GemView3D.create() returns null when it is not available.
(function () {
  "use strict";
  var VS = "#version 300 es\n" +
    "in vec3 aPos; in vec3 aNormal; uniform mat3 uR; uniform float uScale;\n" +
    "out vec3 vPos; out vec3 vNormal;\n" +
    "void main(){ vec3 r = uR * aPos; gl_Position = vec4(r.x * uScale, r.y * uScale, -r.z * 0.3, 1.0); vPos = aPos; vNormal = aNormal; }";

  var FS = "#version 300 es\nprecision highp float; precision highp int;\n" +
    "uniform mat3 uR; uniform vec4 uPlanes[256]; uniform int uPlaneCount; uniform vec3 uBody;\n" +
    "uniform float uIor; uniform float uDisp; uniform float uDensity; uniform float uMilky; uniform float uExposure; uniform int uSmooth;\n" +
    "in vec3 vPos; in vec3 vNormal; out vec4 outColor;\n" +
    "vec3 ToWorld(vec3 d){ vec3 r = uR * d; return normalize(vec3(r.x, r.y, -r.z)); }\n" +
    "float Lamp(vec3 d, vec3 axis, float inner, float outer){ return smoothstep(inner, outer, dot(d, normalize(axis))); }\n" +
    "vec3 Env(vec3 d){\n" +
    "  vec3 c = mix(vec3(0.07,0.08,0.11), vec3(0.42,0.45,0.54), clamp(d.y*0.5+0.55,0.0,1.0)) * 0.95;\n" +
    "  c += vec3(0.95,0.97,1.0) * 0.55 * (1.0 - smoothstep(-0.9,-0.2,d.z));\n" +
    "  c += vec3(1.0,0.98,0.94) * 2.4 * Lamp(d, vec3(-0.50,0.65,-0.60), 0.80, 0.93);\n" +
    "  c += vec3(0.90,0.95,1.0) * 1.3 * Lamp(d, vec3(0.70,-0.22,-0.70), 0.84, 0.94);\n" +
    "  c += vec3(1.0) * 1.0 * Lamp(d, vec3(0.0,1.0,-0.15), 0.88, 0.96);\n" +
    "  float s = Lamp(d, vec3(-0.30,0.55,-0.78), 0.985, 0.998) + Lamp(d, vec3(0.42,0.30,-0.86), 0.985, 0.998) + Lamp(d, vec3(-0.62,-0.12,-0.78), 0.986, 0.998)\n" +
    "          + Lamp(d, vec3(0.10,-0.55,-0.83), 0.986, 0.998) + Lamp(d, vec3(0.70,0.62,-0.35), 0.987, 0.998) + Lamp(d, vec3(-0.80,0.30,-0.52), 0.987, 0.998)\n" +
    "          + Lamp(d, vec3(0.05,0.92,-0.38), 0.988, 0.998) + Lamp(d, vec3(0.60,-0.55,-0.58), 0.988, 0.998);\n" +
    "  c += vec3(1.0) * 8.0 * s;\n" +
    "  return c;\n}\n" +
    "float Trace(vec3 p, vec3 dir, float ior, float sigma, int channel){\n" +
    "  float acc = 0.0; float weight = 1.0;\n" +
    "  for (int bounce = 0; bounce < 8; bounce++){\n" +
    "    float tMin = 1.0e9; int hit = -1;\n" +
    "    for (int i = 0; i < 256; i++){\n" +
    "      if (i >= uPlaneCount) break;\n" +
    "      vec4 pl = uPlanes[i]; float den = dot(pl.xyz, dir);\n" +
    "      if (den > 1.0e-5){ float t = (pl.w - dot(pl.xyz, p)) / den; if (t > 1.0e-4 && t < tMin){ tMin = t; hit = i; } }\n" +
    "    }\n" +
    "    if (hit < 0) break;\n" +
    "    p += dir * tMin;\n" +
    "    weight *= exp(-sigma * tMin);\n" +
    "    vec3 n = uPlanes[hit].xyz; float cosi = dot(dir, n); float s2 = ior * ior * (1.0 - cosi * cosi);\n" +
    "    if (s2 >= 1.0){ dir = dir - 2.0 * cosi * n; p -= n * 1.0e-3; continue; }\n" +
    "    float cost = sqrt(1.0 - s2);\n" +
    "    float rs = (ior * cosi - cost) / (ior * cosi + cost); float rp = (cosi - ior * cost) / (cosi + ior * cost);\n" +
    "    float reflectance = 0.5 * (rs * rs + rp * rp);\n" +
    "    vec3 outDir = refract(dir, -n, ior); vec3 seen = Env(ToWorld(outDir));\n" +
    "    float through = channel == 0 ? seen.r : (channel == 1 ? seen.g : seen.b);\n" +
    "    acc += weight * (1.0 - reflectance) * through;\n" +
    "    weight *= reflectance; dir = dir - 2.0 * cosi * n; p -= n * 1.0e-3;\n" +
    "    if (weight < 0.004) break;\n" +
    "  }\n  return acc;\n}\n" +
    "void main(){\n" +
    "  vec3 n = normalize(vNormal);\n" +
    "  vec3 view = vec3(0.0, 0.0, -1.0) * uR;\n" +      // from the camera into the scene, in the object's space
    "  float facing = -dot(view, n); if (facing <= 0.0) discard;\n" +
    "  float cosi = clamp(facing, 0.0, 1.0); float f0 = pow((uIor - 1.0) / (uIor + 1.0), 2.0);\n" +
    "  float fresnel = f0 + (1.0 - f0) * pow(1.0 - cosi, 5.0);\n" +
    "  vec3 mirrored = Env(ToWorld(reflect(view, n)));\n" +
    "  vec3 body = max(uBody, vec3(0.03)); vec3 sigma = -log(body) * uDensity;\n" +
    "  vec3 nw = ToWorld(n);\n" +
    "  float diffuse = 0.28 + 0.72 * clamp(dot(nw, normalize(vec3(-0.5, 0.65, -0.6))), 0.0, 1.0);\n" +
    "  vec3 col;\n" +
    "  if (uSmooth == 1){\n" +
    "    col = body * diffuse * 1.6 + mirrored * fresnel * (0.6 + 0.9 * (1.0 - uMilky)) + body * 0.25 * (1.0 - uMilky) * pow(cosi, 2.0);\n" +
    "  } else {\n" +
    "    vec3 glass = vec3(0.0);\n" +
    "    for (int ch = 0; ch < 3; ch++){\n" +
    "      float iorC = uIor + float(ch - 1) * uDisp; float sig = ch == 0 ? sigma.r : (ch == 1 ? sigma.g : sigma.b);\n" +
    "      float seenC = Trace(vPos, refract(view, n, 1.0 / iorC), iorC, sig, ch);\n" +
    "      glass += vec3(ch == 0 ? 1.0 : 0.0, ch == 1 ? 1.0 : 0.0, ch == 2 ? 1.0 : 0.0) * seenC;\n" +
    "    }\n" +
    "    vec3 clear = glass * (1.0 - fresnel) + mirrored * fresnel;\n" +
    "    vec3 matte = body * diffuse * 1.6 + mirrored * fresnel * 0.6;\n" +
    "    col = mix(clear, matte, uMilky);\n" +
    "  }\n" +
    "  col = 1.0 - exp(-col * uExposure);\n" +
    "  outColor = vec4(col, 1.0);\n}";

  var cache = {};
  function fetchMesh(shape, style) {
    var key = shape + "__" + style;
    if (!cache[key]) cache[key] = fetch("/static/data/meshes/" + key + ".json").then(function (r) { return r.json(); });
    return cache[key];
  }
  var indexPromise = null;
  function fetchIndex() { return indexPromise || (indexPromise = fetch("/static/data/meshes/index.json").then(function (r) { return r.json(); })); }

  function compile(gl, type, src) {
    var s = gl.createShader(type); gl.shaderSource(s, src); gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
    return s;
  }
  function mul(a, b) { var o = []; for (var i = 0; i < 3; i++) { o[i] = []; for (var j = 0; j < 3; j++) { o[i][j] = a[i][0] * b[0][j] + a[i][1] * b[1][j] + a[i][2] * b[2][j]; } } return o; }
  function rotX(t) { var c = Math.cos(t), s = Math.sin(t); return [[1, 0, 0], [0, c, -s], [0, s, c]]; }
  function rotY(t) { var c = Math.cos(t), s = Math.sin(t); return [[c, 0, s], [0, 1, 0], [-s, 0, c]]; }

  function create(container) {
    var canvas = document.createElement("canvas"); canvas.className = "gem3d"; canvas.width = 420; canvas.height = 420;
    var gl = canvas.getContext("webgl2", { antialias: true, alpha: true, premultipliedAlpha: false });
    if (!gl) return null;
    var prog;
    try {
      prog = gl.createProgram();
      gl.attachShader(prog, compile(gl, gl.VERTEX_SHADER, VS)); gl.attachShader(prog, compile(gl, gl.FRAGMENT_SHADER, FS));
      gl.linkProgram(prog);
      if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(prog));
    } catch (e) { if (window.console) console.error(e); return null; }
    gl.useProgram(prog);
    var U = {}; ["uR", "uScale", "uPlanes", "uPlaneCount", "uBody", "uIor", "uDisp", "uDensity", "uMilky", "uExposure", "uSmooth"].forEach(function (n) { U[n] = gl.getUniformLocation(prog, n); });
    var aPos = gl.getAttribLocation(prog, "aPos"), aNormal = gl.getAttribLocation(prog, "aNormal");
    var vao = gl.createVertexArray(); gl.bindVertexArray(vao);
    var bufPos = gl.createBuffer(), bufNor = gl.createBuffer();
    container.innerHTML = ""; container.appendChild(canvas);

    var R = mul(rotY(0.5), rotX(-0.42));
    var model = null, params = null, dirty = false, token = 0;

    function upload(mesh, ratios) {
      var v = mesh.v, t = mesh.t, smooth = !!mesh.n;
      var pos = new Float32Array(t.length * 3), nor = new Float32Array(t.length * 3);
      var planes = [], seen = {};
      for (var k = 0; k < t.length; k += 3) {
        var P = [];
        for (var c = 0; c < 3; c++) { var i = t[k + c]; P.push([v[i * 3] * ratios[0], v[i * 3 + 1] * ratios[1], v[i * 3 + 2] * ratios[2]]); }
        var e1 = [P[1][0] - P[0][0], P[1][1] - P[0][1], P[1][2] - P[0][2]], e2 = [P[2][0] - P[0][0], P[2][1] - P[0][1], P[2][2] - P[0][2]];
        var fn = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]], len = Math.hypot(fn[0], fn[1], fn[2]) || 1;
        fn = [fn[0] / len, fn[1] / len, fn[2] / len];
        for (var c2 = 0; c2 < 3; c2++) {
          var o = (k + c2) * 3; pos[o] = P[c2][0]; pos[o + 1] = P[c2][1]; pos[o + 2] = P[c2][2];
          if (smooth) {
            var j = t[k + c2], nn = [mesh.n[j * 3] / ratios[0], mesh.n[j * 3 + 1] / ratios[1], mesh.n[j * 3 + 2] / ratios[2]], nl = Math.hypot(nn[0], nn[1], nn[2]) || 1;
            nor[o] = nn[0] / nl; nor[o + 1] = nn[1] / nl; nor[o + 2] = nn[2] / nl;
          } else { nor[o] = fn[0]; nor[o + 1] = fn[1]; nor[o + 2] = fn[2]; }
        }
        var fid = mesh.f[k / 3];
        if (!smooth && !seen[fid] && len > 1e-9) { seen[fid] = true; planes.push([fn[0], fn[1], fn[2], fn[0] * P[0][0] + fn[1] * P[0][1] + fn[2] * P[0][2]]); }
      }
      gl.bindVertexArray(vao);
      gl.bindBuffer(gl.ARRAY_BUFFER, bufPos); gl.bufferData(gl.ARRAY_BUFFER, pos, gl.STATIC_DRAW); gl.enableVertexAttribArray(aPos); gl.vertexAttribPointer(aPos, 3, gl.FLOAT, false, 0, 0);
      gl.bindBuffer(gl.ARRAY_BUFFER, bufNor); gl.bufferData(gl.ARRAY_BUFFER, nor, gl.STATIC_DRAW); gl.enableVertexAttribArray(aNormal); gl.vertexAttribPointer(aNormal, 3, gl.FLOAT, false, 0, 0);
      var flat = new Float32Array(256 * 4); planes.slice(0, 256).forEach(function (p, i) { flat.set(p, i * 4); });
      model = { count: t.length, planes: flat, planeCount: Math.min(planes.length, 256), smooth: smooth };
    }

    function render() {
      dirty = false;
      if (!model || !params) return;
      gl.viewport(0, 0, canvas.width, canvas.height);
      gl.clearColor(0, 0, 0, 0); gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT); gl.enable(gl.DEPTH_TEST);
      gl.useProgram(prog); gl.bindVertexArray(vao);
      var m = new Float32Array(9); for (var r = 0; r < 3; r++) for (var c = 0; c < 3; c++) m[c * 3 + r] = R[r][c];
      gl.uniformMatrix3fv(U.uR, false, m); gl.uniform1f(U.uScale, 1 / 1.3);
      gl.uniform4fv(U.uPlanes, model.planes); gl.uniform1i(U.uPlaneCount, model.planeCount);
      gl.uniform3f(U.uBody, params.body[0], params.body[1], params.body[2]);
      gl.uniform1f(U.uIor, Math.max(1.2, params.ior)); gl.uniform1f(U.uDisp, params.disp);
      gl.uniform1f(U.uDensity, 0.34 * (params.depth || 1)); gl.uniform1f(U.uMilky, params.milky || 0);
      gl.uniform1f(U.uExposure, model.smooth ? 1.3 : 1.35); gl.uniform1i(U.uSmooth, model.smooth ? 1 : 0);
      gl.drawArrays(gl.TRIANGLES, 0, model.count);
    }
    function schedule() { if (!dirty) { dirty = true; requestAnimationFrame(render); } }

    var drag = null;
    canvas.addEventListener("pointerdown", function (e) { drag = [e.clientX, e.clientY]; canvas.setPointerCapture(e.pointerId); });
    canvas.addEventListener("pointermove", function (e) {
      if (!drag) return;
      var dx = e.clientX - drag[0], dy = e.clientY - drag[1]; drag = [e.clientX, e.clientY];
      R = mul(mul(rotY(dx * 0.012), rotX(dy * 0.012)), R); schedule();
    });
    ["pointerup", "pointercancel"].forEach(function (n) { canvas.addEventListener(n, function () { drag = null; }); });

    return {
      canvas: canvas,
      // opts: shape, style (slugs), body [r,g,b], ior, disp, depth, milky, dims [lengthMm, widthMm, depthMm]
      set: function (opts) {
        var my = ++token; params = opts;
        return Promise.all([fetchIndex(), fetchMesh(opts.shape, opts.style)]).then(function (res) {
          if (my !== token) return;
          var ar = res[0].aspect[opts.shape] || 1, d = opts.dims, Lp = Math.sqrt(d[0] * d[1] / ar), Wp = Lp * ar, D = Math.max(d[2], 0.1), mx = Math.max(Lp, Wp, D);
          var ratios = [Lp / mx, Wp / mx, D / mx];
          upload(res[1], ratios); schedule();
        });
      },
      redraw: function (opts) { params = opts; schedule(); },
      rotate: function (dx, dy) { R = mul(mul(rotY(dx), rotX(dy)), R); schedule(); },
      index: fetchIndex
    };
  }
  window.GemView3D = { create: create };
})();
