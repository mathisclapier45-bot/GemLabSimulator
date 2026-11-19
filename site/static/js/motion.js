// "Pause the animations": a switch for the moving parts of the site (hero, ribbon of stones, drawings of the lessons).
// It starts off when the device asks for reduced motion, and the choice is remembered in this browser. The site works without it.
(function () {
  "use strict";
  var KEY = "gl_motion", root = document.documentElement;
  function stored() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function save(value) { try { localStorage.setItem(KEY, value); } catch (e) { /* private window: not remembered */ } }
  var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var state = stored() || (reduce ? "off" : "on");

  function apply() {
    root.setAttribute("data-motion", state);
    document.querySelectorAll(".motion-toggle").forEach(function (b) {
      b.textContent = state === "off" ? b.getAttribute("data-resume") : b.getAttribute("data-pause");
    });
    window.dispatchEvent(new Event("gl-motion"));
  }
  document.addEventListener("click", function (e) {
    var b = e.target.closest && e.target.closest(".motion-toggle");
    if (!b) return;
    state = state === "off" ? "on" : "off";
    save(state);
    apply();
  });
  apply();
})();
