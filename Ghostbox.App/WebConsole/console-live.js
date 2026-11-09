// Whispers of the Spirit — live console wiring.
// Connects the console's panels to what the app is really doing: receiver state, live analyser,
// effect values, slow-down speed, skins and a session log of real events. Installed after the bridge.
(() => {
  if (window.__whispersLiveInstalled) return;
  window.__whispersLiveInstalled = true;
  const $ = id => document.getElementById(id);
  const send = (action, value) => {
    const url = 'whispers://' + action + (value === undefined ? '' : '?value=' + encodeURIComponent(value));
    if (window.__whispersSend) window.__whispersSend(url); else window.location.href = url;
  };
  const setText = (id, text) => { const el = $(id); if (el && el.textContent !== text) el.textContent = text; };
  const led = (id, color, glow) => { const el = $(id); if (!el) return; el.style.backgroundColor = color; el.style.boxShadow = glow ? '0 0 10px ' + color : 'none'; };
  const db = v => (v <= -95 ? '—' : v.toFixed(1));

  // ---------- skins (colour themes for the console chassis) ----------
  const chassis = document.querySelector('[data-purpose="workstation-hardware-enclosure"]');
  const skins = { void: '', military: 'hue-rotate(-70deg) saturate(1.15)', nebula: 'hue-rotate(65deg) saturate(1.2)', obsidian: 'grayscale(0.85) contrast(1.05)' };
  const applySkin = name => {
    if (!skins.hasOwnProperty(name)) name = 'void';
    if (chassis) chassis.style.filter = skins[name];
    document.querySelectorAll('[data-skin]').forEach(b => {
      const on = b.dataset.skin === name;
      b.setAttribute('aria-pressed', String(on));
      b.style.borderColor = on ? '#38bdf8' : ''; b.style.color = on ? '#a5f3fc' : ''; b.style.backgroundColor = on ? '#083344' : '';
      b.textContent = (on ? '● ' : '') + b.textContent.replace(/^● /, '');
    });
    try { localStorage.setItem('whispers-skin', name); } catch (e) { }
  };
  document.querySelectorAll('[data-skin]').forEach(b => b.addEventListener('click', () => applySkin(b.dataset.skin)));
  let savedSkin = 'void'; try { savedSkin = localStorage.getItem('whispers-skin') || 'void'; } catch (e) { }
  applySkin(savedSkin);

  // ---------- panel 04: live effects ----------
  const fx = {
    echo: { scale: 1, fmt: v => v > 0 ? Math.round(v * 100) + '% mix' : 'OFF' },
    delay: { scale: 1, fmt: v => Math.round(v) + ' ms' },
    reverb: { scale: 100, fmt: v => v > 0 ? Math.round(v) + '% wet' : 'OFF' },
    feedback: { scale: 1, fmt: v => v > 0 ? Math.round(v * 100) + '%' : 'OFF' },
    chop: { scale: 1, fmt: v => v > 0 ? Math.round(v) + ' Hz' : 'OFF' },
  };
  const touched = {};
  for (const [key, def] of Object.entries(fx)) {
    const el = $('fx-' + key); if (!el) continue;
    el.addEventListener('input', () => { touched[key] = Date.now(); setText('fx-' + key + '-read', def.fmt(Number(el.value))); });
    el.addEventListener('change', () => { touched[key] = Date.now(); send('effect', key + ':' + (Number(el.value) / def.scale)); });
  }
  const syncFx = f => {
    let active = 0;
    for (const [key, def] of Object.entries(fx)) {
      const el = $('fx-' + key); if (!el) continue;
      const v = (f[key] ?? 0) * def.scale;
      if (key !== 'delay' && v > 0) active++;
      if (document.activeElement === el || Date.now() - (touched[key] || 0) < 2500) continue;
      el.value = v; setText('fx-' + key + '-read', def.fmt(v));
    }
    setText('fx-count', active ? active + ' ACTIVE' : 'ALL OFF');
    const faders = window.__whispersFaders || {};
    for (const [action, value] of [['denoiselevel', f.denoise], ['volume', f.volume]]) {
      const fader = faders[action]; if (!fader || value === undefined || document.activeElement === fader.slider) continue;
      const v = String(Math.round(value * 100)); if (fader.slider.value !== v) { fader.slider.value = v; fader.draw(); }
    }
    const monitor = document.querySelector('input[aria-label="Monitor volume"]');
    if (monitor && document.activeElement !== monitor) monitor.value = Math.round((f.volume ?? .6) * 100);
  };

  // ---------- panel 06: slow-down speed (shared with the Review deck) ----------
  const speedSlider = document.querySelector('input[aria-label="Review playback speed"]');
  let speedTouched = 0;
  const drawSpeed = (speed, keepPitch) => {
    const s = Math.min(1, Math.max(0.1, speed));
    setText('slow-speed', s.toFixed(2) + '×'); setText('slow-factor', s.toFixed(2) + '×'); setText('slow-read', s.toFixed(2) + '×');
    setText('slow-percent', s >= 0.999 ? 'NORMAL' : Math.round((1 - s) * 100) + '% SLOWER');
    setText('slow-stretch', (1 / s).toFixed(1));
    setText('slow-bar-read', (1 / s).toFixed(1) + ' s');
    const bar = $('slow-bar'); if (bar) bar.style.width = Math.min(100, 10 / s) + '%';
    const dial = $('slow-dial'); if (dial) dial.style.transform = 'rotate(' + (-135 + (s - 0.1) / 0.9 * 270) + 'deg)';
    setText('slow-pitch', keepPitch ? 'PITCH KEPT' : 'TAPE-STYLE');
    setText('slow-pitch-note', keepPitch ? 'VOICE PITCH KEPT' : 'PITCH DROPS (TAPE-STYLE)');
    document.querySelectorAll('[data-slow]').forEach(b => {
      const on = Math.abs(Number(b.dataset.slow) - s) < 0.001;
      b.style.borderColor = on ? '#f59e0b' : ''; b.style.color = on ? '#f59e0b' : ''; b.style.backgroundColor = on ? '#451a03' : '';
      b.setAttribute('aria-pressed', String(on));
    });
  };
  if (speedSlider) speedSlider.addEventListener('input', () => { speedTouched = Date.now(); drawSpeed(Number(speedSlider.value), true); });
  document.querySelectorAll('[data-slow]').forEach(b => b.addEventListener('click', () => {
    speedTouched = Date.now(); if (speedSlider) speedSlider.value = b.dataset.slow; drawSpeed(Number(b.dataset.slow), true); send('deckspeed', b.dataset.slow);
  }));
  document.querySelectorAll('[data-open-deck]').forEach(b => b.addEventListener('click', () => window.whispersDeckShow?.()));
  drawSpeed(1, true);

  // ---------- live analyser (spectrum bars, level, waveform, X/Y scope, level history) ----------
  const bars = [...document.querySelectorAll('[data-band]')];
  const barHz = [50, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 10000, 12000, 16000];
  const history = [];
  let lastLive = 0;
  const canvasCtx = id => {
    const c = $(id); if (!c) return null;
    const dpr = window.devicePixelRatio || 1, w = c.clientWidth, h = c.clientHeight;
    if (!w || !h) return null;
    if (c.width !== Math.round(w * dpr) || c.height !== Math.round(h * dpr)) { c.width = Math.round(w * dpr); c.height = Math.round(h * dpr); }
    const g = c.getContext('2d'); g.setTransform(dpr, 0, 0, dpr, 0, 0); g.clearRect(0, 0, w, h);
    return { g, w, h };
  };
  const drawWave = wave => {
    const k = canvasCtx('osc-canvas'); if (!k) return; const { g, w, h } = k;
    g.strokeStyle = 'rgba(56,189,248,.18)'; g.beginPath(); g.moveTo(0, h / 2); g.lineTo(w, h / 2); g.stroke();
    if (!wave || wave.length < 2) return;
    g.strokeStyle = '#38bdf8'; g.lineWidth = 1.4; g.shadowColor = 'rgba(56,189,248,.8)'; g.shadowBlur = 6; g.beginPath();
    wave.forEach((v, i) => { const x = i / (wave.length - 1) * w, y = h / 2 - Math.max(-1, Math.min(1, v)) * (h / 2 - 3); i ? g.lineTo(x, y) : g.moveTo(x, y); });
    g.stroke(); g.shadowBlur = 0;
  };
  const drawScope = wave => {
    const k = canvasCtx('scope-canvas'); if (!k) return; const { g, w, h } = k;
    g.strokeStyle = 'rgba(16,185,129,.25)'; g.setLineDash([2, 4]); g.beginPath(); g.moveTo(w / 2, 4); g.lineTo(w / 2, h - 4); g.moveTo(4, h / 2); g.lineTo(w - 4, h / 2); g.stroke(); g.setLineDash([]);
    if (!wave || wave.length < 10) return;
    const lag = 6, r = Math.min(w, h) / 2 - 4; let peak = 0.05;
    for (const v of wave) peak = Math.max(peak, Math.abs(v));
    g.strokeStyle = '#00ff88'; g.lineWidth = 1.2; g.shadowColor = 'rgba(0,255,136,.8)'; g.shadowBlur = 5; g.beginPath();
    for (let i = 0; i + lag < wave.length; i++) { const x = w / 2 + wave[i] / peak * r, y = h / 2 - wave[i + lag] / peak * r; i ? g.lineTo(x, y) : g.moveTo(x, y); }
    g.stroke(); g.shadowBlur = 0;
  };
  const drawHistory = () => {
    const k = canvasCtx('level-canvas'); if (!k) return; const { g, w, h } = k;
    g.strokeStyle = 'rgba(245,158,11,.2)'; g.font = '8px Consolas, monospace'; g.fillStyle = 'rgba(245,158,11,.6)';
    for (const d of [-20, -40]) { const y = h - (d + 60) / 60 * h; g.beginPath(); g.moveTo(0, y); g.lineTo(w, y); g.stroke(); g.fillText(d + ' dB', 2, y - 2); }
    if (history.length < 2) return;
    const now = Date.now();
    g.strokeStyle = '#f59e0b'; g.lineWidth = 1.6; g.shadowColor = 'rgba(245,158,11,.7)'; g.shadowBlur = 4; g.beginPath();
    let started = false;
    for (const [t, v] of history) {
      const x = w - (now - t) / 60000 * w, y = h - Math.max(0, Math.min(1, (v + 60) / 60)) * h;
      if (x < 0) continue; started ? g.lineTo(x, y) : g.moveTo(x, y); started = true;
    }
    g.stroke(); g.shadowBlur = 0;
  };
  const resetAnalyser = () => {
    bars.forEach(b => b.style.height = '5%');
    const vu = $('vu-bar'); if (vu) vu.style.width = '0%';
    drawWave(null); drawScope(null); drawHistory();
  };
  window.whispersLive = d => {
    lastLive = Date.now();
    bars.forEach((bar, i) => {
      const center = Math.floor(Math.log(barHz[i] / 50) / Math.log(320) * 32);
      let v = 0; for (let b = Math.max(0, center - 1); b <= Math.min(31, center + 1); b++) v = Math.max(v, d.bands[b] || 0);
      bar.style.height = Math.max(3, Math.round(v * 100)) + '%';
    });
    const vu = $('vu-bar'); if (vu) vu.style.width = Math.max(0, Math.min(100, (d.rms + 60) / 60 * 100)) + '%';
    setText('vu-peak', db(d.peak) + ' dB');
    setText('osc-rms', 'RMS: ' + db(d.rms) + ' dBFS');
    setText('an-strongest', 'Strongest: ' + Math.round(d.hz) + ' Hz');
    setText('an-clip', 'Clipped: ' + d.clip.toFixed(1) + '%');
    setText('crt-strongest', 'STRONGEST: ' + Math.round(d.hz) + ' Hz');
    setText('crt-level', 'LEVEL: ' + db(d.rms) + ' dBFS');
    setText('chip-level', 'LEVEL ' + db(d.rms) + ' dB');
    setText('hist-now', db(d.rms) + ' dBFS');
    setText('hist-peak', 'PEAK: ' + db(d.peak) + ' dBFS');
    setText('hist-clip', 'CLIPPED: ' + d.clip.toFixed(1) + '%');
    setText('scope-state', d.rms > -55 ? 'SIGNAL' : 'VERY QUIET');
    history.push([Date.now(), d.rms]); while (history.length && Date.now() - history[0][0] > 61000) history.shift();
    drawWave(d.wave); drawScope(d.wave); drawHistory();
  };
  setInterval(() => { if (lastLive && Date.now() - lastLive > 2500) { lastLive = 0; resetAnalyser(); setText('chip-level', 'NO AUDIO'); setText('scope-state', 'NO SIGNAL'); } }, 1000);
  window.addEventListener('resize', () => { drawHistory(); });
  resetAnalyser();

  // ---------- session log of real events ----------
  const log = $('session-log');
  let firstLog = true;
  const addLog = (text, color) => {
    if (!log || !text) return;
    if (firstLog) { log.textContent = ''; firstLog = false; }
    const row = document.createElement('div');
    const time = document.createElement('span'); time.className = 'text-slate-600'; time.textContent = '[' + new Date().toLocaleTimeString([], { hour12: false }) + '] ';
    const msg = document.createElement('span'); msg.textContent = text; if (color) msg.style.color = color;
    row.append(time, msg); log.append(row);
    while (log.children.length > 200) log.firstChild.remove();
    log.scrollTop = log.scrollHeight;
  };
  const prev = {};
  const changed = (key, value) => { const was = prev[key]; prev[key] = value; return was !== undefined && was !== value; };

  // ---------- state from the app (once a second) ----------
  const previous = window.whispersNativeUpdate;
  window.whispersNativeUpdate = state => {
    try { previous?.(state); } catch (e) { }
    const active = state.receiver !== 'RECEIVER IDLE', demo = state.receiver.startsWith('DEMO');
    const recording = state.record === 'RECORDING' || state.record === 'MIC RECORDING';
    // header
    setText('chip-rx', active ? (demo ? 'TEST TONE' : 'RECEIVER ACTIVE') : 'RECEIVER IDLE');
    led('led-rx', active ? '#00ff88' : '#475569', active);
    setText('chip-rec', recording ? state.record : 'NOT RECORDING');
    led('led-rec', recording ? '#ef4444' : '#475569', recording);
    const top = $('pwr-top'), bottom = $('pwr-bottom');
    if (top && bottom) { top.style.backgroundColor = active ? 'rgba(16,185,129,.85)' : '#0f1724'; top.style.boxShadow = active ? '0 0 12px rgba(0,255,136,.55)' : 'none'; bottom.style.backgroundColor = active ? '#0f1724' : '#223552'; bottom.style.boxShadow = 'none'; }
    // receiver panel
    const nodes = (state.nodes || []).length;
    setText('badge-node', nodes ? nodes + ' RECEIVERS FOUND' : 'NO RECEIVER LIST YET');
    setText('scan-label', nodes ? 'SCAN PUBLIC SDR RECEIVERS (' + nodes + ' FOUND)' : 'SCAN PUBLIC SDR RECEIVERS');
    let on = 0; if (state.noise) on++; if (state.filter) on++;
    setText('filters-count', on ? on + ' ON' : 'ALL OFF');
    // main display
    const r = state.radio || {};
    setText('crt-state', active ? (demo ? 'TEST TONE' : 'RECEIVING') : 'IDLE');
    led('crt-led', active ? '#ef4444' : '#475569', active);
    setText('crt-mode', (r.mode || '—') + (r.source === 1 ? ' • STATION STREAMS' : ' • DWELL ' + (r.dwell || '—') + ' ms'));
    setText('crt-chip-mode', 'MODE: ' + (r.mode || '—'));
    setText('crt-chip-step', r.source === 1 ? 'STEP: —' : 'STEP: ' + String(r.step || '—').replace(/k$/i, ' kHz').replace(/M$/i, ' MHz'));
    setText('crt-chip-hops', 'HOPS: ' + (state.hops || 0));
    setText('crt-sub', active ? (state.audio || 'RECEIVING') : 'NOT SWEEPING');
    setText('an-state', active && lastLive ? 'FFT 48 kHz • LIVE' : 'FFT 48 kHz • WAITING');
    led('an-led', active && lastLive ? '#00ff88' : '#475569', active && lastLive);
    setText('an-source', active ? (demo ? 'TEST TONE — NOT A RADIO' : 'LIVE RECEIVER AUDIO') : 'NO AUDIO');
    // master deck & footer
    setText('tally', recording ? 'TALLY: RECORDING' : active ? 'TALLY: LIVE' : 'TALLY: IDLE');
    const rec = $('native-record'); if (rec) rec.classList.toggle('animate-pulse', recording);
    setText('footer-status', !active ? 'SYSTEM READY' : demo ? 'TEST TONE • NOT A RADIO' : /MHz$/.test(state.frequency || '') && state.frequency !== '— MHz' ? 'TUNED: ' + state.frequency : 'RECEIVING');
    // effects and speed
    if (state.fx) syncFx(state.fx);
    if (state.deck && Date.now() - speedTouched > 2500) { if (speedSlider && document.activeElement !== speedSlider) speedSlider.value = state.deck.speed; drawSpeed(state.deck.speed, state.deck.keepPitch); }
    // session log
    if (changed('receiver', state.receiver)) addLog(active ? (demo ? 'Test tone started (synthetic, no radio).' : 'Receiver started.') : 'Receiver stopped.', active ? '#67e8f9' : '#94a3b8');
    if (changed('audio', state.audio) && state.audio) addLog(state.audio);
    if (changed('record', state.record)) addLog(recording ? (state.record === 'MIC RECORDING' ? 'Microphone recording started.' : 'Recording received audio.') : 'Recording stopped.', recording ? '#fca5a5' : '#94a3b8');
    if (changed('detail', state.recordingDetail) && state.recordingDetail) addLog(state.recordingDetail, '#fbbf24');
    if (changed('nodes', nodes) && nodes) addLog(nodes + ' public SDR receivers found.', '#67e8f9');
    if (changed('saved', state.saved) && state.saved > (prev.savedMax || 0)) addLog('Saved takes: ' + state.saved + '.', '#00ff88');
    prev.savedMax = Math.max(prev.savedMax || 0, state.saved || 0);
    if (state.deck && changed('deckStatus', state.deck.status) && state.deck.status) addLog('Review deck: ' + state.deck.status, '#a5f3fc');
  };
})();
