// Whispers of the Spirit — Review deck.
// Full-screen, layered audio review: time ruler, waveform, heat map (spectrogram), speech-like activity,
// pitch and markers, with slowed playback and clean-up. Self-contained: no CDN styles are needed.
(() => {
  if (window.__whispersDeckInstalled) return;
  window.__whispersDeckInstalled = true;

  // ---------- messaging (queued so rapid commands are not dropped by the WebView) ----------
  const queue = [];
  const send = (action, value) => {
    const url = 'whispers://' + action + (value === undefined ? '' : '?value=' + encodeURIComponent(value));
    if (action === 'deckview') { const i = queue.findIndex(u => u.startsWith('whispers://deckview')); if (i >= 0) queue.splice(i, 1); }
    queue.push(url);
  };
  setInterval(() => {
    const url = queue.shift(); if (!url) return;
    if (window.__whispersSend) window.__whispersSend(url); else window.location.href = url;
  }, 60);

  // ---------- styles ----------
  const css = `
  #wd{--bg:#020508;--panel:#0a0f18;--panel2:#0f1724;--line:#1a283f;--line2:#223552;--text:#cbd5e1;--dim:#7c8ba1;--cyan:#38bdf8;--cyan2:#a5f3fc;--green:#00ff88;--amber:#f59e0b;--red:#ef4444;
    position:fixed;inset:0;z-index:2147483000;display:none;grid-template-rows:auto auto minmax(0,1fr) auto auto;background:var(--bg);color:var(--text);
    font:12px/1.35 "Share Tech Mono","JetBrains Mono",Consolas,"Cascadia Mono",monospace;letter-spacing:.02em}
  #wd.open{display:grid}
  #wd *{box-sizing:border-box}
  #wd button,#wd select,#wd input{font:inherit;color:inherit}
  #wd .bar{display:flex;flex-wrap:wrap;align-items:center;gap:6px;padding:6px 10px;background:var(--panel);border-bottom:1px solid var(--line)}
  #wd header.bar{background:linear-gradient(#0b1220,#070b13);gap:12px}
  #wd .title{font-size:15px;letter-spacing:.18em;color:#e2f6ff;font-weight:700}
  #wd .title b{color:var(--cyan)}
  #wd .file{color:var(--green);overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:46vw}
  #wd .spacer{flex:1}
  #wd .grp{display:flex;align-items:center;gap:4px;padding:0 8px;border-left:1px solid var(--line)}
  #wd .grp:first-child{border-left:0;padding-left:0}
  #wd .lbl{color:var(--dim);font-size:10px;letter-spacing:.14em;text-transform:uppercase}
  #wd .btn{background:#07111b;border:1px solid var(--line2);border-radius:5px;padding:5px 9px;cursor:pointer;min-height:30px;white-space:nowrap}
  #wd .btn:hover{border-color:var(--cyan);color:var(--cyan2)}
  #wd .btn:focus-visible,#wd select:focus-visible,#wd input:focus-visible,#wd canvas:focus-visible{outline:2px solid var(--cyan);outline-offset:1px}
  #wd .btn[aria-pressed=true]{background:#0a3445;border-color:var(--cyan);color:var(--cyan2);box-shadow:0 0 10px -2px rgba(56,189,248,.6)}
  #wd .btn.play{min-width:46px;font-size:15px;color:var(--green);border-color:#0f5c3c}
  #wd .btn.stop{color:#f8a3a3;border-color:#5c1f1f}
  #wd .btn.close{border-color:#5c1f1f;color:#fca5a5}
  #wd .btn:disabled{opacity:.4;cursor:not-allowed}
  #wd select,#wd input[type=text]{background:#05080e;border:1px solid var(--line2);border-radius:5px;padding:5px 7px;min-height:30px;max-width:100%}
  #wd input[type=range]{accent-color:var(--amber);width:120px}
  #wd .readout{color:var(--amber);min-width:52px;text-align:right}
  #wd .sw{display:inline-flex;align-items:center;gap:7px;cursor:pointer;user-select:none}
  #wd .sw input{appearance:none;width:32px;height:17px;border-radius:9px;background:#142033;border:1px solid var(--line2);position:relative;cursor:pointer;margin:0;flex:none}
  #wd .sw input::after{content:"";position:absolute;top:2px;left:2px;width:11px;height:11px;border-radius:50%;background:#64748b;transition:left .12s}
  #wd .sw input:checked{background:#064e3b;border-color:var(--green)}
  #wd .sw input:checked::after{left:17px;background:var(--green);box-shadow:0 0 8px var(--green)}
  #wd main{display:grid;grid-template-columns:minmax(0,1fr) 270px;min-height:0}
  #wd .lanes{position:relative;min-height:0;border-right:1px solid var(--line);background:#03070c}
  #wd .lanes canvas{position:absolute;inset:0;width:100%;height:100%;touch-action:none;cursor:crosshair}
  #wd .lanes.nav canvas{cursor:grab}
  #wd .empty{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;text-align:center;padding:20px;color:var(--dim)}
  #wd .empty strong{color:var(--cyan2);font-size:16px;letter-spacing:.14em}
  #wd aside{overflow:auto;background:var(--panel);padding:8px 10px 16px}
  #wd aside h3{margin:14px 0 6px;font-size:10px;letter-spacing:.18em;color:var(--cyan);text-transform:uppercase;font-weight:700;border-bottom:1px solid var(--line);padding-bottom:4px}
  #wd aside h3:first-child{margin-top:4px}
  #wd .row{display:flex;align-items:center;justify-content:space-between;gap:8px;margin:5px 0}
  #wd .row select{width:150px}
  #wd .note{color:var(--dim);font-size:10.5px;margin:4px 0}
  #wd .mk{display:grid;grid-template-columns:auto 1fr auto auto;gap:6px;align-items:center;padding:4px 0;border-bottom:1px dashed var(--line)}
  #wd .mk .t{color:var(--amber)}
  #wd .mk .n{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
  #wd .mk .btn{min-height:24px;padding:2px 7px}
  #wd .ov{height:46px;padding:0;border-top:1px solid var(--line);background:#03070c;position:relative}
  #wd .ov canvas{width:100%;height:100%;display:block;cursor:pointer;touch-action:none}
  #wd footer.bar{border-top:1px solid var(--line);border-bottom:0;font-size:11px;color:var(--dim);gap:16px}
  #wd footer .st{color:var(--green)}
  #wd footer .cur{color:var(--cyan2)}
  @media (max-width:900px){
    #wd{grid-template-rows:auto auto auto auto auto;overflow:auto}
    #wd main{grid-template-columns:1fr;min-height:auto}
    #wd .lanes{height:62vh;border-right:0}
    #wd aside{border-top:1px solid var(--line);overflow:visible}
    #wd .file{max-width:60vw}
  }`;
  const style = document.createElement('style'); style.textContent = css; document.head.append(style);

  // ---------- markup ----------
  const deck = document.createElement('section');
  deck.id = 'wd'; deck.setAttribute('role', 'dialog'); deck.setAttribute('aria-label', 'Review deck');
  deck.innerHTML = `
  <header class="bar"><span class="title"><b>◆</b> REVIEW DECK</span><span class="file" data-file>No recording loaded</span><span class="spacer"></span>
    <button class="btn" data-act="import" title="Copy an MP3, WAV or FLAC file into the review folder and analyse it">⇪ Import audio</button>
    <select data-takes aria-label="Saved recordings"><option value="">Saved takes…</option></select>
    <button class="btn close" data-act="close" title="Close the Review deck (Esc)">✕ Close</button></header>
  <div class="bar" role="toolbar" aria-label="Playback">
    <div class="grp"><button class="btn" data-act="home" title="Go to start (Home)" aria-label="Go to start">|◀</button>
      <button class="btn" data-act="back" title="Back 2 seconds (←)" aria-label="Back 2 seconds">◀◀</button>
      <button class="btn play" data-act="play" title="Play / pause (Space)" aria-label="Play">▶</button>
      <button class="btn stop" data-act="stop" title="Stop" aria-label="Stop">■</button>
      <button class="btn" data-act="fwd" title="Forward 2 seconds (→)" aria-label="Forward 2 seconds">▶▶</button>
      <button class="btn" data-act="loop" aria-pressed="false" title="Loop the selection (L)">⟲ Loop</button></div>
    <div class="grp"><span class="lbl">Speed</span>
      <button class="btn" data-speed="1">1×</button><button class="btn" data-speed="0.75">0.75×</button><button class="btn" data-speed="0.5">0.5×</button><button class="btn" data-speed="0.25">0.25×</button><button class="btn" data-speed="0.1">0.1×</button>
      <input type="range" min="0.1" max="1" step="0.05" value="1" data-speedslider aria-label="Playback speed"><span class="readout" data-speedread>1.00×</span>
      <label class="sw" title="On: words get longer but voices keep their normal pitch. Off: tape-style, the pitch drops with the speed."><input type="checkbox" data-pitch checked>Keep voice pitch</label></div>
    <div class="grp"><span class="lbl">Tool</span>
      <button class="btn" data-tool="select" aria-pressed="true" title="Drag to select a range; click to place the playhead">⬚ Select</button>
      <button class="btn" data-tool="nav" aria-pressed="false" title="Drag to move along the recording">✋ Navigate</button></div>
    <div class="grp"><span class="lbl">Zoom</span>
      <button class="btn" data-act="zoomout" title="Zoom out (−)" aria-label="Zoom out">－</button><button class="btn" data-act="zoomin" title="Zoom in (+)" aria-label="Zoom in">＋</button>
      <button class="btn" data-act="zoomsel" title="Zoom to the selection">⬚→</button><button class="btn" data-act="fit" title="Show the whole recording (F)">Fit</button></div>
  </div>
  <main>
    <div class="lanes"><canvas tabindex="0" aria-label="Recording lanes: waveform, heat map, speech-like activity, pitch and markers"></canvas>
      <div class="empty" data-empty><strong>NO RECORDING IN THE DECK</strong><span>Import an MP3, WAV or FLAC file, or pick a saved take at the top right.<br>Ghost box sessions you record are listed there automatically.</span>
        <button class="btn" data-act="import">⇪ Import audio</button></div></div>
    <aside aria-label="Layers and tools">
      <h3>Layers</h3>
      <label class="row sw"><span>Waveform</span><input type="checkbox" data-layer="wave" checked></label>
      <label class="row sw"><span>Heat map (spectrogram)</span><input type="checkbox" data-layer="heat" checked></label>
      <label class="row sw"><span>Speech-like activity</span><input type="checkbox" data-layer="voice" checked></label>
      <label class="row sw"><span>Pitch line</span><input type="checkbox" data-layer="pitch" checked></label>
      <label class="row sw"><span>Pitch over heat map</span><input type="checkbox" data-layer="overlay"></label>
      <label class="row sw"><span>Markers</span><input type="checkbox" data-layer="marks" checked></label>
      <h3>Heat map</h3>
      <div class="row"><span>Colours</span><select data-palette><option value="aether">Aether</option><option value="phosphor">Phosphor</option><option value="thermal">Thermal</option><option value="grey">Grey</option></select></div>
      <div class="row"><span>Range</span><select data-range><option value="4000">Voices (to 4 kHz)</option><option value="8000">To 8 kHz</option><option value="12000">Full (to 12 kHz)</option></select></div>
      <div class="row"><span>Brightness</span><input type="range" min="-0.4" max="0.4" step="0.02" value="0" data-bright aria-label="Heat map brightness"></div>
      <div class="row"><span>Contrast</span><input type="range" min="0.5" max="3" step="0.05" value="1.3" data-contrast aria-label="Heat map contrast"></div>
      <label class="row sw"><span>Auto level</span><input type="checkbox" data-auto checked></label>
      <div class="row"><span>Follow playback</span><select data-follow><option value="page">Page</option><option value="scroll">Scroll</option><option value="off">Off</option></select></div>
      <h3>Clean-up while listening</h3>
      <label class="row sw" title="Boosts 180 Hz–4.2 kHz where speech sits and evens out the level"><span>Voice focus</span><input type="checkbox" data-clean="voice"></label>
      <label class="row sw" title="Reduces steady background hiss"><span>Hiss reduction</span><input type="checkbox" data-clean="hiss"></label>
      <label class="row sw" title="Removes 50 Hz mains hum and its harmonics"><span>Hum removal (50 Hz)</span><input type="checkbox" data-clean="hum"></label>
      <label class="row sw" title="Smooths clicks and crackle"><span>Click removal</span><input type="checkbox" data-clean="clicks"></label>
      <p class="note">Applied to what you hear only. The original recording is never changed. Strong clean-up can also remove faint sounds — compare with it off.</p>
      <h3>Markers</h3>
      <div class="row"><input type="text" data-note placeholder="What do you hear? (M)" aria-label="Marker note" style="flex:1;min-width:0"><button class="btn" data-act="mark">+ Add</button></div>
      <p class="note">Adds a marker at the playhead. Click a marker to jump to it.</p>
      <div data-marks></div>
      <h3>Session &amp; export</h3>
      <div class="row"><input type="text" data-session value="Investigation" aria-label="Session name" style="flex:1;min-width:0"><button class="btn" data-act="save">Save</button></div>
      <div class="row"><button class="btn" data-act="wav" style="flex:1">Export selection WAV</button><button class="btn" data-act="mp3" style="flex:1">MP3</button></div>
      <p class="note">Measurements show where speech-like sound is and make it easier to hear. They cannot tell who is speaking or where a sound came from.</p>
    </aside>
  </main>
  <div class="ov"><canvas aria-label="Whole recording overview. Click to move the view."></canvas></div>
  <footer class="bar"><span data-hint>Drag to select • click to place the playhead • mouse wheel to zoom • Shift+wheel to scroll • Space to play</span><span class="spacer"></span><span class="cur" data-cursor></span><span class="st" data-status></span></footer>`;
  document.body.append(deck);
  const $ = q => deck.querySelector(q), $$ = q => [...deck.querySelectorAll(q)];
  const lanesBox = $('.lanes'), canvas = $('.lanes canvas'), ovCanvas = $('.ov canvas');
  const ctx = canvas.getContext('2d'), ovCtx = ovCanvas.getContext('2d');

  // ---------- state ----------
  const S = {
    open: false, duration: 0, file: '', vs: 0, ve: 1, overview: null, view: null,
    playhead: 0, playing: false, paused: false, selS: 0, selE: 0, markers: [], speed: 1, keepPitch: true, loop: false,
    tool: 'select', layers: { wave: true, heat: true, voice: true, pitch: true, overlay: false, marks: true },
    palette: 'aether', fmax: 4000, bright: 0, contrast: 1.3, auto: true, follow: 'page', cursor: null, takes: [], baseDirty: true,
  };
  const b64 = s => { const bin = atob(s), out = new Uint8Array(bin.length); for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i); return out; };
  const fmt = t => { if (!isFinite(t)) t = 0; const m = Math.floor(t / 60), s = t - m * 60; return m + ':' + (s < 10 ? '0' : '') + s.toFixed(t < 600 ? 3 : 1); };
  const clamp = (v, a, b) => Math.min(b, Math.max(a, v));

  // ---------- palettes ----------
  const stops = {
    aether: [[0, '#020508'], [.22, '#0b1f5c'], [.42, '#1171b8'], [.58, '#19c2c2'], [.72, '#5fe36a'], [.86, '#f5d33b'], [1, '#ff3b2f']],
    phosphor: [[0, '#010402'], [.3, '#003b1f'], [.6, '#00a85a'], [.85, '#00ff88'], [1, '#e2fff0']],
    thermal: [[0, '#000000'], [.3, '#3b0a0a'], [.55, '#b3200e'], [.78, '#f59e0b'], [.92, '#fff1b8'], [1, '#ffffff']],
    grey: [[0, '#000000'], [1, '#ffffff']],
  };
  const luts = {};
  const lut = name => {
    if (luts[name]) return luts[name];
    const st = stops[name], out = new Uint8Array(256 * 3), hex = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
    for (let i = 0; i < 256; i++) {
      const v = i / 255; let k = 0; while (k < st.length - 2 && v > st[k + 1][0]) k++;
      const [p0, c0] = st[k], [p1, c1] = st[k + 1], f = clamp((v - p0) / (p1 - p0 || 1), 0, 1), a = hex(c0), b = hex(c1);
      for (let j = 0; j < 3; j++) out[i * 3 + j] = Math.round(a[j] + (b[j] - a[j]) * f);
    }
    return luts[name] = out;
  };
  let levelLo = 0, levelHi = 255;
  const computeAutoLevel = d => {
    const h = new Uint32Array(256); for (const v of d.heat) h[v]++;
    const n = d.heat.length; let acc = 0, lo = 0, hi = 255;
    for (let i = 0; i < 256; i++) { acc += h[i]; if (acc < n * .05) lo = i; if (acc < n * .997) hi = i; }
    levelLo = lo; levelHi = Math.max(lo + 20, hi);
  };

  // ---------- data from native ----------
  window.whispersDeckOpen = info => {
    S.duration = info.duration; S.file = info.file; S.vs = 0; S.ve = info.duration || 1; S.overview = S.view = null;
    S.playhead = 0; S.selS = 0; S.selE = info.duration; S.markers = [];
    $('[data-file]').textContent = info.file + ' • ' + fmt(info.duration);
    show();
  };
  window.whispersDeckData = d => {
    const lanes = { ...d, wmin: new Int8Array(b64(d.wmin).buffer), wmax: new Int8Array(b64(d.wmax).buffer), heat: b64(d.heat), voice: b64(d.voice), pitch: new Uint16Array(b64(d.pitch).buffer), level: b64(d.level) };
    if (d.kind === 'overview') { S.overview = lanes; computeAutoLevel(lanes); requestView(); drawOverview(); }
    else S.view = lanes;
    S.baseDirty = true; draw();
  };
  window.whispersDeckPlayhead = t => { S.playhead = t; follow(); draw(); drawOverview(); };
  window.whispersDeckShow = () => show();
  const previous = window.whispersNativeUpdate;
  window.whispersNativeUpdate = state => {
    try { previous?.(state); } catch (e) { }
    S.playing = !!state.playing; S.paused = !!state.paused;
    const takes = state.takes || [];
    if (JSON.stringify(takes) !== JSON.stringify(S.takes)) {
      S.takes = takes; $('[data-takes]').replaceChildren(new Option(takes.length ? 'Saved takes…' : 'No saved takes yet', ''), ...takes.map((n, i) => new Option(n, i)));
    }
    const dk = state.deck; if (dk) {
      if (!S.playing) S.playhead = dk.playhead;
      S.speed = dk.speed; S.keepPitch = dk.keepPitch; S.loop = dk.loop;
      if (!dragging && dk.loaded) { S.selS = dk.rangeStart; S.selE = dk.rangeEnd; }
      const mk = JSON.stringify(dk.markers); if (mk !== JSON.stringify(S.markers)) { S.markers = dk.markers; renderMarkers(); S.baseDirty = true; }
      for (const box of $$('[data-clean]')) box.checked = dk.cleanup.includes(box.dataset.clean);
      $('[data-status]').textContent = dk.status;
    }
    syncControls(); draw(); drawOverview();
  };

  // ---------- view ----------
  let viewTimer = 0;
  const requestView = () => {
    clearTimeout(viewTimer);
    viewTimer = setTimeout(() => { if (S.duration > 0 && S.open) send('deckview', S.vs.toFixed(4) + ':' + S.ve.toFixed(4) + ':' + Math.round(clamp(canvas.clientWidth, 100, 2400))); }, 120);
  };
  const setView = (vs, ve) => {
    const min = Math.min(0.05, S.duration || 0.05), len = clamp(ve - vs, min, S.duration || 1);
    vs = clamp(vs, 0, Math.max(0, S.duration - len)); S.vs = vs; S.ve = vs + len; S.baseDirty = true; draw(); drawOverview(); requestView();
  };
  const zoom = (factor, at) => { at ??= (S.vs + S.ve) / 2; const len = (S.ve - S.vs) * factor; const f = (at - S.vs) / (S.ve - S.vs); setView(at - len * f, at - len * f + len); };
  const follow = () => {
    if (!S.playing || S.follow === 'off') return;
    const len = S.ve - S.vs;
    if (S.follow === 'scroll') { if (S.playhead > S.vs + len * .5 || S.playhead < S.vs) setView(S.playhead - len * .5, S.playhead + len * .5); }
    else if (S.playhead > S.ve || S.playhead < S.vs) setView(S.playhead - len * .02, S.playhead + len * .98);
  };

  // ---------- layout ----------
  const layout = (w, h) => {
    const L = S.layers, rows = [], ruler = 22;
    let y = ruler; const fixed = { voice: 44, pitch: 84, marks: 24 };
    let flexTotal = 0; const flex = [];
    if (L.wave) flex.push(['wave', L.heat ? 1 : 3]);
    if (L.heat) flex.push(['heat', 3]);
    for (const [, f] of flex) flexTotal += f;
    const fixedSum = (L.voice ? fixed.voice : 0) + (L.pitch ? fixed.pitch : 0) + (L.marks ? fixed.marks : 0) + 4 * 6;
    const avail = Math.max(60, h - ruler - fixedSum);
    for (const key of ['wave', 'heat', 'voice', 'pitch', 'marks']) {
      if (!L[key]) continue;
      const f = flex.find(x => x[0] === key), hh = f ? Math.max(40, avail * f[1] / (flexTotal || 1)) : fixed[key];
      rows.push({ key, y, h: hh }); y += hh + 6;
    }
    return { ruler, rows, w, h };
  };

  // ---------- drawing ----------
  const base = document.createElement('canvas'), bctx = base.getContext('2d');
  let geo = null;
  const dataFor = () => {
    const v = S.view, eps = (S.ve - S.vs) * 0.002;
    if (v && v.start <= S.vs + eps && v.end >= S.ve - eps && (v.end - v.start) < (S.ve - S.vs) * 1.6) return v;
    return S.overview;
  };
  const colAt = (d, t) => clamp(Math.floor((t - d.start) / (d.end - d.start) * d.cols), 0, d.cols - 1);
  const xOf = t => (t - S.vs) / (S.ve - S.vs) * geo.w;
  const tOf = x => S.vs + x / geo.w * (S.ve - S.vs);
  const laneLabel = (c, text, y) => { c.font = '10px Consolas, monospace'; c.fillStyle = 'rgba(2,5,8,.75)'; const w = c.measureText(text).width + 10; c.fillRect(4, y + 3, w, 15); c.fillStyle = '#7dd3fc'; c.fillText(text, 9, y + 14); };
  const freqY = (hz, r) => r.y + r.h - Math.log(hz / 50) / Math.log(S.fmax / 50) * r.h;
  const pitchY = (hz, r) => r.y + r.h - 4 - Math.log(clamp(hz, 60, 600) / 60) / Math.log(10) * (r.h - 8);

  function drawBase() {
    const dpr = window.devicePixelRatio || 1, w = canvas.clientWidth, h = canvas.clientHeight;
    base.width = Math.max(1, Math.round(w * dpr)); base.height = Math.max(1, Math.round(h * dpr));
    const c = bctx; c.setTransform(dpr, 0, 0, dpr, 0, 0); c.fillStyle = '#03070c'; c.fillRect(0, 0, w, h);
    geo = layout(w, h); const d = dataFor();
    // ruler
    c.fillStyle = '#07111b'; c.fillRect(0, 0, w, geo.ruler); c.strokeStyle = '#1a283f'; c.beginPath(); c.moveTo(0, geo.ruler - .5); c.lineTo(w, geo.ruler - .5); c.stroke();
    const span = S.ve - S.vs, target = span / Math.max(1, w / 110), steps = [0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600];
    const step = steps.find(s => s >= target) || 600;
    c.font = '10px Consolas, monospace'; c.fillStyle = '#7c8ba1'; c.strokeStyle = '#223552';
    for (let t = Math.ceil(S.vs / step) * step; t <= S.ve; t += step) {
      const x = Math.round(xOf(t)) + .5; c.beginPath(); c.moveTo(x, geo.ruler - 7); c.lineTo(x, geo.ruler); c.stroke(); c.fillText(fmt(t).replace(/0+$/, '').replace(/\.$/, ''), x + 3, 13);
    }
    for (const r of geo.rows) {
      c.fillStyle = '#05090f'; c.fillRect(0, r.y, w, r.h);
      if (!d) continue;
      const cols = Math.round(w);
      if (r.key === 'wave') {
        const mid = r.y + r.h / 2; c.strokeStyle = 'rgba(56,189,248,.18)'; c.beginPath(); c.moveTo(0, mid); c.lineTo(w, mid); c.stroke();
        c.fillStyle = '#67e8f9';
        for (let x = 0; x < cols; x++) {
          const a = colAt(d, tOf(x)), b = colAt(d, tOf(x + 1)); let lo = 127, hi = -127;
          for (let i = a; i <= Math.max(a, b - 1); i++) { lo = Math.min(lo, d.wmin[i]); hi = Math.max(hi, d.wmax[i]); }
          const y0 = mid - hi / 127 * (r.h / 2 - 2), y1 = mid - lo / 127 * (r.h / 2 - 2); c.fillRect(x, y0, 1, Math.max(1, y1 - y0));
        }
        laneLabel(c, 'WAVEFORM', r.y);
      } else if (r.key === 'heat') {
        const W = Math.max(1, Math.round(w)), H = Math.max(1, Math.round(r.h)), img = c.createImageData(W, H), px = img.data, L = lut(S.palette);
        const lo = S.auto ? levelLo : 0, hi = S.auto ? levelHi : 255, scale = 1 / Math.max(1, hi - lo);
        const rowBin = new Float32Array(H);
        for (let y = 0; y < H; y++) { const hz = 50 * Math.pow(S.fmax / 50, (H - 1 - y) / (H - 1)); rowBin[y] = clamp(Math.log(hz / d.minHz) / Math.log(d.maxHz / d.minHz) * d.bins, 0, d.bins - 1.001); }
        for (let x = 0; x < W; x++) {
          const ci = colAt(d, tOf(x + .5)), off = ci * d.bins;
          for (let y = 0; y < H; y++) {
            const fb = rowBin[y], k = fb | 0, f = fb - k, raw = d.heat[off + k] * (1 - f) + d.heat[off + k + 1] * f;
            let v = (raw - lo) * scale; v = (v - .5) * S.contrast + .5 + S.bright; const li = clamp(Math.round(v * 255), 0, 255) * 3, p = (y * W + x) * 4;
            px[p] = L[li]; px[p + 1] = L[li + 1]; px[p + 2] = L[li + 2]; px[p + 3] = 255;
          }
        }
        const tmp = document.createElement('canvas'); tmp.width = W; tmp.height = H; tmp.getContext('2d').putImageData(img, 0, 0);
        c.drawImage(tmp, 0, r.y, w, r.h);
        // frequency grid
        c.font = '10px Consolas, monospace';
        for (const hz of [100, 200, 500, 1000, 2000, 4000, 8000]) {
          if (hz >= S.fmax) continue; const y = Math.round(freqY(hz, r)) + .5;
          c.strokeStyle = 'rgba(255,255,255,.10)'; c.beginPath(); c.moveTo(0, y); c.lineTo(w, y); c.stroke();
          c.fillStyle = 'rgba(226,246,255,.65)'; c.fillText(hz >= 1000 ? hz / 1000 + 'k' : String(hz), w - 30, y - 2);
        }
        if (S.layers.overlay) {
          for (let x = 0; x < cols; x++) {
            const p = d.pitch[colAt(d, tOf(x + .5))]; if (!p) continue; const hz = p / 10; if (hz >= S.fmax) continue; const y = freqY(hz, r);
            c.fillStyle = '#020508'; c.fillRect(x, y - 2.5, 1.5, 5); c.fillStyle = '#a5f3fc'; c.fillRect(x, y - 1, 1.5, 2);
          }
        }
        laneLabel(c, 'HEAT MAP • frequency over time', r.y);
      } else if (r.key === 'voice') {
        for (let x = 0; x < cols; x++) {
          const v = d.voice[colAt(d, tOf(x + .5))] / 255; if (v < .02) continue;
          c.fillStyle = v > .6 ? '#00ff88' : v > .3 ? '#7ddc6a' : '#2f5f4a'; const hh = v * (r.h - 4); c.fillRect(x, r.y + r.h - 2 - hh, 1, hh);
        }
        c.strokeStyle = 'rgba(245,158,11,.35)'; c.setLineDash([3, 4]); const ty = r.y + r.h - 2 - .5 * (r.h - 4); c.beginPath(); c.moveTo(0, ty); c.lineTo(w, ty); c.stroke(); c.setLineDash([]);
        laneLabel(c, 'SPEECH-LIKE ACTIVITY', r.y);
      } else if (r.key === 'pitch') {
        c.font = '10px Consolas, monospace';
        for (const hz of [100, 200, 400]) { const y = Math.round(pitchY(hz, r)) + .5; c.strokeStyle = 'rgba(255,255,255,.08)'; c.beginPath(); c.moveTo(0, y); c.lineTo(w, y); c.stroke(); c.fillStyle = 'rgba(203,213,225,.5)'; c.fillText(hz + ' Hz', w - 44, y - 2); }
        let prev = null;
        for (let x = 0; x < cols; x++) {
          const ci = colAt(d, tOf(x + .5)), p = d.pitch[ci];
          if (!p) { prev = null; continue; }
          const y = pitchY(p / 10, r), v = d.voice[ci] / 255;
          c.strokeStyle = c.fillStyle = v > .5 ? '#fbbf24' : '#b45309';
          if (prev && Math.abs(prev.y - y) < 18) { c.beginPath(); c.moveTo(prev.x, prev.y); c.lineTo(x, y); c.lineWidth = 2; c.stroke(); c.lineWidth = 1; }
          else c.fillRect(x - 1, y - 1, 2, 2);
          prev = { x, y };
        }
        laneLabel(c, 'PITCH • rise and fall of a voice', r.y);
      } else if (r.key === 'marks') {
        laneLabel(c, 'MARKERS', r.y);
      }
    }
    // markers across lanes
    if (S.layers.marks && geo.rows.length) {
      const top = geo.ruler, bottom = geo.rows.at(-1).y + geo.rows.at(-1).h, mr = geo.rows.find(r => r.key === 'marks');
      c.font = '11px Consolas, monospace';
      S.markers.forEach((m, i) => {
        if (m.t < S.vs || m.t > S.ve) return; const x = Math.round(xOf(m.t)) + .5;
        c.strokeStyle = 'rgba(245,158,11,.55)'; c.setLineDash([4, 3]); c.beginPath(); c.moveTo(x, top); c.lineTo(x, bottom); c.stroke(); c.setLineDash([]);
        if (mr) {
          const label = (i + 1) + (m.note ? ' ' + m.note : ''), tw = Math.min(220, c.measureText(label).width + 10);
          c.fillStyle = '#3a2405'; c.fillRect(x, mr.y + 3, tw, mr.h - 6); c.fillStyle = '#fbbf24'; c.fillText(label.length > 34 ? label.slice(0, 33) + '…' : label, x + 5, mr.y + mr.h / 2 + 4);
        }
      });
    }
    S.baseDirty = false;
  }

  function draw() {
    if (!S.open) return;
    const dpr = window.devicePixelRatio || 1, w = canvas.clientWidth, h = canvas.clientHeight;
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) { canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr); S.baseDirty = true; }
    $('[data-empty]').style.display = S.duration > 0 ? 'none' : 'flex';
    if (S.duration <= 0) { ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.fillStyle = '#03070c'; ctx.fillRect(0, 0, canvas.width, canvas.height); return; }
    if (S.baseDirty || !geo) drawBase();
    ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.drawImage(base, 0, 0); ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    const top = geo.ruler, bottom = geo.rows.length ? geo.rows.at(-1).y + geo.rows.at(-1).h : h;
    // selection
    const sa = Math.min(S.selS, S.selE), sb = Math.max(S.selS, S.selE);
    if (sb - sa > 0.0005 && sb - sa < S.duration - 0.01) {
      const x0 = xOf(sa), x1 = xOf(sb);
      ctx.fillStyle = 'rgba(245,158,11,.14)'; ctx.fillRect(x0, top, x1 - x0, bottom - top);
      ctx.strokeStyle = '#f59e0b'; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(x0, top); ctx.lineTo(x0, bottom); ctx.moveTo(x1, top); ctx.lineTo(x1, bottom); ctx.stroke(); ctx.lineWidth = 1;
      ctx.fillStyle = '#f59e0b'; ctx.font = '10px Consolas, monospace'; ctx.fillText(fmt(sb - sa) + ' selected', Math.max(2, x0 + 4), top + 11 + 0);
    }
    // cursor crosshair
    if (S.cursor) { ctx.strokeStyle = 'rgba(165,243,252,.35)'; ctx.beginPath(); ctx.moveTo(S.cursor.x + .5, top); ctx.lineTo(S.cursor.x + .5, bottom); ctx.stroke(); }
    // playhead
    const px = xOf(S.playhead);
    if (px >= -1 && px <= w + 1) {
      ctx.strokeStyle = S.playing ? '#ff2a2a' : '#f8fafc'; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(px, 0); ctx.lineTo(px, bottom); ctx.stroke(); ctx.lineWidth = 1;
      ctx.fillStyle = ctx.strokeStyle; ctx.beginPath(); ctx.moveTo(px - 5, 0); ctx.lineTo(px + 5, 0); ctx.lineTo(px, 7); ctx.fill();
    }
  }

  function drawOverview() {
    if (!S.open) return;
    const dpr = window.devicePixelRatio || 1, w = ovCanvas.clientWidth, h = ovCanvas.clientHeight;
    ovCanvas.width = Math.round(w * dpr); ovCanvas.height = Math.round(h * dpr);
    const c = ovCtx; c.setTransform(dpr, 0, 0, dpr, 0, 0); c.fillStyle = '#03070c'; c.fillRect(0, 0, w, h);
    const d = S.overview; if (!d || S.duration <= 0) return;
    for (let x = 0; x < w; x++) {
      const ci = clamp(Math.floor(x / w * d.cols), 0, d.cols - 1), v = d.voice[ci] / 255;
      const a = d.wmax[ci] / 127, b = d.wmin[ci] / 127;
      c.fillStyle = v > .5 ? '#00c46a' : '#2a6f8f'; c.fillRect(x, h / 2 - a * (h / 2 - 3), 1, Math.max(1, (a - b) * (h / 2 - 3)));
    }
    const X = t => t / S.duration * w;
    const sa = Math.min(S.selS, S.selE), sb = Math.max(S.selS, S.selE);
    if (sb - sa > 0.0005 && sb - sa < S.duration - 0.01) { c.fillStyle = 'rgba(245,158,11,.25)'; c.fillRect(X(sa), 0, Math.max(1, X(sb) - X(sa)), h); }
    c.fillStyle = '#f59e0b'; for (const m of S.markers) c.fillRect(X(m.t), 0, 1.5, 8);
    c.strokeStyle = '#38bdf8'; c.lineWidth = 1.5; c.strokeRect(X(S.vs) + .5, 1, Math.max(3, X(S.ve) - X(S.vs) - 1), h - 2); c.lineWidth = 1;
    c.fillStyle = S.playing ? '#ff2a2a' : '#f8fafc'; c.fillRect(X(S.playhead) - .5, 0, 1.5, h);
  }

  // ---------- interaction: lanes ----------
  let dragging = null;
  canvas.addEventListener('pointerdown', e => {
    if (S.duration <= 0) return; canvas.focus(); canvas.setPointerCapture(e.pointerId);
    const x = e.offsetX, t = tOf(x), nav = S.tool === 'nav' || e.button === 1 || e.altKey;
    dragging = { x, t, vs: S.vs, ve: S.ve, nav, moved: false };
    if (nav) lanesBox.classList.add('nav');
  });
  canvas.addEventListener('pointermove', e => {
    if (S.duration <= 0 || !geo) return;
    const x = e.offsetX, y = e.offsetY, t = clamp(tOf(x), 0, S.duration); S.cursor = { x, y };
    // readout
    const d = dataFor(); let text = fmt(t);
    const r = geo.rows.find(r => y >= r.y && y <= r.y + r.h);
    if (d) {
      const ci = colAt(d, t);
      if (r?.key === 'heat') text += ' • ' + Math.round(50 * Math.pow(S.fmax / 50, (r.y + r.h - y) / r.h)) + ' Hz';
      if (d.pitch[ci]) text += ' • pitch ' + Math.round(d.pitch[ci] / 10) + ' Hz';
      text += ' • speech-like ' + Math.round(d.voice[ci] / 2.55) + '%';
    }
    $('[data-cursor]').textContent = text;
    if (dragging) {
      if (Math.abs(x - dragging.x) > 3) dragging.moved = true;
      if (dragging.nav) { const dt = (x - dragging.x) / geo.w * (dragging.ve - dragging.vs); setView(dragging.vs - dt, dragging.ve - dt); }
      else if (dragging.moved) { S.selS = clamp(dragging.t, 0, S.duration); S.selE = t; }
    }
    draw();
  });
  const endDrag = e => {
    if (!dragging) return; const d = dragging; dragging = null; lanesBox.classList.remove('nav');
    if (d.nav) return;
    const t = clamp(tOf(e.offsetX), 0, S.duration);
    if (!d.moved) {
      // Click: markers lane jumps to the nearest marker, otherwise place the playhead.
      const mr = geo.rows.find(r => r.key === 'marks'), y = e.offsetY;
      let target = t;
      if (mr && y >= mr.y && y <= mr.y + mr.h) { const near = S.markers.map(m => m.t).sort((a, b) => Math.abs(xOf(a) - e.offsetX) - Math.abs(xOf(b) - e.offsetX))[0]; if (near !== undefined && Math.abs(xOf(near) - e.offsetX) < 40) target = near; }
      const sa = Math.min(S.selS, S.selE), sb = Math.max(S.selS, S.selE);
      if (target < sa || target > sb) { S.selS = 0; S.selE = S.duration; send('deckrange', '0:0'); }
      S.playhead = target; send('deckseek', target.toFixed(4));
    } else {
      const a = Math.min(d.t, t), b = Math.max(d.t, t); S.selS = a; S.selE = b; S.playhead = a;
      send('deckrange', a.toFixed(4) + ':' + b.toFixed(4)); send('deckseek', a.toFixed(4));
    }
    draw(); drawOverview();
  };
  canvas.addEventListener('pointerup', endDrag);
  canvas.addEventListener('pointercancel', () => { dragging = null; lanesBox.classList.remove('nav'); });
  canvas.addEventListener('pointerleave', () => { S.cursor = null; $('[data-cursor]').textContent = ''; draw(); });
  canvas.addEventListener('wheel', e => {
    if (S.duration <= 0) return; e.preventDefault();
    if (e.shiftKey || Math.abs(e.deltaX) > Math.abs(e.deltaY)) { const len = S.ve - S.vs, dv = (e.shiftKey ? e.deltaY : e.deltaX) / 600 * len; setView(S.vs + dv, S.ve + dv); }
    else zoom(Math.pow(1.0015, e.deltaY), tOf(e.offsetX));
  }, { passive: false });

  // ---------- interaction: overview ----------
  let ovDrag = false;
  const ovMove = e => { const len = S.ve - S.vs, t = e.offsetX / ovCanvas.clientWidth * S.duration; setView(t - len / 2, t + len / 2); };
  ovCanvas.addEventListener('pointerdown', e => { if (S.duration <= 0) return; ovDrag = true; ovCanvas.setPointerCapture(e.pointerId); ovMove(e); });
  ovCanvas.addEventListener('pointermove', e => { if (ovDrag) ovMove(e); });
  ovCanvas.addEventListener('pointerup', () => { ovDrag = false; });

  // ---------- controls ----------
  const play = () => {
    if (S.duration <= 0) return;
    if (S.playing) send('pause'); else send('deckplay', S.playhead.toFixed(4));
    if (!S.playing) { S.playing = true; S.paused = false; } else S.paused = !S.paused;
    syncControls();
  };
  const seek = t => { t = clamp(t, 0, S.duration); S.playhead = t; send('deckseek', t.toFixed(4)); if (t < S.vs || t > S.ve) setView(t - (S.ve - S.vs) * .1, t + (S.ve - S.vs) * .9); draw(); drawOverview(); };
  const addMarker = () => { if (S.duration <= 0) return; const box = $('[data-note]'); send('deckmarker', S.playhead.toFixed(4) + '|' + box.value); box.value = ''; };
  const actions = {
    import: () => send('import'), close: () => hide(), play, stop: () => { send('deckstop'); S.playing = false; syncControls(); },
    home: () => seek(Math.min(S.selS, S.selE) > 0.001 && Math.max(S.selS, S.selE) < S.duration - 0.01 ? Math.min(S.selS, S.selE) : 0),
    back: () => seek(S.playhead - 2), fwd: () => seek(S.playhead + 2),
    loop: () => { S.loop = !S.loop; send('deckloop', S.loop ? '1' : '0'); syncControls(); },
    zoomin: () => zoom(.5), zoomout: () => zoom(2), fit: () => setView(0, S.duration),
    zoomsel: () => { const a = Math.min(S.selS, S.selE), b = Math.max(S.selS, S.selE); if (b - a > 0.01) setView(a - (b - a) * .05, b + (b - a) * .05); },
    mark: addMarker, save: () => send('decksave', $('[data-session]').value),
    wav: () => send('deckexport', 'wav'), mp3: () => send('deckexport', 'mp3'),
  };
  deck.addEventListener('click', e => { const b = e.target.closest('[data-act]'); if (b && actions[b.dataset.act]) actions[b.dataset.act](); });
  $('[data-takes]').addEventListener('change', e => { if (e.target.value !== '') send('take', e.target.value); e.target.value = ''; });
  const setSpeed = v => { S.speed = clamp(Number(v), .1, 1); send('deckspeed', S.speed.toFixed(2)); syncControls(); };
  $$('[data-speed]').forEach(b => b.addEventListener('click', () => setSpeed(b.dataset.speed)));
  const slider = $('[data-speedslider]');
  slider.addEventListener('input', () => { $('[data-speedread]').textContent = Number(slider.value).toFixed(2) + '×'; });
  slider.addEventListener('change', () => setSpeed(slider.value));
  $('[data-pitch]').addEventListener('change', e => { S.keepPitch = e.target.checked; send('deckpitch', e.target.checked ? '1' : '0'); });
  $$('[data-tool]').forEach(b => b.addEventListener('click', () => { S.tool = b.dataset.tool; syncControls(); }));
  $$('[data-layer]').forEach(b => b.addEventListener('change', () => { S.layers[b.dataset.layer] = b.checked; S.baseDirty = true; draw(); }));
  $$('[data-clean]').forEach(b => b.addEventListener('change', () => send('deckclean', b.dataset.clean + ':' + (b.checked ? '1' : '0'))));
  $('[data-palette]').addEventListener('change', e => { S.palette = e.target.value; S.baseDirty = true; draw(); });
  $('[data-range]').addEventListener('change', e => { S.fmax = Number(e.target.value); S.baseDirty = true; draw(); });
  $('[data-bright]').addEventListener('input', e => { S.bright = Number(e.target.value); S.baseDirty = true; draw(); });
  $('[data-contrast]').addEventListener('input', e => { S.contrast = Number(e.target.value); S.baseDirty = true; draw(); });
  $('[data-auto]').addEventListener('change', e => { S.auto = e.target.checked; S.baseDirty = true; draw(); });
  $('[data-follow]').addEventListener('change', e => { S.follow = e.target.value; });
  $('[data-note]').addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); addMarker(); } e.stopPropagation(); });

  function renderMarkers() {
    const box = $('[data-marks]');
    if (!S.markers.length) { box.innerHTML = '<p class="note">No markers yet.</p>'; return; }
    box.replaceChildren(...S.markers.map((m, i) => {
      const row = document.createElement('div'); row.className = 'mk';
      const t = document.createElement('span'); t.className = 't'; t.textContent = (i + 1) + '  ' + fmt(m.t);
      const n = document.createElement('span'); n.className = 'n'; n.textContent = m.note || '—'; n.title = m.note;
      const go = document.createElement('button'); go.className = 'btn'; go.textContent = 'Go'; go.onclick = () => seek(m.t);
      const del = document.createElement('button'); del.className = 'btn'; del.textContent = '✕'; del.setAttribute('aria-label', 'Delete marker ' + (i + 1)); del.onclick = () => send('deckmarkerdel', String(i));
      row.append(t, n, go, del); return row;
    }));
  }
  renderMarkers();

  function syncControls() {
    const p = $('[data-act=play]'); p.textContent = S.playing && !S.paused ? '⏸' : '▶'; p.setAttribute('aria-label', S.playing && !S.paused ? 'Pause' : 'Play');
    $('[data-act=loop]').setAttribute('aria-pressed', String(S.loop));
    $$('[data-speed]').forEach(b => b.setAttribute('aria-pressed', String(Math.abs(Number(b.dataset.speed) - S.speed) < 0.001)));
    if (document.activeElement !== slider) { slider.value = S.speed; $('[data-speedread]').textContent = S.speed.toFixed(2) + '×'; }
    $('[data-pitch]').checked = S.keepPitch;
    $$('[data-tool]').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.tool === S.tool)));
    for (const b of $$('.bar [data-act]')) if (!['import', 'close'].includes(b.dataset.act)) b.disabled = S.duration <= 0;
  }

  // ---------- keyboard ----------
  document.addEventListener('keydown', e => {
    if (!S.open || e.ctrlKey || e.metaKey) return;
    const tag = (e.target.tagName || '').toLowerCase();
    if (tag === 'input' && e.target.type === 'text' || tag === 'select') return;
    const k = e.key, map = { ' ': play, Home: actions.home, ArrowLeft: actions.back, ArrowRight: actions.fwd, '+': actions.zoomin, '=': actions.zoomin, '-': actions.zoomout, f: actions.fit, F: actions.fit, l: actions.loop, L: actions.loop, m: addMarker, M: addMarker, Escape: hide };
    if (map[k]) { e.preventDefault(); map[k](); }
  });

  // ---------- open / close ----------
  function show() {
    S.open = true; deck.classList.add('open'); document.documentElement.style.overflow = 'hidden';
    syncControls(); S.baseDirty = true; requestAnimationFrame(() => { draw(); drawOverview(); requestView(); });
  }
  function hide() { S.open = false; deck.classList.remove('open'); document.documentElement.style.overflow = ''; }
  new ResizeObserver(() => { if (!S.open) return; S.baseDirty = true; draw(); drawOverview(); requestView(); }).observe(lanesBox);
  syncControls();
})();
