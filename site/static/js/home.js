// The sapphire of the home page: the same 3D viewer as the encyclopedia, turning slowly. Stops when the page is hidden or motion is reduced.
(function () {
  "use strict";
  var box = document.getElementById("hero-gem");
  if (!box || !window.GemView3D) return;
  var view = window.GemView3D.create(box);
  if (!view) return;
  view.canvas.width = view.canvas.height = 380;
  view.set({ shape: "coussin", style: "brillant", body: [0.07, 0.22, 0.80], ior: 1.766, disp: 0.011, depth: 1.0, milky: 0, dims: [9, 8, 5.2] });
  var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  function still() { return reduce || document.documentElement.getAttribute("data-motion") === "off"; }
  var last = 0, visible = true;
  if ("IntersectionObserver" in window) new IntersectionObserver(function (e) { visible = e[0].isIntersecting; }).observe(box);
  function tick(ts) {
    if (visible && !document.hidden && !still() && ts - last > 70) { last = ts; view.rotate(0.03, 0); }
    requestAnimationFrame(tick);
  }
  requestAnimationFrame(tick);
})();
