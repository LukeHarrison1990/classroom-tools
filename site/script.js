// Classroom Toolkit website. Copyright (c) 2026 Luke Harrison. SPDX-License-Identifier: MIT
(function () {
  "use strict";

  // ---- The traffic light in the hero: the same captions as the app's defaults ----
  var lamps = document.querySelectorAll(".lamp");
  var caption = document.getElementById("caption");
  var captions = ["Silent – working alone", "Quiet – partner voices", "Talk – group discussion"];
  var colours = ["var(--red)", "var(--amber)", "var(--green)"];
  var lit = 0;

  function show(i) {
    lit = i;
    for (var n = 0; n < lamps.length; n++) lamps[n].setAttribute("aria-pressed", n === i ? "true" : "false");
    caption.textContent = captions[i];
    caption.style.setProperty("--c", colours[i]);
  }

  for (var n = 0; n < lamps.length; n++) {
    lamps[n].addEventListener("click", function () { show(+this.getAttribute("data-i")); });
  }

  document.addEventListener("keydown", function (e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return;
    var t = e.target && e.target.tagName;
    if (t === "INPUT" || t === "TEXTAREA" || t === "SELECT") return;
    var k = e.key.toLowerCase();
    if (k === "r") show(0);
    else if (k === "a") show(1);
    else if (k === "g") show(2);
  });

  show(0);

  // ---- Download button: link straight to the latest release's zip ----
  var button = document.getElementById("download");
  var note = document.getElementById("download-note");
  var api = "https://api.github.com/repos/LukeHarrison1990/classroom-tools/releases/latest";

  if (window.fetch) {
    fetch(api, { headers: { Accept: "application/vnd.github+json" } })
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
      .then(function (rel) {
        var zip = (rel.assets || []).filter(function (a) { return /\.zip$/i.test(a.name); })[0];
        if (!zip) return;
        var version = String(rel.tag_name || "").replace(/^v/, "");
        var mb = (zip.size / 1048576).toFixed(1);
        button.href = zip.browser_download_url;
        button.textContent = "Download version " + version;
        note.textContent = "For Windows 10 and 11. A " + mb + " MB zip with nothing to install.";
      })
      .catch(function () { /* keep the link to the releases page */ });
  }
})();
