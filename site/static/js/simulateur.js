// Starts the Unity simulator. The files come from /simulateur/fichiers/, which only serves logged-in members.
(function () {
  var base = "/simulateur/fichiers/";
  var canvas = document.getElementById("unity-canvas");
  var bar = document.getElementById("sim-bar");
  var loading = document.getElementById("sim-loading");
  var error = document.getElementById("sim-error");
  var instance = null;

  function fail(message) {
    loading.hidden = true;
    error.hidden = false;
    if (window.console) { console.error(message); }
  }

  var script = document.createElement("script");
  script.src = base + "WebCours.loader.js";
  script.onerror = function () { fail("loader not found"); };
  script.onload = function () {
    var config = {
      arguments: [],
      dataUrl: base + "WebCours.data.gz",
      frameworkUrl: base + "WebCours.framework.js.gz",
      codeUrl: base + "WebCours.wasm.gz",
      streamingAssetsUrl: "StreamingAssets",
      companyName: "GemLab",
      productName: "GemLabSimulator",
      productVersion: "1.0"
    };
    createUnityInstance(canvas, config, function (progress) {
      bar.style.width = (progress * 100) + "%";
    }).then(function (u) {
      instance = u;
      loading.hidden = true;
      canvas.focus();
    }).catch(fail);
  };
  document.body.appendChild(script);

  var full = document.getElementById("sim-full");
  if (full) {
    full.addEventListener("click", function () {
      if (instance) { instance.SetFullscreen(1); }
    });
  }
})();
