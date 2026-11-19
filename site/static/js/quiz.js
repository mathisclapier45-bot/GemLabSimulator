// Counter of the mixed quiz: how many questions are answered so far. The page works without it.
(function () {
  "use strict";
  var form = document.querySelector("form.quiz-mixed"), box = document.querySelector(".counter");
  if (!form || !box) return;
  var total = parseInt(box.getAttribute("data-total"), 10) || 0, label = box.getAttribute("data-label") || "";
  function update() {
    var names = {};
    form.querySelectorAll("input[type=radio]:checked").forEach(function (r) { names[r.name] = true; });
    box.textContent = label.replace("{n}", Object.keys(names).length).replace("{total}", total);
  }
  form.addEventListener("change", update);
  update();
})();
