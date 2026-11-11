// Whispers of the Spirit — SLS camera page (Android build only, page 8 / "SLS" tab).
// The phone camera, a night-vision filter, and Google MediaPipe Pose Landmarker running offline on
// the phone: it draws a stick-figure skeleton over human-like body shapes, with an intensity bar and
// a sensitivity slider. It finds *shapes that look like a body* — it cannot detect or prove spirits.
// The camera is on only while this page is shown; the radio keeps running meanwhile.
(() => {
  if (window.__whispersSlsInstalled) return;
  const page = document.getElementById('sls-page');
  if (!page) return;
  window.__whispersSlsInstalled = true;
  // Phone: the app's secure page address (SlsWebHost). Computer: the page's own address (DesktopWebHost).
  const desktop = document.documentElement.classList.contains('whispers-desktop');
  const BASE = (desktop ? location.origin + '/' : 'https://appassets.androidplatform.net/') + 'sls/';
  const tr = text => (window.__whispersTr ? window.__whispersTr(text) : text);

  const css = `
  #sls-page{display:flex;flex-direction:column;background:#020805;color:#a7f3d0;font-family:"Share Tech Mono","JetBrains Mono",monospace;
    --sl:#4ade80;--sd:#14532d;--sb:#03140a;--sx:#dcfce7}
  #sls-page .hd{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 12px;border-bottom:1px solid #0b2a17}
  #sls-page .ttl{color:var(--sl);letter-spacing:.08em;font-size:calc(15px * var(--tz,1))}
  #sls-page .lamp{font-size:calc(13px * var(--tz,1));color:#64748b}
  #sls-page .lamp.on{color:#ef4444}
  #sls-page .sw{display:flex;align-items:center;gap:7px;font-size:calc(12px * var(--tz,1));color:var(--sl);background:none;border:0;padding:4px 0}
  #sls-page .sw i{width:38px;height:20px;border-radius:10px;background:#04070b;border:1px solid var(--sd);position:relative;display:inline-block}
  #sls-page .sw i::after{content:'';position:absolute;top:2px;left:2px;width:14px;height:14px;border-radius:50%;background:#374151;transition:left .15s}
  #sls-page .sw[aria-checked="true"] i::after{left:20px;background:radial-gradient(circle at 38% 32%,#eafff4,var(--sl) 60%);box-shadow:0 0 8px var(--sl)}
  #sls-page .view{position:relative;flex:1;min-height:0;overflow:hidden;background:#03140a}
  #sls-page video,#sls-page canvas{position:absolute;inset:0;width:100%;height:100%;object-fit:cover}
  #sls-page video,#sls-page canvas.kin{filter:var(--sls-filter,none)}
  #sls-page canvas[hidden],#sls-page video[hidden]{display:none}
  #sls-page .src{display:flex;gap:4px;margin-left:auto;margin-right:12px}
  #sls-page .src[hidden]{display:none}
  #sls-page .src button{font:inherit;font-size:calc(11px * var(--tz,1));letter-spacing:.06em;padding:5px 10px;border-radius:8px;border:1px solid var(--sd);background:var(--sb);color:#94a3b8;cursor:pointer}
  #sls-page .src button[aria-pressed="true"]{color:var(--sl);border-color:var(--sl);box-shadow:0 0 8px var(--sl)}
  #sls-page.front video,#sls-page.front canvas{transform:scaleX(-1)}
  #sls-page .tools{position:absolute;right:8px;top:8px;display:flex;flex-direction:column;gap:8px}
  #sls-page .tools button{width:44px;height:44px;border-radius:50%;border:1px solid var(--sd);background:rgba(2,8,5,.75);color:var(--sl);font-size:19px;
    box-shadow:0 2px 6px rgba(0,0,0,.7)}
  #sls-page .tools button[hidden]{display:none}
  #sls-page .info{position:absolute;left:8px;top:8px;background:rgba(2,8,5,.75);border:1px solid var(--sd);border-radius:8px;padding:5px 8px;
    color:var(--sl);white-space:pre-line;font-size:calc(12px * var(--tz,1));line-height:1.4}
  #sls-page .fname{position:absolute;left:8px;bottom:8px;background:rgba(2,8,5,.75);border:1px solid var(--sd);border-radius:8px;padding:3px 8px;
    color:var(--sl);font-size:calc(11px * var(--tz,1))}
  #sls-page .msg{position:absolute;left:12px;right:60px;bottom:40px;background:rgba(2,8,5,.88);border:1px solid var(--sd);border-radius:10px;padding:10px 12px;
    color:var(--sx);font-size:calc(13px * var(--tz,1))}
  #sls-page .msg[hidden]{display:none}
  #sls-page .fmenu{position:absolute;right:60px;top:8px;display:grid;gap:6px;padding:8px;border-radius:12px;background:rgba(2,8,5,.92);border:1px solid var(--sd);
    box-shadow:0 6px 16px rgba(0,0,0,.8)}
  #sls-page .fmenu[hidden]{display:none}
  #sls-page .fmenu button{display:flex;align-items:center;gap:8px;padding:8px 12px;border-radius:9px;border:1px solid var(--sd);background:var(--sb);
    color:var(--sx);font:inherit;font-size:calc(13px * var(--tz,1));text-align:left;min-width:150px}
  #sls-page .fmenu button[aria-pressed="true"]{border-color:var(--sl);box-shadow:0 0 8px var(--sl);color:var(--sl)}
  #sls-page .meters{padding:8px 12px 6px;display:grid;gap:6px}
  #sls-page .row{display:flex;justify-content:space-between;font-size:calc(12px * var(--tz,1));letter-spacing:.06em}
  #sls-page .row b{color:var(--sl);font-weight:700}
  #sls-page .bar{display:grid;grid-template-columns:repeat(10,1fr);gap:3px}
  #sls-page .bar i{height:10px;border-radius:2px;background:#0b2a17;transition:background .15s}
  #sls-page .bar i.on{background:var(--sl);box-shadow:0 0 6px var(--sl)}
  #sls-page input[type=range]{width:100%;pointer-events:none;-webkit-appearance:none;appearance:none;height:6px;border-radius:3px;margin:8px 0 4px;
    background:linear-gradient(90deg,var(--sl) var(--p,60%),#0b2a17 var(--p,60%));border:1px solid var(--sd)}
  #sls-page input[type=range]::-webkit-slider-thumb{-webkit-appearance:none;width:22px;height:22px;border-radius:50%;border:0;
    background:radial-gradient(circle at 38% 32%,#ffffff,var(--sl) 60%);box-shadow:0 0 10px var(--sl),0 2px 3px #000}
  html.whispers-ember #sls-page input[type=range]{background:linear-gradient(90deg,var(--sl) var(--p,60%),#2a1810 var(--p,60%))}
  #sls-page .honest{padding:0 12px 8px;color:#6b7f74;font-size:calc(11px * var(--tz,1));line-height:1.35}
  #sls-page{position:relative}
  #sls-page .bar.motion i{height:6px}
  #sls-page .bar.motion i.on{background:#38bdf8;box-shadow:0 0 6px #38bdf8}
  #sls-page .toast{position:absolute;left:12px;right:64px;bottom:44px;z-index:4;background:rgba(2,8,5,.92);border:1px solid var(--sl);border-radius:10px;
    padding:8px 12px;color:var(--sx);font-size:calc(13px * var(--tz,1));box-shadow:0 0 12px rgba(0,0,0,.8);transition:opacity .3s}
  #sls-page .toast[hidden]{display:none}
  #sls-page.rec .view{box-shadow:inset 0 0 0 3px #ef4444}
  #sls-page .cap{display:flex;align-items:center;justify-content:center;gap:28px;padding:8px 12px 2px}
  #sls-page .cap button{width:46px;height:46px;border-radius:50%;border:1px solid var(--sd);background:var(--sb);color:var(--sl);font-size:20px}
  #sls-page .cap .recb{width:62px;height:62px;border:3px solid #e5e7eb;background:#020805;position:relative;padding:0}
  #sls-page .cap .recb::after{content:'';position:absolute;inset:9px;border-radius:50%;background:#ef4444;transition:all .2s}
  #sls-page.rec .cap .recb::after{inset:17px;border-radius:5px;box-shadow:0 0 12px #ef4444}
  #sls-page .cap button:disabled{opacity:.4}
  #sls-page .flash{position:absolute;inset:0;background:#fff;opacity:0;pointer-events:none;transition:opacity .35s}
  #sls-page .flash.on{opacity:.85;transition:none}
  #sls-page .hist{position:absolute;inset:0;z-index:5;background:#020805;display:flex;flex-direction:column}
  #sls-page .hist[hidden],#sls-page .player[hidden]{display:none}
  #sls-page .hist .hd button{background:none;border:1px solid var(--sd);border-radius:9px;color:var(--sl);font:inherit;padding:6px 12px}
  #sls-page .hlist{flex:1;overflow-y:auto;padding:8px 10px;display:grid;gap:8px;align-content:start}
  #sls-page .item{display:grid;grid-template-columns:96px 1fr;gap:10px;padding:8px;border:1px solid var(--sd);border-radius:10px;background:var(--sb)}
  #sls-page .item img{width:96px;height:72px;object-fit:cover;border-radius:6px;background:#000}
  #sls-page .item .t{color:var(--sx);font-size:calc(13px * var(--tz,1))}
  #sls-page .item .d{color:var(--sl);font-size:calc(11px * var(--tz,1));margin-top:2px;line-height:1.35}
  #sls-page .item input{width:100%;margin-top:5px;background:#000;border:1px solid var(--sd);border-radius:6px;color:var(--sx);font:inherit;
    font-size:calc(12px * var(--tz,1));padding:4px 6px}
  #sls-page .item .act{grid-column:1/-1;display:flex;gap:8px}
  #sls-page .item .act button{flex:1;padding:7px 4px;border-radius:8px;border:1px solid var(--sd);background:#000;color:var(--sl);font:inherit;font-size:calc(12px * var(--tz,1))}
  #sls-page .item .act button.sure{border-color:#ef4444;color:#fca5a5}
  #sls-page .empty{color:#6b7f74;text-align:center;padding:30px 10px;font-size:calc(13px * var(--tz,1))}
  #sls-page .player{position:absolute;inset:0;z-index:6;background:#000;display:flex;flex-direction:column}
  #sls-page .player video,#sls-page .player img{position:static;flex:1;min-height:0;width:100%;object-fit:contain;filter:none;transform:none}
  #sls-page .player button{margin:10px;padding:10px;border-radius:10px;border:1px solid var(--sd);background:var(--sb);color:var(--sl);font:inherit}
  html.whispers-ember #sls-page .hist{background:#0c0d0f}
  html.whispers-ember #sls-page{background:#0c0d0f;color:#ffd7b8;--sl:#ff8a3d;--sd:#7c2d12;--sb:#1a0f08;--sx:#ffe0c8}
  html.whispers-ember #sls-page .hd{border-color:#2c2d33}
  html.whispers-ember #sls-page .bar i{background:#2a1810}
  html.whispers-ember #sls-page .info,html.whispers-ember #sls-page .fname,html.whispers-ember #sls-page .msg,html.whispers-ember #sls-page .tools button{background:rgba(12,13,15,.8)}
  `;
  const style = document.createElement('style'); style.textContent = css; document.head.appendChild(style);

  // Visual effects for the camera picture. They only change colours/outlines: no heat, no X-ray.
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('width', '0'); svg.setAttribute('height', '0'); svg.style.position = 'absolute';
  const lum = '<feColorMatrix type="matrix" values="0.3 0.59 0.11 0 0  0.3 0.59 0.11 0 0  0.3 0.59 0.11 0 0  0 0 0 1 0"/>';
  svg.innerHTML =
    // heat-style: dark = deep blue, then purple, red, orange, yellow, white for the brightest parts
    '<filter id="sls-thermal" color-interpolation-filters="sRGB">' + lum +
      '<feComponentTransfer><feFuncR type="table" tableValues="0 0.2 0.6 0.95 1 1 1"/><feFuncG type="table" tableValues="0 0 0 0.25 0.6 0.9 1"/>' +
      '<feFuncB type="table" tableValues="0.2 0.55 0.6 0.25 0.05 0.2 0.95"/></feComponentTransfer></filter>' +
    // X-ray: inverted, cold blue-white film with a soft glow
    '<filter id="sls-xray" color-interpolation-filters="sRGB">' + lum +
      '<feComponentTransfer><feFuncR type="table" tableValues="0.85 0.45 0.12 0.02"/><feFuncG type="table" tableValues="0.95 0.65 0.22 0.04"/>' +
      '<feFuncB type="table" tableValues="1 0.9 0.42 0.1"/></feComponentTransfer>' +
      '<feGaussianBlur stdDeviation="1.6" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>' +
    // Spirit edges: only the outlines of shapes glow on a black screen
    '<filter id="sls-edges" color-interpolation-filters="sRGB">' + lum +
      '<feConvolveMatrix order="3" kernelMatrix="-1 -1 -1 -1 8 -1 -1 -1 -1" preserveAlpha="true"/>' +
      '<feComponentTransfer><feFuncR type="linear" slope="2.2"/><feFuncG type="linear" slope="5"/><feFuncB type="linear" slope="5"/></feComponentTransfer>' +
      '<feGaussianBlur stdDeviation="0.8" result="g"/><feMerge><feMergeNode in="g"/><feMergeNode in="g"/></feMerge></filter>';
  document.body.appendChild(svg);
  const FILTERS = [
    { key: 'GREEN', icon: '🟢', css: 'grayscale(1) sepia(1) hue-rotate(70deg) saturate(2.6) brightness(1.05) contrast(1.1)', line: '#4ade80' },
    { key: 'X-RAY', icon: '🦴', css: 'url(#sls-xray) contrast(1.25)', line: '#facc15' },
    { key: 'THERMAL-STYLE', icon: '🔥', css: 'url(#sls-thermal) contrast(1.15)', line: '#e0f2fe' },
    { key: 'UV-STYLE', icon: '🟣', css: 'grayscale(1) sepia(1) hue-rotate(225deg) saturate(3.2) brightness(1.05) contrast(1.25)', line: '#a3e635' },
    { key: 'SPIRIT EDGES', icon: '👻', css: 'url(#sls-edges)', line: '#f472b6' },
    { key: 'EMBER', icon: '🟠', css: 'grayscale(1) sepia(1) hue-rotate(-12deg) saturate(3) brightness(1.05) contrast(1.1)', line: '#ff8a3d' },
    { key: 'PLAIN', icon: '⚪', css: 'none', line: '#4ade80' },
  ];

  page.innerHTML =
    '<div class="hd"><span class="ttl">📷 SLS CAMERA</span><span class="lamp" data-lamp data-no-translate>● …</span>' +
    '<div class="src" data-src hidden data-no-translate></div>' +
    '<button type="button" class="sw" data-detect role="switch" aria-checked="true"><span>DETECTION</span><i></i></button></div>' +
    '<div class="view"><video playsinline muted poster="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"></video><canvas class="kin" hidden></canvas><canvas class="ovl"></canvas>' +
    '<div class="info" data-info data-no-translate></div><div class="fname" data-fname data-no-translate></div>' +
    '<div class="tools"><button type="button" data-flip aria-label="Flip camera">⇄</button><button type="button" data-torch aria-label="Torch" hidden>🔦</button>' +
    '<button type="button" data-filter aria-label="Filter">◐</button><button type="button" data-bell aria-label="Alert sound">🔕</button></div><div class="msg" data-msg hidden></div>' +
    '<div class="fmenu" data-fmenu hidden></div><div class="flash" data-flash></div><div class="toast" data-toast hidden data-no-translate></div></div>' +
    '<div class="cap"><button type="button" data-hist aria-label="History">🕘</button><button type="button" class="recb" data-rec aria-label="Record"></button>' +
    '<button type="button" data-snap aria-label="Snapshot">📸</button></div>' +
    '<div class="meters"><div class="row"><span>SLS INTENSITY</span><b data-score data-no-translate>0%</b></div><div class="bar" data-bar>' + '<i></i>'.repeat(10) + '</div>' +
    '<div class="row"><span>MOTION</span><b data-motion-read data-no-translate>0%</b></div><div class="bar motion" data-motion>' + '<i></i>'.repeat(10) + '</div>' +
    '<div class="row"><span>SENSITIVITY</span><b data-sens-read data-no-translate>60%</b></div>' +
    '<input type="range" min="0" max="100" step="1" value="60" data-sens aria-label="Sensitivity"></div>' +
    '<div class="honest">It finds body-like shapes, not spirits. Filters are visual effects only — no real heat or X-ray.</div>' +
    '<div class="hist" data-histbox hidden data-no-translate><div class="hd"><span class="ttl" data-htitle></span><button type="button" data-hclose></button></div>' +
    '<div class="hlist" data-hlist></div></div>' +
    '<div class="player" data-player hidden data-no-translate><div data-pbody style="flex:1;min-height:0;display:flex"></div><button type="button" data-pclose></button></div>';
  const $ = s => page.querySelector(s);
  const video = $('video'), canvas = $('canvas.ovl'), ctx = canvas.getContext('2d');
  const kcan = $('canvas.kin'), kctx = kcan.getContext('2d');
  const info = $('[data-info]'), msg = $('[data-msg]'), lamp = $('[data-lamp]'), fname = $('[data-fname]');
  const bars = [...page.querySelectorAll('[data-bar] i')], scoreRead = $('[data-score]');
  const sens = $('[data-sens]'), sensRead = $('[data-sens-read]'), detectSw = $('[data-detect]'), torchBtn = $('[data-torch]');
  const say = text => { msg.hidden = !text; msg.textContent = text ? tr(text) : ''; if (text) info.textContent = '—'; };

  // Saved choices
  const load = (k, d) => { try { const v = localStorage.getItem(k); return v === null ? d : v; } catch (e) { return d; } };
  const save = (k, v) => { try { localStorage.setItem(k, String(v)); } catch (e) { } };
  let facing = load('sls-facing', desktop ? 'user' : 'environment'), filterKey = load('sls-filter-key', document.documentElement.classList.contains('whispers-ember') ? 'EMBER' : 'GREEN');
  let detecting = load('sls-detect', '1') === '1';
  // Picture source. Phone: its camera. Computer: the webcam, or a Kinect (infrared or depth) when the app has the Kinect helper.
  let source = desktop ? load('sls-source', 'webcam') : 'webcam';
  const kinect = () => source !== 'webcam';
  sens.value = load('sls-sens', '60');

  let stream = null, active = false, landmarker = null, vision = null, loading = null, engine = '', torchOn = false;
  let lastVideoTime = -1, count = 0, rate = 0, rateStart = 0, figures = 0, best = 0, shown = 0;
  const ember = () => document.documentElement.classList.contains('whispers-ember');

  const filter = () => FILTERS.find(f => f.key === filterKey) || FILTERS[0];
  const fmenu = page.querySelector('[data-fmenu]');
  const renderMenu = () => {
    fmenu.innerHTML = FILTERS.map(f => '<button type="button" data-fkey="' + f.key + '" aria-pressed="' + (f.key === filterKey) + '"><span>' + f.icon + '</span>' + tr(f.key) + '</button>').join('');
  };
  const applyFilter = () => {
    const f = filter();
    page.style.setProperty('--sls-filter', f.css);
    fname.textContent = tr('FILTER') + ': ' + tr(f.key);
  };
  const showSens = () => { sensRead.textContent = sens.value + '%'; sens.style.setProperty('--p', sens.value + '%'); };
  const showDetect = () => detectSw.setAttribute('aria-checked', String(detecting));
  const mirrored = () => !kinect() && facing === 'user';
  const showLamp = () => { lamp.classList.toggle('on', !!stream); lamp.textContent = stream ? '● ' + tr('SCANNING') : '○ ' + tr('CAMERA OFF'); };

  const loadEngine = async () => {
    if (landmarker) return landmarker;
    if (loading) return loading;
    loading = (async () => {
      vision = await import(BASE + 'vision_bundle.mjs');
      const files = { wasmLoaderPath: BASE + 'vision_wasm_internal.js', wasmBinaryPath: BASE + 'vision_wasm_internal.wasm' };
      const options = delegate => ({ baseOptions: { modelAssetPath: BASE + 'pose_landmarker_lite.task', delegate },
        runningMode: 'VIDEO', numPoses: 1, minPoseDetectionConfidence: 0.3, minPosePresenceConfidence: 0.3, minTrackingConfidence: 0.3 });
      try { landmarker = await vision.PoseLandmarker.createFromOptions(files, options('GPU')); engine = 'GPU'; }
      catch (e) { landmarker = await vision.PoseLandmarker.createFromOptions(files, options('CPU')); engine = 'CPU'; }
      return landmarker;
    })();
    try { return await loading; } finally { loading = null; }
  };

  // The current picture and its size (webcam video, or the latest Kinect picture).
  const frame = () => (kinect() ? kcan : video);
  const fw = () => (kinect() ? kcan.width : video.videoWidth), fh = () => (kinect() ? kcan.height : video.videoHeight);
  let kFrame = 0, kSeen = 0, kPeople = [], kPolling = false;
  const kinectApi = (action, args) => fetch(location.origin + '/kinect/' + action + (args ? '?' + new URLSearchParams(args) : '')).then(r => (r.ok ? r.text() : ''), () => '');
  const pollKinect = async () => {
    if (kPolling) return; kPolling = true;
    try {
      while (active && kinect() && stream) {
        const r = await fetch(location.origin + '/kinect/frame?after=' + kFrame).catch(() => null);
        if (!r || r.status !== 200) { await new Promise(ok => setTimeout(ok, 15)); continue; }
        const no = Number(r.headers.get('X-Frame')) || kFrame + 1;
        try { kPeople = JSON.parse(r.headers.get('X-Skeleton') || '{}').people || []; } catch (e) { kPeople = []; }
        const bmp = await createImageBitmap(await r.blob());
        if (kcan.width !== bmp.width || kcan.height !== bmp.height) { kcan.width = bmp.width; kcan.height = bmp.height; }
        kctx.drawImage(bmp, 0, 0); if (bmp.close) bmp.close(); kFrame = no;
      }
    } finally { kPolling = false; }
  };
  const stopCamera = () => {
    if (stream) stream.getTracks().forEach(t => t.stop());
    if (stream && stream.kinect) kinectApi('stop');
    stream = null; video.srcObject = null; torchOn = false; torchBtn.hidden = true;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    showLamp();
  };
  const startCamera = async () => {
    stopCamera();
    page.classList.toggle('front', mirrored());
    video.hidden = kinect(); kcan.hidden = !kinect();
    if (kinect()) {
      const status = await kinectApi('start', { mode: source === 'kinect-depth' ? 'depth' : 'ir' });
      if (status !== 'ready') { const e = new Error(status); e.name = 'KinectError'; throw e; }
      stream = { kinect: true, getTracks: () => [] }; kFrame = 0; kSeen = 0; kPeople = [];
      pollKinect(); showLamp(); return;
    }
    stream = await navigator.mediaDevices.getUserMedia({ audio: false, video: { facingMode: facing, width: { ideal: 640 }, height: { ideal: 480 } } });
    video.srcObject = stream;
    await video.play();
    const track = stream.getVideoTracks()[0];
    torchBtn.hidden = !(track?.getCapabilities?.().torch);
    showLamp();
  };

  // Sensitivity: higher = shows weaker, more doubtful shapes (more false alarms).
  const minScore = () => Math.round(85 - 0.7 * Number(sens.value));     // 85% … 15%
  const jointMin = () => 0.65 - 0.45 * Number(sens.value) / 100;          // 0.65 … 0.20

  // Kinect v1 joints: 0 hip centre, 1 spine, 2 shoulder centre, 3 head, 4-7 left arm, 8-11 right arm, 12-15 left leg, 16-19 right leg.
  const KINECT_BONES = [[0, 1], [1, 2], [2, 3], [2, 4], [4, 5], [5, 6], [6, 7], [2, 8], [8, 9], [9, 10], [10, 11],
    [0, 12], [12, 13], [13, 14], [14, 15], [0, 16], [16, 17], [17, 18], [18, 19]].map(([start, end]) => ({ start, end }));
  const kinectResult = () => ({ landmarks: kPeople.map(joints => joints.map(([x, y, st]) => ({ x, y, visibility: st === 2 ? 1 : st === 1 ? 0.55 : 0 }))) });
  const draw = result => {
    const w = fw(), h = fh();
    if (!w || !h) return;
    if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; }
    ctx.clearRect(0, 0, w, h);
    const line = filter().line;
    const dot = ember() ? '#ffe0c8' : '#ffffff';
    figures = 0; best = 0;
    const jm = jointMin();
    (result?.landmarks || []).forEach((points, n) => {
      const seen = points.filter(p => (p.visibility ?? 1) > jm);
      const score = Math.round(100 * points.reduce((s, p) => s + (p.visibility ?? 1), 0) / points.length);
      best = Math.max(best, score);
      if (seen.length < 6 || score < minScore()) return;
      figures++;
      ctx.lineWidth = Math.max(2, w / 160); ctx.strokeStyle = line; ctx.shadowColor = line; ctx.shadowBlur = 10;
      for (const c of (kinect() ? KINECT_BONES : vision.PoseLandmarker.POSE_CONNECTIONS)) {
        const a = points[c.start], b = points[c.end];
        if ((a.visibility ?? 1) < jm || (b.visibility ?? 1) < jm) continue;
        ctx.beginPath(); ctx.moveTo(a.x * w, a.y * h); ctx.lineTo(b.x * w, b.y * h); ctx.stroke();
      }
      ctx.fillStyle = dot;
      for (const p of seen) { ctx.beginPath(); ctx.arc(p.x * w, p.y * h, Math.max(2.5, w / 220), 0, 7); ctx.fill(); }
      const xs = seen.map(p => p.x * w), ys = seen.map(p => p.y * h);
      const x0 = Math.max(4, Math.min(...xs) - 14), x1 = Math.min(w - 4, Math.max(...xs) + 14), y0 = Math.max(4, Math.min(...ys) - 18), y1 = Math.min(h - 4, Math.max(...ys) + 14);
      const k = Math.max(14, (x1 - x0) / 6);
      ctx.shadowBlur = 0; ctx.lineWidth = Math.max(2, w / 200);
      for (const [x, y, dx, dy] of [[x0, y0, 1, 1], [x1, y0, -1, 1], [x0, y1, 1, -1], [x1, y1, -1, -1]]) {
        ctx.beginPath(); ctx.moveTo(x, y + dy * k); ctx.lineTo(x, y); ctx.lineTo(x + dx * k, y); ctx.stroke();
      }
      const label = `${tr('FIGURE')} ${n + 1} · ${score}%`, fs = Math.round(w / 28);
      ctx.save(); if (mirrored()) { ctx.translate(w, 0); ctx.scale(-1, 1); }
      ctx.font = `${fs}px monospace`; ctx.fillStyle = line;
      const tw = ctx.measureText(label).width;
      const lx = Math.min(Math.max(8, mirrored() ? w - x1 : x0), w - tw - 8);
      ctx.fillText(label, lx, Math.min(Math.max(fs + 6, y0 - 6), h - 8));
      ctx.restore();
    });
  };

  const showMeters = () => {
    shown = shown * 0.6 + (figures ? best : 0) * 0.4;           // smooth the bar
    const lit = Math.round(shown / 10);
    bars.forEach((b, i) => b.classList.toggle('on', i < lit));
    scoreRead.textContent = Math.round(shown) + '%';
    const lines = [`${tr('FIGURES')} ${figures}`, `${tr('SPEED')} ${rate} / s · ${kinect() ? 'KINECT' : engine || '…'}`, source === 'kinect-ir' ? 'KINECT · ' + tr('INFRARED') : source === 'kinect-depth' ? 'KINECT · ' + tr('DEPTH') :
      desktop ? tr('WEBCAM') : facing === 'user' ? tr('FRONT CAMERA') : tr('BACK CAMERA')];
    const radioLine = radioRec ? `📻 ${tr('RADIO REC')} · ${tr('MARKERS')} ${radioMarks}` : '';
    info.textContent = (detecting ? lines.join('\n') : tr('DETECTION') + ': ' + tr('OFF')) + (radioLine ? '\n' + radioLine : '');
  };

  // ---- Phase 3: motion, figure events (radio marker, alert sound) ----
  // Motion: compares each small grey picture with the previous one. Works even when no figure is found.
  const small = document.createElement('canvas'); small.width = 48; small.height = 36;
  const sctx = small.getContext('2d', { willReadFrequently: true });
  const motionBars = [...page.querySelectorAll('[data-motion] i')], motionRead = $('[data-motion-read]');
  let lastGrey = null, motion = 0, motionTick = 0;
  const motionStep = () => {
    sctx.drawImage(frame(), 0, 0, 48, 36);
    const px = sctx.getImageData(0, 0, 48, 36).data, grey = new Uint8Array(48 * 36);
    let diff = 0;
    for (let i = 0, j = 0; j < grey.length; i += 4, j++) {
      grey[j] = (px[i] * 77 + px[i + 1] * 150 + px[i + 2] * 29) >> 8;
      if (lastGrey) { const d = Math.abs(grey[j] - lastGrey[j]); if (d > 12) diff += d; }
    }
    const level = lastGrey ? Math.min(100, Math.round(diff / grey.length * 1.8)) : 0;
    lastGrey = grey;
    motion = motion * 0.6 + level * 0.4;
    const lit = Math.round(motion / 10);
    motionBars.forEach((b, i) => b.classList.toggle('on', i < lit));
    motionRead.textContent = Math.round(motion) + '%';
  };

  // Radio: is a radio recording running? (read from the app's regular state update)
  let radioRec = false, radioMarks = 0;
  const previousUpdate = window.whispersNativeUpdate;
  window.whispersNativeUpdate = state => {
    try { previousUpdate?.(state); } catch (e) { }
    const now = state?.record === 'RECORDING';
    if (now && !radioRec) radioMarks = 0;
    radioRec = now;
  };
  const sendApp = (action, value) => {
    const url = 'whispers://' + action + '?value=' + encodeURIComponent(value);
    if (window.__whispersSend) window.__whispersSend(url); else window.location.href = url;
  };
  const toast = $('[data-toast]');
  let toastTimer = 0;
  const showToast = text => { toast.textContent = text; toast.hidden = false; clearTimeout(toastTimer); toastTimer = setTimeout(() => { toast.hidden = true; }, 3000); };
  window.__whispersSlsMarked = seconds => {
    if (!(seconds >= 0)) return;
    radioMarks++;
    showToast('📻 ' + tr('Marker on the radio recording at') + ' ' + seconds.toFixed(1) + ' s');
  };

  // Alert sound (off by default): a short soft double beep when a figure appears.
  const bellBtn = $('[data-bell]');
  let bell = load('sls-bell', '0') === '1', audio = null;
  const showBell = () => { bellBtn.textContent = bell ? '🔔' : '🔕'; bellBtn.style.color = bell ? '#fde047' : ''; };
  const beep = () => {
    try {
      audio = audio || new AudioContext();
      const t = audio.currentTime;
      [[880, 0], [660, 0.16]].forEach(([f, at]) => {
        const o = audio.createOscillator(), g = audio.createGain();
        o.frequency.value = f; o.type = 'sine';
        g.gain.setValueAtTime(0.0001, t + at); g.gain.exponentialRampToValueAtTime(0.18, t + at + 0.02); g.gain.exponentialRampToValueAtTime(0.0001, t + at + 0.14);
        o.connect(g).connect(audio.destination); o.start(t + at); o.stop(t + at + 0.15);
      });
    } catch (e) { }
  };
  bellBtn.addEventListener('click', () => { bell = !bell; save('sls-bell', bell ? '1' : '0'); showBell(); if (bell) beep(); });

  // A figure "appears" when it is seen in 3 frames in a row after none; one event at most every 5 s.
  let seenFrames = 0, figureOn = false, lastEvent = -1e9;
  const figureEvents = now => {
    if (figures > 0) seenFrames++; else { seenFrames = 0; figureOn = false; }
    if (figureOn || seenFrames < 3) return;
    figureOn = true;
    if (now - lastEvent < 5000) return;
    lastEvent = now;
    if (bell) beep();
    if (rec) rec.markers.push({ t: Math.round((now - rec.started) / 100) / 10, note: `${tr('FIGURE')} ${best}%` });
    if (radioRec) sendApp('slsmark', best + '|' + figures);
  };

  const loop = () => {
    if (!active) return;
    const fresh = kinect() ? stream && kFrame !== kSeen && kcan.width > 0 : stream && video.readyState >= 2 && video.currentTime !== lastVideoTime;
    if (fresh) {
      lastVideoTime = video.currentTime; kSeen = kFrame;
      const now = performance.now();
      if (detecting && kinect()) { draw(kinectResult()); count++; }
      else if (detecting && landmarker) {
        try { draw(landmarker.detectForVideo(video, now)); count++; }
        catch (e) { say('Detection error: ' + e.message); }
      } else { figures = 0; best = 0; ctx.clearRect(0, 0, canvas.width, canvas.height); }
      if (now - rateStart >= 1000) { rate = Math.round(count * 1000 / (now - rateStart)); count = 0; rateStart = now; }
      if (++motionTick % 3 === 0) { try { motionStep(); } catch (e) { } }
      if (detecting) figureEvents(now);
      showMeters();
      if (rec) recordFrame();
    }
    requestAnimationFrame(loop);
  };

  // ---- Recording, snapshots and History (saved only on this phone, in the app's own folder) ----
  const store = window.WhispersSls || null;
  const FILES = BASE.replace(/sls\/$/, 'sls-files/');
  const comp = document.createElement('canvas'), cctx = comp.getContext('2d');
  const pad = n => String(n).padStart(2, '0');
  const stampTime = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
  let rec = null;   // { name, recorder, started, peak, figuresMax, queue, thumbScore, lastThumb }

  // One picture = filtered camera + skeleton overlay + date/time stamp (what the screen shows).
  const compose = () => {
    const w = fw(), h = fh();
    if (!w || !h) return false;
    if (comp.width !== w || comp.height !== h) { comp.width = w; comp.height = h; }
    cctx.save();
    if (mirrored()) { cctx.translate(w, 0); cctx.scale(-1, 1); }
    cctx.filter = filter().css;
    cctx.drawImage(frame(), 0, 0, w, h);
    cctx.filter = 'none';
    if (detecting) cctx.drawImage(canvas, 0, 0, w, h);
    cctx.restore();
    const fs = Math.max(11, Math.round(w / 40));
    cctx.fillStyle = 'rgba(0,0,0,.55)'; cctx.fillRect(0, h - fs - 12, w, fs + 12);
    cctx.font = `${fs}px monospace`; cctx.fillStyle = filter().line;
    cctx.fillText(`WHISPERS OF THE SPIRIT · SLS · ${stampTime(new Date())}`, 8, h - 8);
    if (rec) {
      cctx.fillStyle = '#ef4444'; cctx.beginPath(); cctx.arc(w - fs - 6, fs + 8, fs / 2, 0, 7); cctx.fill();
      cctx.font = `bold ${fs}px monospace`; cctx.fillText('REC', w - fs * 4 - 10, fs + 13);
    }
    return true;
  };

  const blobToBase64 = blob => new Promise((ok, fail) => {
    const r = new FileReader();
    r.onload = () => ok(String(r.result).split(',')[1] || '');
    r.onerror = () => fail(r.error);
    r.readAsDataURL(blob);
  });
  const jpeg = q => comp.toDataURL('image/jpeg', q).split(',')[1];
  const flash = () => { const f = $('[data-flash]'); f.classList.add('on'); setTimeout(() => f.classList.remove('on'), 60); };
  const recBtn = $('[data-rec]'), snapBtn = $('[data-snap]'), histBtn = $('[data-hist]');
  const mmss = s => `${pad(Math.floor(s / 60))}:${pad(Math.floor(s % 60))}`;
  const MAX_SECONDS = 600;

  const recordFrame = () => {
    if (!compose()) return;
    const now = performance.now(), secs = (now - rec.started) / 1000;
    if (figures > rec.figuresMax) rec.figuresMax = figures;
    const score = figures ? best : 0;
    if (score > rec.peak) rec.peak = score;
    // Thumbnail = the strongest moment (checked at most once a second).
    if (!rec.thumb || (score > rec.thumbScore && now - rec.lastThumb > 1000)) { rec.thumb = jpeg(0.7); rec.thumbScore = score; rec.lastThumb = now; }
    lamp.textContent = '● REC ' + mmss(secs);
    if (secs >= MAX_SECONDS) stopRecording();
  };

  const startRecording = () => {
    if (rec || !stream || !store || !window.MediaRecorder) { if (!store) say('Saving is not available.'); return; }
    if (!compose()) return;
    const type = ['video/webm;codecs=vp8', 'video/webm'].find(t => MediaRecorder.isTypeSupported(t));
    if (!type) { say('Video recording is not supported on this phone.'); return; }
    const name = store.begin();
    const recorder = new MediaRecorder(comp.captureStream(15), { mimeType: type, videoBitsPerSecond: 1500000 });
    rec = { name, recorder, started: performance.now(), when: new Date(), peak: 0, figuresMax: 0, queue: Promise.resolve(), thumb: '', thumbScore: -1, lastThumb: 0, ok: true, markers: [], radio: radioRec };
    const r = rec;
    recorder.ondataavailable = e => {
      if (!e.data || !e.data.size) return;
      r.queue = r.queue.then(() => blobToBase64(e.data)).then(b64 => { if (!store.append(r.name, 'webm', b64)) r.ok = false; }).catch(() => { r.ok = false; });
    };
    recorder.onstop = () => {
      r.queue.then(() => {
        if (r.thumb) store.append(r.name, 'jpg', r.thumb);
        store.finish(r.name, JSON.stringify({ kind: 'video', time: r.when.toISOString(), durationS: Math.round((performance.now() - r.started) / 1000),
          peak: r.peak, figuresMax: r.figuresMax, filter: filterKey, camera: kinect() ? source : desktop ? 'webcam' : facing === 'user' ? 'front' : 'back', place: '',
          markers: r.markers.slice(0, 200), radio: r.radio || radioRec }));
        say(r.ok ? 'Video saved in History.' : 'The video could not be saved completely.');
        setTimeout(() => { if (msg.textContent === tr('Video saved in History.')) say(''); }, 2500);
      });
    };
    recorder.start(1000);
    page.classList.add('rec'); recBtn.setAttribute('aria-label', 'Stop');
  };
  const stopRecording = () => {
    if (!rec) return;
    const r = rec; rec = null;
    try { r.recorder.stop(); } catch (e) { }
    page.classList.remove('rec'); recBtn.setAttribute('aria-label', 'Record');
    showLamp();
  };

  const snapshot = () => {
    if (!stream || !store) { if (!store) say('Saving is not available.'); return; }
    if (!compose()) return;
    const name = store.begin();
    if (!store.append(name, 'jpg', jpeg(0.92))) { say('The picture could not be saved.'); return; }
    store.finish(name, JSON.stringify({ kind: 'photo', time: new Date().toISOString(), durationS: 0, peak: figures ? best : 0, figuresMax: figures,
      filter: filterKey, camera: kinect() ? source : desktop ? 'webcam' : facing === 'user' ? 'front' : 'back', place: '' }));
    flash();
  };

  // History screen
  const histBox = $('[data-histbox]'), hlist = $('[data-hlist]'), player = $('[data-player]'), pbody = $('[data-pbody]');
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
  const SAFE = /^SLS-\d{8}-\d{6}(-\d+)?$/;
  const renderHistory = () => {
    $('[data-htitle]').textContent = '🕘 ' + tr('HISTORY');
    $('[data-hclose]').textContent = tr('BACK');
    let items = [];
    try { items = JSON.parse(store ? store.list() : '[]'); } catch (e) { }
    items = items.filter(it => SAFE.test(it.name || ''));
    if (!items.length) { hlist.innerHTML = '<div class="empty">' + esc(tr('Nothing saved yet. Use ⏺ to record or 📸 for a picture.')) + '</div>'; return; }
    const stamp = Date.now();
    hlist.innerHTML = items.map(it => {
      const d = new Date(it.time), video = it.kind === 'video';
      const facts = [video ? '🎥 ' + tr('VIDEO') + ' ' + mmss(it.durationS || 0) : '📸 ' + tr('PICTURE'),
        tr('PEAK') + ' ' + (it.peak || 0) + '%', tr('FIGURES') + ' ' + (it.figuresMax || 0), tr('FILTER') + ': ' + tr(it.filter || '')]
        .concat(it.markers?.length ? [tr('MARKERS') + ' ' + it.markers.length] : [], it.radio ? ['📻 ' + tr('RADIO REC')] : []).join(' · ');
      return '<div class="item" data-name="' + it.name + '"><img alt="" src="' + FILES + it.name + '.jpg?t=' + stamp + '">' +
        '<div><div class="t">' + esc(isNaN(d) ? it.name : stampTime(d)) + '</div><div class="d">' + esc(facts) + '</div>' +
        '<input type="text" maxlength="60" data-place placeholder="' + esc(tr('Place name…')) + '" value="' + esc(it.place) + '"></div>' +
        '<div class="act"><button type="button" data-play data-kind="' + (video ? 'webm' : 'jpg') + '">▶ ' + esc(tr('OPEN')) + '</button>' +
        '<button type="button" data-share>' + (desktop ? '📂 ' + esc(tr('SHOW FILE')) : '↗ ' + esc(tr('SHARE'))) + '</button><button type="button" data-del>🗑 ' + esc(tr('DELETE')) + '</button></div></div>';
    }).join('');
  };
  const openHistory = () => { stopRecording(); stop(); renderHistory(); histBox.hidden = false; };
  const closeHistory = () => { closePlayer(); histBox.hidden = true; if (canRun()) start(); };
  let playUrl = '';
  const closePlayer = () => { player.hidden = true; pbody.innerHTML = ''; if (playUrl) URL.revokeObjectURL(playUrl); playUrl = ''; };
  const play = async (name, kind) => {
    $('[data-pclose]').textContent = tr('CLOSE');
    player.hidden = false;
    if (kind !== 'webm') { pbody.innerHTML = '<img alt="" src="' + FILES + name + '.jpg">'; return; }
    // The WebView video player asks for parts of a file; load the whole video first and play it from memory.
    pbody.innerHTML = '<div class="empty" style="flex:1">…</div>';
    try {
      const blob = await (await fetch(FILES + name + '.webm')).blob();
      if (player.hidden) return;
      playUrl = URL.createObjectURL(blob);
      pbody.innerHTML = '<video controls autoplay playsinline poster="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"></video>';
      pbody.querySelector('video').src = playUrl;
    } catch (e) { pbody.innerHTML = '<div class="empty" style="flex:1">' + esc(tr('The video could not be opened.')) + '</div>'; }
  };
  hlist.addEventListener('click', e => {
    const item = e.target.closest('.item'); if (!item) return;
    const name = item.dataset.name;
    const b = e.target.closest('button'); if (!b) return;
    if (b.hasAttribute('data-play')) play(name, b.dataset.kind);
    else if (b.hasAttribute('data-share')) store?.share(name);
    else if (b.hasAttribute('data-del')) {
      if (!b.classList.contains('sure')) { b.classList.add('sure'); b.textContent = '🗑 ' + tr('TAP AGAIN'); setTimeout(() => { if (b.isConnected) { b.classList.remove('sure'); b.textContent = '🗑 ' + tr('DELETE'); } }, 3000); return; }
      store?.remove(name); renderHistory();
    }
  });
  hlist.addEventListener('change', e => {
    const input = e.target.closest('[data-place]'); if (!input) return;
    store?.rename(input.closest('.item').dataset.name, input.value);
  });
  $('[data-hclose]').addEventListener('click', closeHistory);
  $('[data-pclose]').addEventListener('click', closePlayer);
  recBtn.addEventListener('click', () => (rec ? stopRecording() : startRecording()));
  snapBtn.addEventListener('click', snapshot);
  histBtn.addEventListener('click', openHistory);
  if (!store) { recBtn.disabled = true; snapBtn.disabled = true; histBtn.disabled = true; }

  const start = async () => {
    if (active) return;
    active = true; say(''); showLamp(); info.textContent = tr('Starting the camera…');
    seenFrames = 0; figureOn = false;   // each opening starts fresh: a figure already in view counts as new
    if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) { say('The camera is not available on this screen.'); return; }
    try { await startCamera(); }
    catch (e) {
      say(e.name === 'KinectError' ? (e.message === 'no-kinect' ? 'No Kinect found. Plug in the Kinect with its power adapter, then choose it again.' :
        'The Kinect could not start. Install the Kinect for Windows Runtime 1.8 and plug in the Kinect with its power adapter.') :
        e.name === 'NotAllowedError' ? (desktop ? 'Camera permission was refused. Allow the camera for this app in your computer’s privacy settings, then open the SLS camera again.' :
        'Camera permission was refused. Allow the camera for Whispers of the Spirit in Android Settings → Apps.') : 'The camera could not start.');
      return;
    }
    if (!active) { stopCamera(); return; }   // page left while the camera was starting
    if (!kinect()) {
      info.textContent = tr('Loading the body-shape engine…');
      try { await loadEngine(); }
      catch (e) { say('The body-shape engine could not load.'); }
    }
    rateStart = performance.now(); count = 0; lastVideoTime = -1;
    requestAnimationFrame(loop);
  };
  const stop = () => { stopRecording(); active = false; stopCamera(); };

  // Camera on only while the SLS page is shown (and the app is visible); a recording is saved when leaving.
  let onPage = false;
  const canRun = () => onPage && !document.hidden && histBox.hidden;
  window.addEventListener('whispers-page', e => { onPage = e.detail.section === 'sls'; if (canRun()) start(); else stop(); });
  document.addEventListener('visibilitychange', () => { if (canRun()) start(); else stop(); });

  // Camera choice on the computer: shown only when the app has the Kinect helper.
  const srcBox = $('[data-src]');
  const SOURCES = [['webcam', 'WEBCAM'], ['kinect-ir', 'KINECT INFRARED'], ['kinect-depth', 'KINECT DEPTH']];
  const renderSources = () => { srcBox.innerHTML = SOURCES.map(([k, l]) => '<button type="button" data-skey="' + k + '" aria-pressed="' + (k === source) + '">' + tr(l) + '</button>').join(''); };
  if (desktop && location.protocol === 'https:') kinectApi('status').then(t => {
    if (t !== 'helper') { if (kinect()) source = 'webcam'; return; }
    renderSources(); srcBox.hidden = false;
  });
  srcBox.addEventListener('click', async e => {
    const b = e.target.closest('[data-skey]'); if (!b || b.dataset.skey === source) return;
    source = b.dataset.skey; save('sls-source', source); renderSources();
    if (source === 'kinect-depth') { filterKey = 'PLAIN'; applyFilter(); renderMenu(); }
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (active) { stopRecording(); active = false; stopCamera(); await new Promise(ok => setTimeout(ok, 300)); start(); }
  });

  // Controls
  $('[data-flip]').addEventListener('click', async () => {
    facing = facing === 'user' ? 'environment' : 'user'; save('sls-facing', facing);
    if (active) { try { await startCamera(); lastVideoTime = -1; } catch (e) { say('The camera could not start.'); } }
  });
  $('[data-filter]').addEventListener('click', () => { renderMenu(); fmenu.hidden = !fmenu.hidden; });
  fmenu.addEventListener('click', e => {
    const b = e.target.closest('[data-fkey]'); if (!b) return;
    filterKey = b.dataset.fkey; save('sls-filter-key', filterKey); applyFilter(); renderMenu(); fmenu.hidden = true;
  });
  torchBtn.addEventListener('click', async () => {
    const track = stream?.getVideoTracks()[0]; if (!track) return;
    torchOn = !torchOn;
    try { await track.applyConstraints({ advanced: [{ torch: torchOn }] }); } catch (e) { torchOn = false; }
    torchBtn.style.color = torchOn ? '#fde047' : '';
  });
  detectSw.addEventListener('click', () => { detecting = !detecting; save('sls-detect', detecting ? '1' : '0'); showDetect(); showMeters(); });
  sens.addEventListener('input', () => { showSens(); save('sls-sens', sens.value); });

  applyFilter(); showSens(); showDetect(); showLamp(); showMeters(); showBell();
  // Re-label on language change (texts in data-no-translate parts are built with tr()).
  new MutationObserver(() => { applyFilter(); if (!rec) showLamp(); if (!histBox.hidden) renderHistory(); if (!srcBox.hidden) renderSources(); }).observe(document.documentElement, { attributes: true, attributeFilter: ['lang'] });
})();
