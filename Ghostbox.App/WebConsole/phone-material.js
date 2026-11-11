// Whispers of the Spirit — "Material" and "Ember" skins (Android build only).
// Adds two skin buttons. Material keeps the console's layout and colours and only changes how the
// controls are made: raised soft panels, pill switches with a glowing ball, glowing sliders and
// faders, rounded hardware buttons and a lit ring on the knob. Ember is the same material in
// charcoal grey with orange lights. Choosing any other skin turns them off.
// The Review deck is left as it is.
(() => {
  if (window.__whispersPhoneMaterialInstalled) return;
  window.__whispersPhoneMaterialInstalled = true;
  const S = 'html.whispers-material [data-purpose="workstation-hardware-enclosure"]';
  const css = `
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
  // "Ember": the same material in charcoal grey with orange lights, like the reference picture.
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
  // Larger text on the phone, for every skin: the console's 8–10 px labels are hard to read there.
  const C = '[data-purpose="workstation-hardware-enclosure"]';
  const readable = `
  ${C} [class*="text-[7px]"],${C} [class*="text-[8px]"]{font-size:calc(12.5px * var(--tz,1))!important;line-height:1.3!important}
  ${C} [class*="text-[9px]"],${C} [class*="text-[10px]"]{font-size:calc(13.5px * var(--tz,1))!important;line-height:1.35!important}
  ${C} [class*="text-[11px]"],${C} .text-xs{font-size:calc(14.5px * var(--tz,1))!important;line-height:1.4!important}
  ${C} .text-sm{font-size:calc(16px * var(--tz,1))!important}
  ${C} input,${C} select{font-size:calc(15px * var(--tz,1))!important}
  ${C} *{overflow-wrap:normal;word-break:normal;hyphens:none}
  ${C} [class*="tracking-"]{letter-spacing:.03em!important}
  ${C} header [class*="text-[7px]"],${C} header [class*="text-[8px]"],${C} header [class*="text-[9px]"]{font-size:10px!important}
  `;
  const style = document.createElement('style');
  style.id = 'whispers-phone-material';
  style.textContent = readable + css + ember;
  document.head.appendChild(style);

  const others = [...document.querySelectorAll('[data-skin]')];
  const last = others[others.length - 1];
  if (!last) return;
  const make = (skin, label) => { const b = last.cloneNode(false); b.dataset.skin = skin; b.textContent = label; return b; };
  const material = make('material', 'Material'), emberButton = make('ember', 'Ember');
  last.after(material, emberButton);
  const mine = { material, ember: emberButton };

  const mark = (b, on) => {
    b.setAttribute('aria-pressed', String(on));
    b.style.borderColor = on ? '#38bdf8' : ''; b.style.color = on ? '#a5f3fc' : ''; b.style.backgroundColor = on ? '#083344' : '';
    b.textContent = (on ? '● ' : '') + b.textContent.replace(/^● /, '');
  };
  const root = document.documentElement;
  const save = mode => { try { localStorage.setItem('whispers-material', mode); } catch (e) { } };
  const clear = () => { root.classList.remove('whispers-material', 'whispers-ember'); Object.values(mine).forEach(b => mark(b, false)); };
  const turnOn = mode => {
    document.querySelector('[data-skin="void"]')?.click(); // plain colours underneath, as in Void Celestial
    others.forEach(b => mark(b, false));
    clear();
    root.classList.add('whispers-material');
    if (mode === 'ember') root.classList.add('whispers-ember');
    mark(mine[mode], true); save(mode);
  };
  material.addEventListener('click', () => turnOn('material'));
  emberButton.addEventListener('click', () => turnOn('ember'));
  others.forEach(b => b.addEventListener('click', () => {
    if (!root.classList.contains('whispers-material')) return;
    clear(); save('');
  }));
  let saved = ''; try { saved = localStorage.getItem('whispers-material') || ''; } catch (e) { }
  if (saved === '1') saved = 'material';
  if (mine[saved]) turnOn(saved); else clear();

  // ---------- swipeable pages for the phone ----------
  // The console's panels are moved (not rebuilt, so every control keeps working) into six
  // screen-sized pages that slide sideways under your finger, grouped as Listen / Effects / Review.
  // Start/Stop and Rec stay at the bottom of every page.
  const q = s => document.querySelector(s);
  const kids = e => e ? [...e.children].filter(c => c.nodeType === 1) : [];
  const chassis = q('[data-purpose="workstation-hardware-enclosure"]');
  const main = q('[data-purpose="rackmount-subsystems"]');
  const receiver = kids(q('[data-purpose="module-sdr-receiver"]'));
  const display = kids(q('[data-purpose="module-crt-oscilloscope"]'));
  const knobs = kids(q('[data-purpose="module-tactile-controls"]'));
  const studies = kids(q('[data-purpose="module-harmonics-and-temporal-slowdown"]'));
  const deckRow = kids(q('[data-purpose="module-master-deck"]'));
  const footer = chassis && kids(chassis).find(e => e.tagName === 'FOOTER');
  if (!chassis || !main || receiver.length < 2 || knobs.length < 2 || display.length < 2 || studies.length < 2 || deckRow.length < 2) return;
  const plan = [
    ['listen', [display[0]]],                      // live receiver screen
    ['listen', [receiver[0]]],                     // receiver and sweep settings
    ['effects', [knobs[0], receiver[1]]],          // switches and faders + DSP filters
    ['effects', [knobs[1], display[1]]],           // voice effects + scopes
    ['effects', [studies[0]]],                     // sound analyser
    ['review', [studies[1], footer]],              // slow-down + skins
    ['review', [deckRow[1], deckRow[0]]],          // recordings / Review deck + session log
  ];
  const sections = ['listen', 'effects', 'review'];

  const pager = document.createElement('div');
  pager.id = 'phone-pager';
  const pageEls = plan.map(([section, items]) => {
    const page = document.createElement('div'); page.className = 'pp-page'; page.dataset.section = section;
    const inner = document.createElement('div'); inner.className = 'pp-inner';
    items.filter(Boolean).forEach(item => inner.appendChild(item));
    page.appendChild(inner); pager.appendChild(page);
    return page;
  });
  chassis.insertBefore(pager, main);

  const pagesCss = `
  html.phone-pages,html.phone-pages body{height:100%;overflow:hidden!important;overscroll-behavior:none}
  html.phone-pages body{padding:0!important;display:block!important}
  html.phone-pages [data-purpose="workstation-hardware-enclosure"]{border-radius:0!important;border-width:0!important;max-width:none!important;height:var(--pp-h);background:#070b12}
  html.phone-pages [data-purpose="workstation-hardware-enclosure"] > header,html.phone-pages [data-purpose="rackmount-subsystems"]{display:none!important}
  #phone-pager{display:flex;height:100%;overflow:hidden;touch-action:pan-y}
  #phone-pager .pp-page{flex:0 0 100%;height:100%;overflow-y:auto;overflow-x:hidden;padding:10px;transform:translateX(var(--pp-x,0px));will-change:transform}
  #phone-pager.pp-anim .pp-page{transition:transform .28s cubic-bezier(.2,.8,.2,1)}
  #phone-pager .pp-inner{display:flex;flex-direction:column;gap:12px}
  #phone-pager .pp-inner > *{flex:none!important;width:100%!important;margin:0!important}
  #phone-pager footer{border-radius:12px;border:1px solid #1e2a3c}
  #phone-pager footer div{flex-wrap:wrap}
  #phone-bar{position:fixed;left:0;right:0;bottom:0;z-index:100000;background:#070b12;border-top:1px solid #1e2a3c;
    font-family:"Share Tech Mono","JetBrains Mono",monospace;box-shadow:0 -6px 18px rgba(0,0,0,.7)}
  #phone-bar .st{color:#7dd3fc;font-size:12px;padding:5px 10px 0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
  #phone-bar .dots{display:flex;justify-content:center;gap:6px;padding:7px 10px 0}
  #phone-bar .dots i{font-style:normal;min-width:38px;height:36px;display:flex;align-items:center;justify-content:center;border-radius:9px;
    font-size:16px;font-weight:700;color:#94a3b8;background:#0f1724;border:1px solid #1e2a3c;box-shadow:0 2px 4px rgba(0,0,0,.7);transition:all .15s}
  #phone-bar .dots i.gap{margin-left:12px}
  #phone-bar .dots i.on{color:#04120b;background:#00ff88;border-color:#00ff88;box-shadow:0 0 10px rgba(0,255,136,.5)}
  #phone-bar .act{display:grid;grid-template-columns:1fr 1fr;gap:8px;padding:6px 10px}
  #phone-bar .act button{border-radius:10px;padding:10px 0;font-size:15px;font-weight:700;letter-spacing:.06em;border:1px solid #38bdf8;color:#a5f3fc;background:#0b1622;
    box-shadow:0 3px 7px rgba(0,0,0,.8),inset 0 1px 0 rgba(255,255,255,.12)}
  #phone-bar .act .rec{border-color:#ef4444;color:#fca5a5;background:#22090c}
  #phone-bar .tabs{display:grid;grid-template-columns:1fr 1fr 1fr 64px;border-top:1px solid #1e2a3c}
  #phone-bar .tabs button{padding:10px 0;font-size:14px;letter-spacing:.08em;color:#64748b;background:none;border:0;border-top:3px solid transparent}
  #phone-bar .tabs button[aria-selected="true"]{color:#00ff88;border-top-color:#00ff88}
  html.whispers-ember #phone-bar{background:#0f1012;border-top-color:#2c2d33}
  html.whispers-ember #phone-bar .st{color:#ffb27a}
  html.whispers-ember #phone-bar .dots i{background:#1c1d21;border-color:#2c2d33}
  html.whispers-ember #phone-bar .dots i.on{color:#1a0b02;background:#ff8a3d;border-color:#ff8a3d;box-shadow:0 0 10px rgba(255,122,42,.5)}
  html.whispers-ember #phone-bar .act button{border-color:#ff7a2a;color:#ffb27a;background:#1c1d21}
  html.whispers-ember #phone-bar .act .rec{border-color:#7f1d1d;color:#fca5a5;background:#2a0d10}
  html.whispers-ember #phone-bar .tabs button[aria-selected="true"]{color:#ff8a3d;border-top-color:#ff8a3d}
  html.whispers-ember [data-purpose="workstation-hardware-enclosure"]{background:#121316!important}
  `;
  style.textContent += pagesCss;

  const bar = document.createElement('nav');
  bar.id = 'phone-bar';
  bar.setAttribute('aria-label', 'Console pages');
  bar.innerHTML = '<div class="st" role="status"></div><div class="dots">' +
    plan.map(([s], i) => '<i role="button" tabindex="0" aria-label="Page ' + (i + 1) + '" class="' + (i && plan[i - 1][0] !== s ? 'gap' : '') + '">' + (i + 1) + '</i>').join('') +
    '</div><div class="act"><button type="button" class="go">↻ START</button><button type="button" class="rec">● REC</button></div>' +
    '<div class="tabs" role="tablist">' + sections.map(s => '<button type="button" role="tab" data-go="' + s + '">' + s.toUpperCase() + '</button>').join('') +
    '<button type="button" class="gear" aria-label="Settings">⚙</button></div>';
  document.body.appendChild(bar);
  root.classList.add('phone-pages');
  const go = bar.querySelector('.go'), rec = bar.querySelector('.rec'), st = bar.querySelector('.st');
  const dots = [...bar.querySelectorAll('.dots i')];
  go.addEventListener('click', () => q('#native-start')?.click());
  rec.addEventListener('click', () => q('#native-record')?.click());

  let index = 0;
  const showPage = () => {
    dots.forEach((d, i) => d.classList.toggle('on', i === index));
    bar.querySelectorAll('[data-go]').forEach(b => b.setAttribute('aria-selected', String(b.dataset.go === plan[index][0])));
    try { sessionStorage.setItem('whispers-page', String(index)); } catch (e) { }
  };
  let lastSlide = 0;
  const place = (offset, animate) => {
    pager.classList.toggle('pp-anim', !!animate);
    pager.style.setProperty('--pp-x', (-index * pager.clientWidth + offset) + 'px');
  };
  const goTo = (i, smooth) => {
    index = Math.max(0, Math.min(plan.length - 1, i));
    place(0, smooth);
    if (smooth) lastSlide = Date.now();
    showPage();
  };
  // One page per swipe: the page follows the finger and settles on the next or previous page.
  let drag = null;
  pager.addEventListener('touchstart', e => {
    if (e.touches.length !== 1) { drag = null; return; }
    const t = e.touches[0];
    drag = { x: t.clientX, y: t.clientY, t: Date.now(), dx: 0, mode: '' };
  }, { passive: true });
  pager.addEventListener('touchmove', e => {
    if (!drag) return;
    if (window.__ppSliderActive) { drag = null; place(0, true); return; }
    const t = e.touches[0], dx = t.clientX - drag.x, dy = t.clientY - drag.y;
    if (!drag.mode) {
      if (Math.abs(dx) < 10 && Math.abs(dy) < 10) return;
      drag.mode = Math.abs(dx) > Math.abs(dy) ? 'h' : 'v';
    }
    if (drag.mode !== 'h') return;
    e.preventDefault();
    const edge = (index === 0 && dx > 0) || (index === plan.length - 1 && dx < 0);
    drag.dx = edge ? dx / 3 : dx;
    lastSlide = Date.now();
    place(drag.dx, false);
  }, { passive: false });
  const finishDrag = () => {
    if (!drag) return;
    const d = drag; drag = null;
    if (d.mode !== 'h') return;
    const fast = Math.abs(d.dx) / Math.max(1, Date.now() - d.t) > 0.35;
    const step = Math.abs(d.dx) > pager.clientWidth * 0.22 || (fast && Math.abs(d.dx) > 30) ? (d.dx < 0 ? 1 : -1) : 0;
    lastSlide = Date.now();
    goTo(index + step, true);
  };
  pager.addEventListener('touchend', finishDrag, { passive: true });
  pager.addEventListener('touchcancel', finishDrag, { passive: true });
  bar.querySelectorAll('[data-go]').forEach(b => b.addEventListener('click', () => goTo(plan.findIndex(p => p[0] === b.dataset.go), true)));
  dots.forEach((d, i) => d.addEventListener('click', () => goTo(i, true)));

  // Each page fits the screen: a page that is a little too tall is shrunk to fit (never below 80%);
  // anything still taller can be scrolled up and down inside that page.
  const fit = () => {
    root.style.setProperty('--pp-h', (window.innerHeight - bar.offsetHeight) + 'px');
    pageEls.forEach(page => {
      const inner = page.firstChild;
      inner.style.zoom = '';
      const room = page.clientHeight - 20, need = inner.scrollHeight;
      if (need > room) inner.style.zoom = String(Math.max(0.8, room / need));
    });
  };
  window.addEventListener('resize', () => { fit(); goTo(index, false); });
  let startPage = 0; try { startPage = Number(sessionStorage.getItem('whispers-page')) || 0; } catch (e) { }
  requestAnimationFrame(() => { fit(); goTo(startPage, false); });
  setTimeout(() => { fit(); goTo(index, false); }, 700);
  setInterval(fit, 2000);

  // ---------- deliberate touches only ----------
  // Sliding between pages must not change anything by accident:
  //  - sliders and faders move only after a short touch-and-hold (they glow when grabbed);
  //  - buttons and switches ignore taps that come during or just after a page slide.
  const HOLD_MS = 230, MOVE_PX = 10;
  style.textContent += `
  html.phone-pages #phone-pager input[type=range]{pointer-events:none}
  html.phone-pages #phone-pager .pp-grab{outline:2px solid #38bdf8;outline-offset:6px;border-radius:8px;box-shadow:0 0 14px rgba(56,189,248,.6)}
  html.whispers-ember #phone-pager .pp-grab{outline-color:#ff8a3d;box-shadow:0 0 14px rgba(255,122,42,.6)}
  #phone-hint{position:fixed;left:50%;transform:translateX(-50%);bottom:calc(var(--bar-h,200px) + 12px);z-index:100001;pointer-events:none;
    background:#0b1622;color:#e0f2fe;border:1px solid #38bdf8;border-radius:10px;padding:8px 14px;font:14px "Share Tech Mono",monospace;
    opacity:0;transition:opacity .25s;white-space:nowrap}
  #phone-hint.show{opacity:1}
  html.whispers-ember #phone-hint{background:#1c1d21;color:#ffe0c8;border-color:#ff7a2a}
  `;
  const hint = document.createElement('div'); hint.id = 'phone-hint'; hint.textContent = 'Touch and hold to adjust';
  document.body.appendChild(hint);
  let hintTimer = 0;
  const showHint = () => {
    root.style.setProperty('--bar-h', bar.offsetHeight + 'px');
    hint.classList.add('show'); clearTimeout(hintTimer); hintTimer = setTimeout(() => hint.classList.remove('show'), 1600);
  };

  // The slider (or fader) under the finger; faders are invisible vertical sliders over their track.
  const sliderAt = (x, y) => {
    const page = pageEls[index]; if (!page) return null;
    for (const input of page.querySelectorAll('input[type=range]')) {
      const r = (input.closest('.fader-track') || input).getBoundingClientRect();
      if (r.width && x >= r.left - 12 && x <= r.right + 12 && y >= r.top - 14 && y <= r.bottom + 14) return input;
    }
    return null;
  };
  const setFrom = (input, x, y) => {
    const r = input.getBoundingClientRect();
    const vertical = r.height > r.width;
    let ratio = vertical ? (r.bottom - y) / r.height : (x - r.left) / r.width;
    ratio = Math.min(1, Math.max(0, ratio));
    const min = Number(input.min || 0), max = Number(input.max || 100), step = Number(input.step) || 1;
    let v = min + ratio * (max - min); v = Math.round((v - min) / step) * step + min;
    const text = String(Number(v.toFixed(6)));
    if (input.value !== text) { input.value = text; input.dispatchEvent(new Event('input', { bubbles: true })); }
  };
  let touch = null;
  document.addEventListener('touchstart', e => {
    if (e.touches.length !== 1 || !e.target.closest('#phone-pager')) { touch = null; return; }
    const t = e.touches[0], input = sliderAt(t.clientX, t.clientY);
    if (!input) { touch = null; return; }
    touch = { input, x: t.clientX, y: t.clientY, active: false, moved: false };
    const mine = touch;
    mine.timer = setTimeout(() => {
      if (touch !== mine || mine.moved) return;
      mine.active = true; window.__ppSliderActive = true;
      mine.glow = mine.input.closest('.fader-track') || mine.input;
      mine.glow.classList.add('pp-grab');
      try { navigator.vibrate && navigator.vibrate(15); } catch (err) { }
      setFrom(mine.input, mine.x, mine.y);
    }, HOLD_MS);
  }, { passive: true });
  document.addEventListener('touchmove', e => {
    if (!touch) return;
    const t = e.touches[0];
    if (touch.active) { e.preventDefault(); setFrom(touch.input, t.clientX, t.clientY); return; }
    if (Math.abs(t.clientX - touch.x) > MOVE_PX || Math.abs(t.clientY - touch.y) > MOVE_PX) touch.moved = true; // a swipe: leave the slider alone
  }, { passive: false });
  const endTouch = () => {
    window.__ppSliderActive = false;
    if (!touch) return;
    clearTimeout(touch.timer);
    if (touch.active) { touch.glow.classList.remove('pp-grab'); touch.input.dispatchEvent(new Event('change', { bubbles: true })); }
    else if (!touch.moved) showHint();
    touch = null;
  };
  document.addEventListener('touchend', endTouch, { passive: true });
  document.addEventListener('touchcancel', () => { window.__ppSliderActive = false; if (touch) { clearTimeout(touch.timer); touch.glow?.classList.remove('pp-grab'); touch = null; } }, { passive: true });

  // Taps that land during or right after a page slide are ignored (only real finger taps; the app's own clicks pass).
  document.addEventListener('click', e => {
    if (!e.isTrusted || !e.target.closest('#phone-pager')) return;
    if (Date.now() - lastSlide < 350) { e.preventDefault(); e.stopPropagation(); }
  }, true);

  // Keep the bottom buttons and status line in step with the console.
  setInterval(() => {
    const s = q('#native-start'), r = q('#native-record'), d = q('#phone-pager p[role=status]');
    const orig = el => (window.__whispersOriginalText ? window.__whispersOriginalText(el) : el.textContent);
    if (s) { const t = /■|STOP/i.test(orig(s)) ? '■ STOP' : '↻ START'; if (go.dataset.state !== t) { go.dataset.state = t; go.textContent = t; } }
    if (r) { const t = /RECORDING/i.test(orig(r)) ? '■ STOP REC' : '● REC'; if (rec.dataset.state !== t) { rec.dataset.state = t; rec.textContent = t; } }
    if (d && st.textContent !== d.textContent) st.textContent = d.textContent;
  }, 300);

  // ---------- languages ----------
  // Every visible English text is looked up in the translation table (phone-i18n.js). The English
  // original of each piece of text is remembered, so switching back to English restores it exactly,
  // and texts the app changes later (status lines, button labels) are translated as they appear.
  const dict = window.__whispersI18n || {};
  const langs = window.__whispersLanguages || [{ code: 'en', flag: '🇬🇧', name: 'English' }];
  const codes = ['mt', 'it', 'es', 'fr', 'de'];
  const table = new Map(Object.entries(dict).map(([k, v]) => [k.replace(/\s+/g, ' ').trim().toUpperCase(), v]));
  let lang = 'en'; try { lang = localStorage.getItem('whispers-lang') || 'en'; } catch (e) { }
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
  const doTree = rootEl => {
    const w = document.createTreeWalker(rootEl, NodeFilter.SHOW_TEXT);
    let n; while ((n = w.nextNode())) doNode(n);
    rootEl.querySelectorAll?.('[placeholder]').forEach(el => {
      if (!el.dataset.phPlaceholder) el.dataset.phPlaceholder = el.getAttribute('placeholder');
      el.setAttribute('placeholder', translateText(el.dataset.phPlaceholder));
    });
  };
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
  const setLanguage = code => {
    lang = langs.some(l => l.code === code) ? code : 'en';
    try { localStorage.setItem('whispers-lang', lang); } catch (e) { }
    root.lang = lang;
    doTree(document.body);
    renderSettings();
  };

  // ---------- Settings screen ----------
  const tx = () => (window.__whispersSettingsText || {})[lang] || (window.__whispersSettingsText || {}).en || {};
  style.textContent += `
  #phone-bar .tabs .gear{font-size:22px;color:#94a3b8;border-left:1px solid #1e2a3c}
  #phone-settings{position:fixed;inset:0;z-index:100002;background:#05080e;color:#cbd5e1;overflow-y:auto;display:none;
    font:calc(15px * var(--tz,1))/1.5 "Share Tech Mono","JetBrains Mono",monospace;padding:0 0 24px}
  #phone-settings.open{display:block}
  #phone-settings .top{position:sticky;top:0;display:flex;align-items:center;justify-content:space-between;padding:12px 14px;background:#070b12;border-bottom:1px solid #1e2a3c;z-index:1}
  #phone-settings h2{margin:0;font-size:1.35em;letter-spacing:.06em;color:#e0f2fe}
  #phone-settings h3{margin:0 0 10px;font-size:1.05em;letter-spacing:.08em;color:#7dd3fc;text-transform:uppercase}
  #phone-settings section{margin:14px 12px;padding:14px;border:1px solid #1e2a3c;border-radius:14px;background:#0a0f18;box-shadow:0 6px 14px rgba(0,0,0,.6),inset 0 1px 0 rgba(255,255,255,.05)}
  #phone-settings button{font:inherit;color:inherit}
  #phone-settings .x{border:1px solid #334155;background:#0f1724;border-radius:10px;padding:8px 14px}
  #phone-settings .grid{display:grid;grid-template-columns:1fr 1fr;gap:8px}
  #phone-settings .opt{display:flex;align-items:center;gap:10px;padding:11px 12px;border:1px solid #1e2a3c;border-radius:11px;background:#0f1724;text-align:left}
  #phone-settings .opt .flag{font-size:1.6em;line-height:1}
  #phone-settings .opt[aria-pressed="true"]{border-color:#00ff88;background:#06261c;color:#bbf7d0;box-shadow:0 0 10px rgba(0,255,136,.35)}
  #phone-settings details{border-top:1px solid #1e2a3c;padding:10px 0}
  #phone-settings details:first-of-type{border-top:0}
  #phone-settings summary{cursor:pointer;color:#e0f2fe;font-weight:700;list-style:none;display:flex;gap:10px}
  #phone-settings summary b{display:inline-flex;align-items:center;justify-content:center;min-width:1.7em;height:1.7em;border-radius:50%;background:#0a3445;color:#a5f3fc}
  #phone-settings details p{margin:8px 0 0 2.5em;color:#cbd5e1}
  #phone-settings .note{margin-top:10px;color:#94a3b8;font-size:.92em}
  #phone-settings .about dt{color:#7dd3fc;font-size:.85em;letter-spacing:.08em;text-transform:uppercase;margin-top:10px}
  #phone-settings .about dd{margin:2px 0 0;color:#e2e8f0;word-break:break-word}
  #phone-settings .brand{font-size:1.25em;letter-spacing:.12em;color:#e0f2fe;font-weight:700}
  html.whispers-ember #phone-settings{background:#0c0d0f}
  html.whispers-ember #phone-settings .top{background:#121316;border-color:#2c2d33}
  html.whispers-ember #phone-settings section{background:#17181b;border-color:#2c2d33}
  html.whispers-ember #phone-settings h3,html.whispers-ember #phone-settings .about dt{color:#ff8a3d}
  html.whispers-ember #phone-settings .opt{background:#1c1d21;border-color:#2c2d33}
  html.whispers-ember #phone-settings .opt[aria-pressed="true"]{border-color:#ff8a3d;background:#2a1810;color:#ffd7b8;box-shadow:0 0 10px rgba(255,122,42,.35)}
  html.whispers-ember #phone-settings summary b{background:#2a1810;color:#ffb27a}
  `;
  const panel = document.createElement('div');
  panel.id = 'phone-settings'; panel.setAttribute('role', 'dialog'); panel.setAttribute('aria-modal', 'true'); panel.dataset.noTranslate = '1';
  document.body.appendChild(panel);
  const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
  let textScale = 1; try { textScale = Number(localStorage.getItem('whispers-textsize')) || 1; } catch (e) { }
  const applyScale = () => { root.style.setProperty('--tz', String(textScale)); setTimeout(fit, 50); };
  const themes = () => [...document.querySelectorAll('[data-skin]')];
  const renderSettings = () => {
    const t = tx();
    const current = themes().find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.skin;
    panel.innerHTML =
      '<div class="top"><h2>⚙ ' + esc(t.title) + '</h2><button type="button" class="x" data-close>✕ ' + esc(t.close) + '</button></div>' +
      '<section><h3>🌐 ' + esc(t.language) + '</h3><div class="grid">' +
        langs.map(l => '<button type="button" class="opt" data-lang="' + l.code + '" aria-pressed="' + (l.code === lang) + '"><span class="flag">' + l.flag + '</span>' + esc(l.name) + '</button>').join('') +
      '</div></section>' +
      '<section><h3>🎨 ' + esc(t.theme) + '</h3><div class="grid">' +
        themes().map(b => '<button type="button" class="opt" data-theme="' + b.dataset.skin + '" aria-pressed="' + (b.dataset.skin === current) + '">' + esc(b.textContent.replace(/^● /, '')) + '</button>').join('') +
      '</div></section>' +
      '<section><h3>🔠 ' + esc(t.textSize) + '</h3><div class="grid" style="grid-template-columns:1fr 1fr 1fr">' +
        [1, 1.15, 1.3].map((v, i) => '<button type="button" class="opt" data-size="' + v + '" aria-pressed="' + (Math.abs(textScale - v) < .01) + '" style="justify-content:center;font-size:' + (0.9 + i * 0.15) + 'em">' + esc(t.sizes[i]) + '</button>').join('') +
      '</div></section>' +
      '<section><h3>📖 ' + esc(t.guide) + '</h3>' +
        t.steps.map((s, i) => '<details' + (i === 0 ? ' open' : '') + '><summary><b>' + (i + 1) + '</b>' + esc(s[0]) + '</summary><p>' + esc(s[1]) + '</p></details>').join('') +
        '<p class="note">💡 ' + esc(t.tip) + '</p></section>' +
      '<section class="about"><h3>ℹ️ ' + esc(t.about) + '</h3>' +
        '<div class="brand">WHISPERS OF THE SPIRIT</div>' +
        '<dl><dt>' + esc(t.version) + '</dt><dd>2.1 · Android</dd>' +
        '<dt>' + esc(t.author) + '</dt><dd>debattistacliff-commits</dd>' +
        '<dt>©</dt><dd>© 2026 debattistacliff-commits. ' + esc(t.rights) + '</dd>' +
        '<dt>' + esc(t.source) + '</dt><dd>github.com/debattistacliff-commits/whispers-of-the-spirit</dd></dl>' +
        '<p class="note">⚠️ ' + esc(t.disclaimer) + '</p><p class="note">' + esc(t.credits) + '</p><p class="note">👻 ' + esc(t.made) + '</p></section>';
  };
  const openSettings = () => { renderSettings(); panel.classList.add('open'); panel.scrollTop = 0; };
  panel.addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return;
    if (b.hasAttribute('data-close')) panel.classList.remove('open');
    else if (b.dataset.lang) setLanguage(b.dataset.lang);
    else if (b.dataset.theme) { document.querySelector('[data-skin="' + b.dataset.theme + '"]')?.click(); setTimeout(renderSettings, 50); }
    else if (b.dataset.size) { textScale = Number(b.dataset.size); try { localStorage.setItem('whispers-textsize', String(textScale)); } catch (err) { } applyScale(); renderSettings(); }
  });
  bar.querySelector('.gear').addEventListener('click', openSettings);
  applyScale();
  observer.observe(document.body, { subtree: true, childList: true, characterData: true });
  setLanguage(lang);

  // ---------- old television switch-on when the app opens ----------
  // A bright dot stretches into a line, the line opens to fill the screen with a flash and a
  // little flicker, then the console appears (about one second).
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
