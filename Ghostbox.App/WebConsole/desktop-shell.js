// Whispers of the Spirit — desktop layer (Windows and Linux builds).
// Brings the phone's extras to the computer while the classic console layout stays exactly as it is:
//  - "Material" and "Ember" skins (same look as on the phone);
//  - two buttons in the top bar: SLS CAMERA (radar) and ⚙ Settings;
//  - Settings drawer: language with flags, theme, text size, how to use, about;
//  - every text translated with the phone's table (phone-i18n.js);
//  - the SLS camera in a window over the console (filled in by phone-sls.js, using the webcam);
//  - the old television switch-on when the app opens.
// Loaded before phone-sls.js, which finds the #sls-page element made here.
(() => {
  if (window.__whispersDesktopInstalled) return;
  window.__whispersDesktopInstalled = true;
  const root = document.documentElement;
  root.classList.add('whispers-desktop');
  const q = s => document.querySelector(s);
  const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
  const load = (k, d) => { try { const v = localStorage.getItem(k); return v === null ? d : v; } catch (e) { return d; } };
  const save = (k, v) => { try { localStorage.setItem(k, String(v)); } catch (e) { } };

  // ---------- SLS saving on the computer ----------
  // Same methods as the phone's window.WhispersSls, answered by the app at this page's own address
  // (SlsFiles.Serve). Small, quick requests; the page waits for each answer like on the phone.
  const api = (action, args, body) => {
    try {
      const x = new XMLHttpRequest();
      x.open(body === undefined ? 'GET' : 'POST', location.origin + '/sls-api/' + action + (args ? '?' + new URLSearchParams(args) : ''), false);
      x.send(body === undefined ? null : body);
      return x.status === 200 ? x.responseText : '';
    } catch (e) { return ''; }
  };
  if (location.protocol === 'https:' && !window.WhispersSls) window.WhispersSls = {
    begin: () => api('begin', null, ''),
    append: (name, ext, b64) => api('append', { name, ext }, b64) === '1',
    finish: (name, json) => api('finish', { name }, json) === '1',
    list: () => api('list') || '[]',
    rename: (name, place) => api('rename', { name }, place ?? '') === '1',
    remove: name => api('remove', { name }, '') === '1',
    share: name => { api('share', { name }, ''); },
  };

  // ---------- Material and Ember skins (same as the phone) ----------
  const S = 'html.whispers-material [data-purpose="workstation-hardware-enclosure"]';
  const material = `
  ${S} .shadow-tactile-out{background-image:linear-gradient(180deg,rgba(255,255,255,.04),rgba(0,0,0,.28));
    box-shadow:0 8px 18px rgba(0,0,0,.75),inset 0 1px 0 rgba(255,255,255,.07),inset 0 -1px 0 rgba(0,0,0,.6)!important;border-color:rgba(255,255,255,.07)!important}
  ${S} button{border-radius:10px!important;background-image:linear-gradient(180deg,rgba(255,255,255,.09),rgba(255,255,255,0) 45%,rgba(0,0,0,.35));
    box-shadow:0 3px 7px rgba(0,0,0,.8),inset 0 1px 0 rgba(255,255,255,.14),inset 0 -2px 0 rgba(0,0,0,.45);transition:transform .08s,box-shadow .08s}
  ${S} button:active{transform:translateY(1px);box-shadow:0 1px 3px rgba(0,0,0,.8),inset 0 2px 4px rgba(0,0,0,.7)}
  ${S} button[aria-pressed="true"]{box-shadow:0 0 10px rgba(56,189,248,.45),inset 0 1px 0 rgba(255,255,255,.18),inset 0 0 8px rgba(56,189,248,.35)}
  ${S} .toggle-rocker{width:48px;height:24px;border-radius:12px;background:#04070b;border:1px solid #1b2636;box-shadow:inset 0 2px 5px #000,0 1px 0 rgba(255,255,255,.06)}
  ${S} .toggle-rocker-thumb{width:18px;height:18px;top:2px!important;left:26px;border-radius:50%;border:0;
    background:radial-gradient(circle at 38% 32%,#e0faff,#38bdf8 55%,#0e7490);box-shadow:0 0 10px #38bdf8,0 0 3px #38bdf8}
  ${S} .toggle-rocker.is-off .toggle-rocker-thumb{left:2px;background:radial-gradient(circle at 38% 32%,#5b6575,#252c38 70%);box-shadow:0 2px 3px #000,inset 0 1px 0 rgba(255,255,255,.2)}
  ${S} label > input.peer + div{width:42px!important;height:22px!important;background:#04070b!important;border:1px solid #1b2636;box-shadow:inset 0 2px 4px #000}
  ${S} label > input.peer + div::after{top:3px!important;left:3px!important;width:14px!important;height:14px!important;
    background:radial-gradient(circle at 38% 32%,#5b6575,#252c38 70%)!important;box-shadow:0 2px 3px #000}
  ${S} label > input.peer:checked + div::after{transform:translateX(20px)!important;background:radial-gradient(circle at 38% 32%,#eafff4,#00ff88 55%,#047857)!important;box-shadow:0 0 10px #00ff88}
  ${S} .fader-track{width:5px;background:#04070b;border-color:#123447;box-shadow:inset 0 0 4px #000,0 0 6px rgba(56,189,248,.35)}
  ${S} .fader-thumb{border-radius:5px;border:1px solid #6b7586;
    background:repeating-linear-gradient(180deg,rgba(0,0,0,.45) 0 1px,transparent 1px 4px) center/60% 40% no-repeat,linear-gradient(180deg,#6b7586,#323a47 55%,#1c222c);
    box-shadow:0 5px 10px rgba(0,0,0,.85),inset 0 1px 0 rgba(255,255,255,.35),0 0 8px rgba(56,189,248,.25)}
  ${S} input[type=range]{-webkit-appearance:none;appearance:none;height:5px;border-radius:3px;background:#04070b;border:1px solid #1b2636;box-shadow:inset 0 1px 2px #000}
  ${S} input[type=range]::-webkit-slider-thumb{-webkit-appearance:none;width:18px;height:18px;border-radius:50%;border:0;
    background:radial-gradient(circle at 38% 32%,#e0faff,#38bdf8 55%,#0e7490);box-shadow:0 0 9px #38bdf8,0 2px 3px #000}
  ${S} input[type=range][class*="accent-emerald"]::-webkit-slider-thumb{background:radial-gradient(circle at 38% 32%,#eafff4,#00ff88 55%,#047857);box-shadow:0 0 9px #00ff88,0 2px 3px #000}
  ${S} input[type=range][class*="accent-amber"]::-webkit-slider-thumb{background:radial-gradient(circle at 38% 32%,#fff3d6,#f59e0b 55%,#92400e);box-shadow:0 0 9px #f59e0b,0 2px 3px #000}
  ${S} .shadow-knob-3d{box-shadow:0 0 0 3px #04070b,0 0 0 5px rgba(245,158,11,.85),0 0 16px rgba(245,158,11,.55),0 5px 10px #000!important}
  ${S} select,${S} input[type=text],${S} input:not([type]){border-radius:8px;box-shadow:inset 0 2px 4px rgba(0,0,0,.8)}
  `;
  const E = 'html.whispers-ember [data-purpose="workstation-hardware-enclosure"]';
  const O = '#ff8a3d', OG = 'rgba(255,122,42,';
  const ember = `
  html.whispers-ember body{background:#000!important;background-image:none!important}
  ${E}{background-color:#141518!important;border-color:#2a2b30!important}
  ${E} [class*="bg-void"],${E} [class*="bg-chassis"],${E} [class*="bg-[#0"],${E} [class*="bg-slate-9"],${E} main{background-color:#121316!important;background-image:none}
  ${E} .shadow-tactile-out{background-color:#1c1d21!important}
  ${E} [class*="border-void"],${E} [class*="border-cyan"],${E} [class*="border-emerald"],${E} [class*="border-sky"],${E} [class*="border-[#"],${E} [class*="border-slate"]{border-color:#2c2d33!important}
  ${E} [class*="text-cyan"],${E} [class*="text-emerald"],${E} [class*="text-phosphor"],${E} [class*="text-sky"],${E} [class*="text-green"],${E} [class*="text-teal"],${E} [class*="text-blue"],${E} [class*="text-amber"],${E} [class*="text-yellow"]{color:${O}!important}
  ${E} [class*="bg-emerald"],${E} [class*="bg-cyan"],${E} [class*="bg-phosphor"],${E} [class*="bg-green"]{background-color:${O}!important}
  ${E} [class*="bg-emerald-8"],${E} [class*="bg-emerald-9"],${E} [class*="bg-cyan-8"],${E} [class*="bg-cyan-9"],${E} [class*="bg-green-9"],${E} [class*="/"][class*="bg-cyan"],${E} [class*="/"][class*="bg-emerald"]{background-color:#26160d!important}
  ${E} .crt-screen-emerald,${E} .crt-screen-amber{background:#0b0b0c!important;box-shadow:inset 0 0 30px #000,inset 0 0 12px ${OG}.25)!important}
  ${E} .bg-scope-grid,${E} .bg-amber-grid{background-image:linear-gradient(${OG}.12) 1px,transparent 1px),linear-gradient(90deg,${OG}.12) 1px,transparent 1px)!important;background-size:40px 40px!important}
  ${E} svg,${E} canvas{filter:grayscale(1) sepia(1) saturate(5) hue-rotate(-12deg) brightness(1.05)}
  ${E} button{background-color:#202125!important;border-color:#2e2f35!important;color:#c4c4c8}
  ${E} button[aria-pressed="true"]{background-color:#2a1810!important;border-color:${O}!important;color:#ffb27a!important;box-shadow:0 0 10px ${OG}.45),inset 0 0 8px ${OG}.35),inset 0 1px 0 rgba(255,255,255,.15)}
  ${E} button[class*="red"],${E} button[class*="rose"]{background-color:#2a0d10!important;border-color:#7f1d1d!important;color:#fca5a5!important}
  ${E} .toggle-rocker,${E} label > input.peer + div,${E} input[type=range]{background:#0b0b0c!important;border-color:#2c2d33!important}
  ${E} .toggle-rocker-thumb,${E} label > input.peer:checked + div::after,${E} input[type=range]::-webkit-slider-thumb{background:radial-gradient(circle at 38% 32%,#ffe0c8,${O} 55%,#9a3412)!important;box-shadow:0 0 10px #ff7a2a,0 2px 3px #000!important}
  ${E} .toggle-rocker.is-off .toggle-rocker-thumb{background:radial-gradient(circle at 38% 32%,#5a5b60,#2a2b2f 70%)!important;box-shadow:0 2px 3px #000!important}
  ${E} .fader-track{border-color:#4a2a14!important;box-shadow:inset 0 0 4px #000,0 0 7px ${OG}.45)!important}
  ${E} .fader-thumb{box-shadow:0 5px 10px rgba(0,0,0,.85),inset 0 1px 0 rgba(255,255,255,.35),0 0 8px ${OG}.3)!important}
  ${E} .shadow-knob-3d{box-shadow:0 0 0 3px #0b0b0c,0 0 0 5px ${OG}.9),0 0 16px ${OG}.6),0 5px 10px #000!important}
  `;

  // ---------- top-bar buttons, Settings drawer and SLS window ----------
  const shell = `
  [data-purpose="workstation-hardware-enclosure"]{zoom:var(--dz,1)}
  #desk-tools{display:flex;align-items:center;gap:10px}
  #desk-tools button{font:12px/1 "Share Tech Mono","JetBrains Mono",monospace;letter-spacing:.08em;cursor:pointer}
  #desk-sls{--g:0,255,136;display:flex;align-items:center;gap:8px;padding:4px 13px 4px 5px;border-radius:20px;border:1px solid #15803d;background:#03140a;color:#4ade80;
    animation:desk-pulse 2.4s ease-in-out infinite}
  #desk-sls:hover{background:#052e16}
  #desk-sls[hidden]{display:none}
  #desk-gear{width:32px;height:32px;border-radius:50%;border:1px solid #1e3a5f;background:#0b111b;color:#7dd3fc;font-size:17px!important;display:flex;align-items:center;justify-content:center}
  #desk-gear:hover{border-color:#38bdf8;color:#e0f2fe}
  .desk-radar{position:relative;width:24px;height:24px;border-radius:50%;border:1px solid currentColor;overflow:hidden;background:radial-gradient(circle,rgba(var(--g),.2),#000 72%)}
  .desk-radar i{position:absolute;inset:0;border-radius:50%;background:conic-gradient(from 0deg,rgba(var(--g),.8),rgba(var(--g),.15) 50deg,transparent 80deg);animation:desk-sweep 2.4s linear infinite}
  .desk-radar b{position:absolute;left:50%;top:50%;width:4px;height:4px;margin:-2px;border-radius:50%;background:currentColor}
  @keyframes desk-sweep{to{transform:rotate(360deg)}}
  @keyframes desk-pulse{0%,100%{box-shadow:0 0 6px rgba(var(--g),.25)}50%{box-shadow:0 0 16px rgba(var(--g),.65)}}
  html.whispers-ember #desk-sls{--g:255,122,42;border-color:#9a3412;background:#1a0f08;color:#ff8a3d}
  html.whispers-ember #desk-gear{border-color:#3a2a20;background:#17181b;color:#ff8a3d}

  #desk-shade{position:fixed;inset:0;z-index:100000;background:rgba(0,0,0,.55);opacity:0;pointer-events:none;transition:opacity .25s}
  #desk-shade.on{opacity:1;pointer-events:auto}
  #desk-settings{position:fixed;top:0;right:0;bottom:0;width:min(430px,92vw);z-index:100002;background:#070b12;color:#cbd5e1;overflow-y:auto;
    border-left:1px solid #1e3a5f;box-shadow:-20px 0 40px rgba(0,0,0,.7);transform:translateX(105%);transition:transform .28s cubic-bezier(.2,.8,.2,1);
    font:14px/1.5 "Share Tech Mono","JetBrains Mono",monospace;padding-bottom:20px}
  #desk-settings.open{transform:none}
  #desk-settings .top{position:sticky;top:0;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;background:#0b111b;border-bottom:1px solid #1e2a3c;z-index:1}
  #desk-settings h2{margin:0;font-size:1.25em;letter-spacing:.1em;color:#e0f2fe}
  #desk-settings h3{margin:0 0 10px;font-size:.95em;letter-spacing:.12em;color:#7dd3fc;text-transform:uppercase}
  #desk-settings section{margin:12px 14px;padding:14px;border:1px solid #1e2a3c;border-radius:12px;background:#0a0f18;box-shadow:0 6px 14px rgba(0,0,0,.5),inset 0 1px 0 rgba(255,255,255,.04)}
  #desk-settings button{font:inherit;color:inherit;cursor:pointer}
  #desk-settings .x{border:1px solid #334155;background:#0f1724;border-radius:8px;padding:6px 12px}
  #desk-settings .grid{display:grid;grid-template-columns:1fr 1fr;gap:8px}
  #desk-settings .opt{display:flex;align-items:center;gap:10px;padding:9px 11px;border:1px solid #1e2a3c;border-radius:10px;background:#0f1724;text-align:left}
  #desk-settings .opt:hover{border-color:#334155}
  #desk-settings .opt .flag{display:inline-flex;line-height:0}
  #desk-settings .opt[aria-pressed="true"]{border-color:#00ff88;background:#06261c;color:#bbf7d0;box-shadow:0 0 10px rgba(0,255,136,.3)}
  #desk-settings .sw{width:14px;height:14px;border-radius:50%;flex:none;border:1px solid rgba(255,255,255,.25)}
  #desk-settings details{border-top:1px solid #1e2a3c;padding:9px 0}
  #desk-settings details:first-of-type{border-top:0}
  #desk-settings summary{cursor:pointer;color:#e0f2fe;list-style:none;display:flex;gap:10px;align-items:center}
  #desk-settings summary b{display:inline-flex;align-items:center;justify-content:center;min-width:1.7em;height:1.7em;border-radius:50%;background:#0a3445;color:#a5f3fc}
  #desk-settings details p{margin:7px 0 0 2.6em;color:#cbd5e1}
  #desk-settings .note{margin-top:10px;color:#94a3b8;font-size:.9em}
  #desk-settings .about dt{color:#7dd3fc;font-size:.8em;letter-spacing:.1em;text-transform:uppercase;margin-top:9px}
  #desk-settings .about dd{margin:2px 0 0;color:#e2e8f0;word-break:break-word}
  #desk-settings .brand{font-size:1.15em;letter-spacing:.14em;color:#e0f2fe}
  html.whispers-ember #desk-settings{background:#0c0d0f;border-color:#3a2a20}
  html.whispers-ember #desk-settings .top{background:#121316;border-color:#2c2d33}
  html.whispers-ember #desk-settings section{background:#17181b;border-color:#2c2d33}
  html.whispers-ember #desk-settings h3,html.whispers-ember #desk-settings .about dt{color:#ff8a3d}
  html.whispers-ember #desk-settings .opt{background:#1c1d21;border-color:#2c2d33}
  html.whispers-ember #desk-settings .opt[aria-pressed="true"]{border-color:#ff8a3d;background:#2a1810;color:#ffd7b8;box-shadow:0 0 10px rgba(255,122,42,.3)}
  html.whispers-ember #desk-settings summary b{background:#2a1810;color:#ffb27a}

  #sls-win{position:fixed;inset:4vh 4vw;z-index:100001;display:flex;flex-direction:column;border:1px solid #15803d;border-radius:14px;overflow:hidden;background:#020805;
    box-shadow:0 24px 60px rgba(0,0,0,.85),0 0 30px rgba(0,255,136,.12);opacity:0;transform:scale(.97);pointer-events:none;transition:opacity .22s,transform .22s}
  #sls-win.open{opacity:1;transform:none;pointer-events:auto}
  #sls-win .wbar{--g:0,255,136;display:flex;align-items:center;gap:10px;padding:8px 12px;background:#03140a;border-bottom:1px solid #0b2a17;color:#4ade80;
    font:13px "Share Tech Mono","JetBrains Mono",monospace;letter-spacing:.14em}
  #sls-win .wbar .sp{flex:1}
  #sls-win .wbar button{font:inherit;letter-spacing:.06em;color:#a7f3d0;background:#020805;border:1px solid #14532d;border-radius:8px;padding:5px 12px;cursor:pointer}
  #sls-win .wbar button:hover{border-color:#4ade80}
  html.whispers-ember #sls-win{border-color:#9a3412;box-shadow:0 24px 60px rgba(0,0,0,.85),0 0 30px rgba(255,122,42,.12)}
  html.whispers-ember #sls-win .wbar{--g:255,122,42;background:#1a0f08;border-color:#3a2a20;color:#ff8a3d}
  html.whispers-ember #sls-win .wbar button{color:#ffd7b8;background:#0c0d0f;border-color:#7c2d12}
  html.whispers-desktop #sls-win #sls-page{flex:1;min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 300px;grid-template-rows:auto auto auto 1fr;
    border:0;height:auto}
  html.whispers-desktop #sls-page .hd{grid-column:1/-1}
  html.whispers-desktop #sls-page .view{grid-column:1;grid-row:2/5;min-height:0}
  html.whispers-desktop #sls-page .cap{grid-column:2;grid-row:2;border-left:1px solid #0b2a17;padding:14px 12px 4px}
  html.whispers-desktop #sls-page .meters{grid-column:2;grid-row:3;border-left:1px solid #0b2a17;padding:10px 14px}
  html.whispers-desktop #sls-page .honest{grid-column:2;grid-row:4;border-left:1px solid #0b2a17;padding:4px 14px 14px;align-self:stretch}
  html.whispers-desktop #sls-page [data-filter],html.whispers-desktop #sls-page [data-flip]{display:none}
  html.whispers-desktop #sls-page .fmenu{display:flex!important;flex-wrap:wrap;left:8px;right:8px;top:auto;bottom:8px;gap:6px;padding:6px;background:rgba(2,8,5,.72)}
  html.whispers-desktop #sls-page .fmenu button{min-width:0;padding:5px 10px;font-size:12px;cursor:pointer}
  html.whispers-desktop #sls-page .fname{display:none}
  html.whispers-desktop #sls-page .msg,html.whispers-desktop #sls-page .toast{bottom:64px}
  html.whispers-desktop #sls-page .tools button,html.whispers-desktop #sls-page .cap button{cursor:pointer}
  html.whispers-desktop #sls-page .hist,html.whispers-desktop #sls-page .player{grid-column:1/-1}
  `;
  const style = document.createElement('style');
  style.id = 'whispers-desktop';
  style.textContent = material + ember + shell;
  document.head.appendChild(style);

  // Skin buttons next to the classic ones (footer), remembered between starts.
  const others = [...document.querySelectorAll('[data-skin]')];
  const last = others[others.length - 1];
  const mine = {};
  if (last) {
    const make = (skin, label) => { const b = last.cloneNode(false); b.dataset.skin = skin; b.textContent = label; return b; };
    mine.material = make('material', 'Material'); mine.ember = make('ember', 'Ember');
    last.after(mine.material, mine.ember);
    const mark = (b, on) => {
      b.setAttribute('aria-pressed', String(on));
      b.style.borderColor = on ? '#38bdf8' : ''; b.style.color = on ? '#a5f3fc' : ''; b.style.backgroundColor = on ? '#083344' : '';
      b.textContent = (on ? '● ' : '') + b.textContent.replace(/^● /, '');
    };
    const clear = () => { root.classList.remove('whispers-material', 'whispers-ember'); Object.values(mine).forEach(b => mark(b, false)); };
    const turnOn = mode => {
      q('[data-skin="void"]')?.click();
      others.forEach(b => mark(b, false));
      clear();
      root.classList.add('whispers-material');
      if (mode === 'ember') root.classList.add('whispers-ember');
      mark(mine[mode], true); save('whispers-material', mode);
    };
    mine.material.addEventListener('click', () => turnOn('material'));
    mine.ember.addEventListener('click', () => turnOn('ember'));
    others.forEach(b => b.addEventListener('click', () => { if (root.classList.contains('whispers-material')) { clear(); save('whispers-material', ''); } }));
    const saved = load('whispers-material', '');
    if (mine[saved]) turnOn(saved); else clear();
  }

  // Top-bar buttons, placed before the power switch.
  const tools = document.createElement('div');
  tools.id = 'desk-tools';
  tools.innerHTML = '<button type="button" id="desk-sls"><span class="desk-radar"><i></i><b></b></span><span>SLS CAMERA</span></button>' +
    '<button type="button" id="desk-gear" aria-label="Settings" title="Settings">⚙</button>';
  const right = q('header') && [...q('header').children].pop();
  if (right) right.prepend(tools); else document.body.appendChild(tools);

  // ---------- languages (same table and rules as the phone) ----------
  const dict = window.__whispersI18n || {};
  const langs = window.__whispersLanguages || [{ code: 'en', flag: '🇬🇧', name: 'English' }];
  const codes = ['mt', 'it', 'es', 'fr', 'de'];
  const table = new Map(Object.entries(dict).map(([k, v]) => [k.replace(/\s+/g, ' ').trim().toUpperCase(), v]));
  let lang = load('whispers-lang', 'en');
  const originals = new WeakMap(), written = new WeakMap();
  const isUpper = t => t === t.toUpperCase() && /[A-Z]/.test(t);
  const lookup = core => {
    const v = table.get(core.toUpperCase()); const i = codes.indexOf(lang);
    if (!v || i < 0) return null;
    return isUpper(core) ? v[i].toUpperCase() : v[i];
  };
  const translateText = text => {
    if (lang === 'en' || !/\p{L}{2}/u.test(text)) return text;
    const whole = text.replace(/\s+/g, ' ').trim();
    const direct = lookup(whole);
    if (direct !== null) return text.replace(whole.length ? text.trim() : '', direct);
    const m = text.match(/^(\s*[^\p{L}\p{N}]*)(.*?)([^\p{L}\p{N})]*\s*)$/su);
    const lead = m ? m[1] : '', core = (m ? m[2] : text).replace(/\s+/g, ' '), tail = m ? m[3] : '';
    let out = lookup(core);
    if (out === null && /\s[•·]\s/.test(core)) out = core.split(/(\s[•·]\s)/).map(p => /\s[•·]\s/.test(p) ? p : (lookup(p.trim()) ?? p)).join('');
    if (out === null && /^[^:]{2,30}:\s/.test(core)) { const [label, ...rest] = core.split(':'); const l = lookup(label.trim()); if (l) out = l + ':' + rest.join(':'); }
    return out === null ? text : lead + out + tail;
  };
  const skip = el => !el || el.closest('script,style,select[data-takes],#session-log,textarea,[data-no-translate]') ||
    el.matches?.('[data-skin]') || /^(⟳ BOUNCE|▲ UP|▼ DOWN|⤨ RANDOM)$/.test((el.textContent || '').trim());
  const doNode = node => {
    if (skip(node.parentElement)) return;
    const now = node.data;
    if (written.get(node) !== now) originals.set(node, now);
    const want = translateText(originals.get(node) ?? now);
    if (want !== now) { written.set(node, want); node.data = want; } else written.set(node, now);
  };
  const doTree = el => {
    const w = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
    let n; while ((n = w.nextNode())) doNode(n);
    el.querySelectorAll?.('[placeholder]').forEach(p => {
      if (!p.dataset.phPlaceholder) p.dataset.phPlaceholder = p.getAttribute('placeholder');
      p.setAttribute('placeholder', translateText(p.dataset.phPlaceholder));
    });
  };
  window.__whispersTr = text => translateText(text);
  window.__whispersOriginalText = el => {
    let out = ''; const w = document.createTreeWalker(el, NodeFilter.SHOW_TEXT); let n;
    while ((n = w.nextNode())) out += originals.get(n) ?? n.data;
    return out;
  };
  const observer = new MutationObserver(list => {
    for (const m of list) {
      if (m.type === 'characterData') doNode(m.target);
      else m.addedNodes.forEach(n => n.nodeType === 3 ? doNode(n) : n.nodeType === 1 && doTree(n));
    }
  });

  // ---------- Settings drawer ----------
  // How-to steps for the computer (the phone's steps talk about swiping and page numbers).
  const STEPS = {
    en: [['Choose a radio source', 'In panel 01, pick KiwiSDR (real radio receivers over the internet), Internet radio or Gqrx (your own SDR). For KiwiSDR, click “Scan public SDR receivers” and choose one.'],
      ['Start listening', 'Click START SWEEP at the bottom. Screen 02 shows the frequency and the live sound. Click it again to stop. No radio? Tick “Test tone” in panel 01.'],
      ['Record', 'Click REC EVP to record what comes through. Ask your questions with “Record question” — your voice is saved separately from the radio.'],
      ['Shape the sound', 'Panels 03 and 04: noise filter, faders, echo, reverb and ghost-box chop.'],
      ['Review your recordings', 'In panel 06, open the Review deck. Slow it down (0.5×, 0.25×…), look at the heat map and add notes where you hear something.'],
      ['SLS camera', 'Click SLS CAMERA in the top bar. Your webcam draws a stick figure over body-like shapes. The radio keeps playing, and figures put markers on a running radio recording.']],
    mt: [['Agħżel sors tar-radju', 'Fil-panel 01, agħżel KiwiSDR (riċevituri veri fuq l-internet), radju fuq l-internet jew Gqrx (l-SDR tiegħek). Għal KiwiSDR, agħfas “Scan public SDR receivers” u agħżel wieħed.'],
      ['Ibda isma\'', 'Agħfas START SWEEP isfel. L-iskrin 02 juri l-frekwenza u l-ħoss live. Agħfas mill-ġdid biex tieqaf. M\'għandekx radju? Immarka “Test tone” fil-panel 01.'],
      ['Irrekordja', 'Agħfas REC EVP biex tirrekordja dak li jidħol. Agħmel il-mistoqsijiet tiegħek b\'“Record question” — leħnek jiġi salvat separat mir-radju.'],
      ['Saħħaħ il-ħoss', 'Panels 03 u 04: filtru tal-istorbju, faders, eko, riverberazzjoni u qtugħ ghost box.'],
      ['Irrevedi r-reġistrazzjonijiet', 'Fil-panel 06, iftaħ ir-Review deck. Naqqas il-veloċità (0.5×, 0.25×…), ħares lejn il-heat map u żid noti fejn tisma\' xi ħaġa.'],
      ['Kamera SLS', 'Agħfas SLS CAMERA fil-bar ta\' fuq. Il-webcam tpinġi figura fuq forom qishom ġisem. Ir-radju jibqa\' jdoqq, u l-figuri jpoġġu marki fuq reġistrazzjoni tar-radju li tkun għaddejja.']],
    it: [['Scegli una sorgente radio', 'Nel pannello 01 scegli KiwiSDR (veri ricevitori su internet), radio via internet o Gqrx (il tuo SDR). Per KiwiSDR clicca “Scan public SDR receivers” e scegline uno.'],
      ['Inizia ad ascoltare', 'Clicca START SWEEP in basso. Lo schermo 02 mostra la frequenza e il suono dal vivo. Clicca di nuovo per fermare. Niente radio? Spunta “Test tone” nel pannello 01.'],
      ['Registra', 'Clicca REC EVP per registrare ciò che arriva. Fai le tue domande con “Record question” — la tua voce è salvata separatamente dalla radio.'],
      ['Modella il suono', 'Pannelli 03 e 04: filtro rumore, fader, eco, riverbero e taglio ghost box.'],
      ['Rivedi le registrazioni', 'Nel pannello 06 apri il Review deck. Rallenta (0.5×, 0.25×…), guarda la mappa di calore e aggiungi note dove senti qualcosa.'],
      ['Fotocamera SLS', 'Clicca SLS CAMERA nella barra in alto. La webcam disegna una figura sulle forme simili a un corpo. La radio continua a suonare e le figure aggiungono marcatori a una registrazione radio in corso.']],
    es: [['Elige una fuente de radio', 'En el panel 01 elige KiwiSDR (receptores reales por internet), radio por internet o Gqrx (tu propio SDR). Para KiwiSDR, haz clic en “Scan public SDR receivers” y elige uno.'],
      ['Empieza a escuchar', 'Haz clic en START SWEEP abajo. La pantalla 02 muestra la frecuencia y el sonido en directo. Vuelve a hacer clic para parar. ¿Sin radio? Marca “Test tone” en el panel 01.'],
      ['Graba', 'Haz clic en REC EVP para grabar lo que llega. Haz tus preguntas con “Record question”: tu voz se guarda aparte de la radio.'],
      ['Da forma al sonido', 'Paneles 03 y 04: filtro de ruido, faders, eco, reverberación y corte ghost box.'],
      ['Revisa tus grabaciones', 'En el panel 06 abre el Review deck. Ralentiza (0.5×, 0.25×…), mira el mapa de calor y añade notas donde oigas algo.'],
      ['Cámara SLS', 'Haz clic en SLS CAMERA en la barra superior. La webcam dibuja una figura sobre formas parecidas a un cuerpo. La radio sigue sonando y las figuras ponen marcadores en una grabación de radio en curso.']],
    fr: [['Choisir une source radio', 'Dans le panneau 01, choisissez KiwiSDR (vrais récepteurs sur internet), la radio internet ou Gqrx (votre SDR). Pour KiwiSDR, cliquez sur « Scan public SDR receivers » et choisissez-en un.'],
      ['Commencer l\'écoute', 'Cliquez sur START SWEEP en bas. L\'écran 02 affiche la fréquence et le son en direct. Cliquez à nouveau pour arrêter. Pas de radio ? Cochez « Test tone » dans le panneau 01.'],
      ['Enregistrer', 'Cliquez sur REC EVP pour enregistrer ce qui arrive. Posez vos questions avec « Record question » — votre voix est enregistrée à part de la radio.'],
      ['Façonner le son', 'Panneaux 03 et 04 : filtre de bruit, faders, écho, réverbération et découpe ghost box.'],
      ['Réécouter vos enregistrements', 'Dans le panneau 06, ouvrez le Review deck. Ralentissez (0.5×, 0.25×…), regardez la carte de chaleur et ajoutez des notes là où vous entendez quelque chose.'],
      ['Caméra SLS', 'Cliquez sur SLS CAMERA dans la barre du haut. La webcam dessine une silhouette sur les formes qui ressemblent à un corps. La radio continue, et les silhouettes ajoutent des repères à un enregistrement radio en cours.']],
    de: [['Radioquelle wählen', 'Wähle in Feld 01 KiwiSDR (echte Empfänger über das Internet), Internetradio oder Gqrx (dein eigenes SDR). Für KiwiSDR klicke auf „Scan public SDR receivers“ und wähle einen aus.'],
      ['Zuhören', 'Klicke unten auf START SWEEP. Bildschirm 02 zeigt die Frequenz und den Live-Ton. Nochmal klicken zum Stoppen. Kein Radio? Setze in Feld 01 den Haken bei „Test tone“.'],
      ['Aufnehmen', 'Klicke auf REC EVP, um aufzunehmen, was hereinkommt. Stelle deine Fragen mit „Record question“ — deine Stimme wird getrennt vom Radio gespeichert.'],
      ['Klang formen', 'Felder 03 und 04: Rauschfilter, Fader, Echo, Hall und Ghost-Box-Zerhacker.'],
      ['Aufnahmen prüfen', 'Öffne in Feld 06 das Review deck. Verlangsamen (0.5×, 0.25×…), Wärmebild ansehen und Notizen setzen, wo du etwas hörst.'],
      ['SLS-Kamera', 'Klicke oben auf SLS CAMERA. Die Webcam zeichnet eine Strichfigur über körperähnliche Formen. Das Radio läuft weiter, und Figuren setzen Markierungen in eine laufende Radioaufnahme.']],
  };
  const tx = () => (window.__whispersSettingsText || {})[lang] || (window.__whispersSettingsText || {}).en || {};
  const platform = /Linux/i.test(navigator.userAgent) && !/Android/i.test(navigator.userAgent) ? 'Linux' : 'Windows';
  // Windows shows flag emoji as two letters, so the flags are small drawings.
  const svgFlag = body => '<svg viewBox="0 0 24 16" width="26" height="17" style="border-radius:2px;box-shadow:0 0 0 1px rgba(255,255,255,.18)">' + body + '</svg>';
  const FLAGS = {
    en: svgFlag('<rect width="24" height="16" fill="#012169"/><path d="M0 0L24 16M24 0L0 16" stroke="#fff" stroke-width="3.2"/><path d="M0 0L24 16M24 0L0 16" stroke="#C8102E" stroke-width="1.2"/>' +
      '<path d="M12 0V16M0 8H24" stroke="#fff" stroke-width="5"/><path d="M12 0V16M0 8H24" stroke="#C8102E" stroke-width="3"/>'),
    mt: svgFlag('<rect width="12" height="16" fill="#fff"/><rect x="12" width="12" height="16" fill="#CF142B"/><path d="M2.5 2.2h2v1h1v2h-1v1h-2v-1h-1v-2h1z" fill="#a0a0a0"/>'),
    it: svgFlag('<rect width="8" height="16" fill="#009246"/><rect x="8" width="8" height="16" fill="#fff"/><rect x="16" width="8" height="16" fill="#CE2B37"/>'),
    es: svgFlag('<rect width="24" height="16" fill="#AA151B"/><rect y="4" width="24" height="8" fill="#F1BF00"/>'),
    fr: svgFlag('<rect width="8" height="16" fill="#0055A4"/><rect x="8" width="8" height="16" fill="#fff"/><rect x="16" width="8" height="16" fill="#EF4135"/>'),
    de: svgFlag('<rect width="24" height="5.34" fill="#000"/><rect y="5.33" width="24" height="5.34" fill="#DD0000"/><rect y="10.66" width="24" height="5.34" fill="#FFCE00"/>'),
  };
  const SKIN_COLOURS = { void: '#0ea5e9', military: '#65a30d', nebula: '#a855f7', obsidian: '#64748b', material: '#00ff88', ember: '#ff8a3d' };
  const shade = document.createElement('div'); shade.id = 'desk-shade';
  const panel = document.createElement('aside');
  panel.id = 'desk-settings'; panel.setAttribute('role', 'dialog'); panel.setAttribute('aria-label', 'Settings'); panel.dataset.noTranslate = '1';
  document.body.append(shade, panel);
  let textScale = Number(load('whispers-desk-textsize', '1')) || 1;
  const applyScale = () => root.style.setProperty('--dz', String(textScale));
  const themes = () => [...document.querySelectorAll('[data-skin]')];
  const renderSettings = () => {
    const t = tx(), steps = STEPS[lang] || STEPS.en;
    const current = themes().find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.skin;
    panel.innerHTML =
      '<div class="top"><h2>⚙ ' + esc(t.title) + '</h2><button type="button" class="x" data-close>✕ ' + esc(t.close) + '</button></div>' +
      '<section><h3>🌐 ' + esc(t.language) + '</h3><div class="grid">' +
        langs.map(l => '<button type="button" class="opt" data-lang="' + l.code + '" aria-pressed="' + (l.code === lang) + '"><span class="flag">' + (FLAGS[l.code] || l.flag) + '</span>' + esc(l.name) + '</button>').join('') +
      '</div></section>' +
      '<section><h3>🎨 ' + esc(t.theme) + '</h3><div class="grid">' +
        themes().map(b => '<button type="button" class="opt" data-theme="' + b.dataset.skin + '" aria-pressed="' + (b.dataset.skin === current) + '"><span class="sw" style="background:' +
          (SKIN_COLOURS[b.dataset.skin] || '#64748b') + '"></span>' + esc(b.textContent.replace(/^● /, '')) + '</button>').join('') +
      '</div></section>' +
      '<section><h3>🔠 ' + esc(t.textSize) + '</h3><div class="grid" style="grid-template-columns:1fr 1fr 1fr">' +
        [1, 1.12, 1.25].map((v, i) => '<button type="button" class="opt" data-size="' + v + '" aria-pressed="' + (Math.abs(textScale - v) < .01) + '" style="justify-content:center;font-size:' + (0.9 + i * 0.12) + 'em">' + esc((t.sizes || [])[i] || '') + '</button>').join('') +
      '</div></section>' +
      '<section><h3>📖 ' + esc(t.guide) + '</h3>' +
        steps.map((s, i) => '<details' + (i === 0 ? ' open' : '') + '><summary><b>' + (i + 1) + '</b>' + esc(s[0]) + '</summary><p>' + esc(s[1]) + '</p></details>').join('') +
        '<p class="note">💡 ' + esc(t.tip) + '</p></section>' +
      '<section class="about"><h3>ℹ️ ' + esc(t.about) + '</h3>' +
        '<div class="brand">WHISPERS OF THE SPIRIT</div>' +
        '<dl><dt>' + esc(t.version) + '</dt><dd>2.3 · ' + platform + '</dd>' +
        '<dt>' + esc(t.author) + '</dt><dd>debattistacliff-commits</dd>' +
        '<dt>©</dt><dd>© 2026 debattistacliff-commits. ' + esc(t.rights) + '</dd>' +
        '<dt>' + esc(t.source) + '</dt><dd>github.com/debattistacliff-commits/whispers-of-the-spirit</dd></dl>' +
        '<p class="note">⚠️ ' + esc(t.disclaimer) + '</p><p class="note">' + esc(t.credits) + '</p><p class="note">👻 ' + esc(t.made) + '</p></section>';
  };
  const closeAll = () => { panel.classList.remove('open'); closeSls(); shade.classList.remove('on'); };
  const openSettings = () => { closeSls(); renderSettings(); panel.classList.add('open'); shade.classList.add('on'); panel.scrollTop = 0; };
  const setLanguage = code => {
    lang = langs.some(l => l.code === code) ? code : 'en';
    save('whispers-lang', lang);
    root.lang = lang;
    doTree(document.body);
    renderSettings();
  };
  panel.addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return;
    if (b.hasAttribute('data-close')) closeAll();
    else if (b.dataset.lang) setLanguage(b.dataset.lang);
    else if (b.dataset.theme) { q('[data-skin="' + b.dataset.theme + '"]')?.click(); setTimeout(renderSettings, 50); }
    else if (b.dataset.size) { textScale = Number(b.dataset.size); save('whispers-desk-textsize', textScale); applyScale(); renderSettings(); }
  });
  shade.addEventListener('click', closeAll);
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && (panel.classList.contains('open') || win.classList.contains('open'))) closeAll(); });

  // ---------- SLS camera window ----------
  const win = document.createElement('div');
  win.id = 'sls-win'; win.setAttribute('role', 'dialog'); win.setAttribute('aria-label', 'SLS camera');
  win.innerHTML = '<div class="wbar"><span class="desk-radar"><i></i><b></b></span><span>SLS CAMERA</span><span class="sp"></span>' +
    '<button type="button" data-sls-close>✕ CLOSE</button></div><div id="sls-page"></div>';
  document.body.appendChild(win);
  const slsEvent = section => window.dispatchEvent(new CustomEvent('whispers-page', { detail: { index: section === 'sls' ? 1 : 0, section } }));
  function closeSls() { if (!win.classList.contains('open')) return; win.classList.remove('open'); slsEvent('desktop'); }
  const openSls = () => { panel.classList.remove('open'); win.classList.add('open'); shade.classList.add('on'); slsEvent('sls'); };
  win.querySelector('[data-sls-close]').addEventListener('click', closeAll);
  q('#desk-sls').addEventListener('click', openSls);
  // The camera needs a secure page with webcam access (Windows build). Elsewhere the SLS button is not shown.
  if (!(location.protocol === 'https:' && navigator.mediaDevices && navigator.mediaDevices.getUserMedia)) q('#desk-sls').hidden = true;
  q('#desk-gear').addEventListener('click', openSettings);
  window.__whispersGoToSection = section => (section === 'sls' ? openSls() : closeAll());

  applyScale();
  observer.observe(document.body, { subtree: true, childList: true, characterData: true });
  setLanguage(lang);
  // After phone-sls.js has filled the window: show the filters as a row of buttons.
  setTimeout(() => q('#sls-page [data-filter]')?.click(), 0);

  // ---------- old television switch-on when the app opens ----------
  const glow = root.classList.contains('whispers-ember') ? '255,138,61' : '120,230,255';
  style.textContent += `
  #crt-on{position:fixed;inset:0;z-index:2147483646;pointer-events:none;background:#000;animation:crt-reveal 1.15s ease-out forwards}
  #crt-on i{position:absolute;inset:0;transform-origin:center;
    background:radial-gradient(ellipse at center,#fff 0%,rgba(${glow},.95) 25%,rgba(${glow},.35) 55%,transparent 78%);
    animation:crt-beam 1.15s cubic-bezier(.2,.7,.2,1) forwards}
  #crt-on b{position:absolute;inset:0;background:repeating-linear-gradient(0deg,rgba(0,0,0,.35) 0 2px,transparent 2px 4px);animation:crt-flicker 1.15s steps(1) forwards}
  @keyframes crt-beam{0%{transform:scale(.01,.004);opacity:1}18%{transform:scale(.01,.004)}38%{transform:scale(1.2,.006);opacity:1}
    62%{transform:scale(1.2,1.25);opacity:.85}100%{transform:scale(1.2,1.25);opacity:0}}
  @keyframes crt-reveal{0%,52%{background:#000}100%{background:transparent}}
  @keyframes crt-flicker{0%{opacity:0}55%{opacity:1}62%{opacity:.4}70%{opacity:.9}78%{opacity:.3}88%{opacity:.6}100%{opacity:0}}
  `;
  const tv = document.createElement('div');
  tv.id = 'crt-on'; tv.setAttribute('aria-hidden', 'true'); tv.innerHTML = '<i></i><b></b>';
  document.body.appendChild(tv);
  setTimeout(() => tv.remove(), 1300);
})();
