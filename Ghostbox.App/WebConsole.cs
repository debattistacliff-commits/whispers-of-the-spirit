using System.Reflection;

namespace Ghostbox.App;

public partial class MainPage
{
    WebView? consoleWebView;

    View BuildWebConsole()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Whispers.Console.html")
            ?? throw new InvalidOperationException("The embedded console design is missing.");
        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();
        // Styles and fonts are bundled so the console looks the same without an internet connection.
        html = html.Replace("/*WHISPERS_OFFLINE_CSS*/", Resource("Whispers.Console.css"));
        // Live wiring and the Review deck ship inside the page but install only after the bridge,
        // which locates the original controls by position.
        html = html.Replace("</body>", "<script>window.__installWhispersLive=function(){" + Resource("Whispers.ConsoleLive.js") +
            "\n};window.__installWhispersDeck=function(){" + Resource("Whispers.ReviewDeck.js") + "\n};</script></body>");
        // The "Material" skin is embedded only in the Android build; other builds skip it.
        string? phoneSkin = OptionalResource("Whispers.PhoneMaterial.js");
        if (phoneSkin is not null) phoneSkin = (OptionalResource("Whispers.PhoneI18n.js") ?? "") + "\n" + phoneSkin + "\n" + (OptionalResource("Whispers.PhoneSls.js") ?? "");
        // Windows: the same extras in the classic layout (Settings drawer, skins, SLS camera window).
        else if (OptionalResource("Whispers.DesktopShell.js") is { } desktopShell)
            phoneSkin = (OptionalResource("Whispers.PhoneI18n.js") ?? "") + "\n" + desktopShell + "\n" + (OptionalResource("Whispers.PhoneSls.js") ?? "");
        if (phoneSkin is not null)
            html = html.Replace("</body>", "<script>window.__installWhispersPhone=function(){" + phoneSkin + "\n};</script></body>");
        consoleWebView = new WebView
        {
#if ANDROID
            // Secure page address so the SLS camera may use the phone camera (see SlsWebHost).
            Source = new HtmlWebViewSource { Html = html, BaseUrl = SlsWebHost.BaseUrl },
#elif WINDOWS
            // Loaded by DesktopWebHost from a secure page address, so the SLS camera may use the webcam.
            StyleId = DesktopWebHost.Prepare(html),
#else
            Source = new HtmlWebViewSource { Html = html },
#endif
            BackgroundColor = Color.FromArgb("020508")
        };
        consoleWebView.Navigating += ConsoleNavigating;
        consoleWebView.Navigated += async (_, _) =>
        {
            try { await consoleWebView.EvaluateJavaScriptAsync(ConsoleBridgeScript); }
            catch (Exception ex) { ShowError("Console bridge: " + ex.Message); }
            try { await consoleWebView.EvaluateJavaScriptAsync("window.__installWhispersLive && window.__installWhispersLive()"); }
            catch (Exception ex) { ShowError("Console wiring: " + ex.Message); }
            try { await consoleWebView.EvaluateJavaScriptAsync("window.__installWhispersDeck && window.__installWhispersDeck()"); }
            catch (Exception ex) { ShowError("Review deck: " + ex.Message); }
            if (phoneSkin is not null)
                try { await consoleWebView.EvaluateJavaScriptAsync("window.__installWhispersPhone && window.__installWhispersPhone()"); }
                catch (Exception ex) { ShowError("Material skin: " + ex.Message); }
        };


        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += async (_, _) => await PushConsoleStateAsync();
        // Smooth Review deck cursor while review audio plays.
        var playhead = Dispatcher.CreateTimer();
        playhead.Interval = TimeSpan.FromMilliseconds(100);
        playhead.Tick += async (_, _) =>
        {
            if (consoleWebView is null) return;
            try
            {
                if (workPlaybackCancellation is not null)
                    await consoleWebView.EvaluateJavaScriptAsync($"window.whispersDeckPlayhead?.({F(DeckPlayheadNow())})");
                // Live analyser data, about ten times a second, only while receiver audio is arriving.
                if (cancellation is not null && lastMeasurement is { } m && (DateTime.UtcNow - lastMeasurementAt).TotalSeconds < 2)
                    await consoleWebView.EvaluateJavaScriptAsync(LiveJson(m));
            }
            catch { }
        };
        Loaded += (_, _) => { timer.Start(); playhead.Start(); };
        Unloaded += (_, _) => { timer.Stop(); playhead.Stop(); };
        return consoleWebView;
    }

    async void ConsoleNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("whispers://", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;
        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri)) return;
        try
        {
            var action = uri.Host.ToLowerInvariant();
            var query = ParseConsoleQuery(uri.Query);
            if (await HandleDeckCommandAsync(action, query)) { await PushConsoleStateAsync(); return; }
            switch (action)
            {
                case "scanreceivers": await LoadRemoteReceiversAsync(); break;
                case "start":
                    if (cancellation is not null) StopScanning();
                    else if (start.IsEnabled)
                    {
                        var settings = System.Text.Json.JsonSerializer.Deserialize<string[]>(query.GetValueOrDefault("value", "[]"));
                        if (settings is not { Length: 12 } || !int.TryParse(settings[0], out var source) || source is < 0 or > 2)
                            throw new ArgumentException("Invalid radio settings.");
                        sourcePicker.SelectedIndex = source;
                        demo.IsToggled = settings[10] == "__demo__";
                        var modes = source == 2 ? Ghostbox.Core.ScanSettings.Modes : Ghostbox.Core.RemoteSdr.Modes;
                        int modeIndex = Array.IndexOf(modes, settings[5]);
                        int directionIndex = Array.IndexOf(Enum.GetNames<Ghostbox.Core.ScanPattern>(), settings[6]);
                        if (modeIndex < 0 || directionIndex < 0) throw new ArgumentException("Unsupported mode or direction.");
                        if (source == 2)
                        {
                            minimum.Text = settings[1]; maximum.Text = settings[2]; step.Text = settings[3]; dwell.Text = settings[4];
                            mode.SelectedIndex = modeIndex; pattern.SelectedIndex = directionIndex;
                            host.Text = settings[7]; port.Text = settings[8]; audioPort.Text = settings[9]; audioEnabled.IsToggled = true;
                        }
                        else if (source == 0)
                        {
                            remoteMin.Text = settings[1]; remoteMax.Text = settings[2]; remoteStep.Text = settings[3]; remoteDwell.Text = settings[4];
                            remoteMode.SelectedIndex = modeIndex; remotePattern.SelectedIndex = directionIndex;
                            if (int.TryParse(settings[11], out var node) && node >= 0 && node < remoteReceivers.Length)
                                remoteReceiverPicker.SelectedIndex = node;
                        }
                        else
                        {
                            customStream.Text = settings[10];
                            stationPattern.SelectedIndex = directionIndex;
                        }
                        StartClicked(null, EventArgs.Empty);
                    }
                    break;
                case "record": RecordRadio(null, EventArgs.Empty); break;
                case "slsmark":
                    double markedAt = MarkSlsFigure(query.GetValueOrDefault("value", ""));
                    await consoleWebView.EvaluateJavaScriptAsync($"window.__whispersSlsMarked?.({markedAt.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)})");
                    return;
                case "speed":
                    if (double.TryParse(query.GetValueOrDefault("value"), System.Globalization.CultureInfo.InvariantCulture, out var speed))
                        Volatile.Write(ref replaySpeed, Math.Clamp(speed, .1, 1));
                    break;
                case "save": SaveConversation(null, EventArgs.Empty); break;
                case "import":
                    var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Open MP3, WAV or FLAC" });
                    if (file is not null) await LoadReviewAsync(file.FullPath);
                    break;
                case "take":
                    if (int.TryParse(query.GetValueOrDefault("value"), out var take) && take >= 0 && take < savedPaths.Length)
                        await LoadReviewAsync(savedPaths[take]);
                    break;
                case "play": PlayWorkbench(); break;
                case "pause": if (workPlaybackCancellation is not null) workPaused = !workPaused; break;
                case "mp3": exportFormat.SelectedIndex = 1; await ExportWorkAsync(true); break;
                case "mic": RecordQuestion(null, EventArgs.Empty); break;
                case "noise": reduceNoise.IsToggled = !reduceNoise.IsToggled; break;
                case "echo": workControls["echo"].Enabled.IsToggled = !workControls["echo"].Enabled.IsToggled; break;
                case "denoiselevel":
                    if (int.TryParse(query.GetValueOrDefault("value"), out var strength))
                        noiseReduction.Value = Math.Clamp(strength, 0, 100) / 100d;
                    break;
                case "filtersweep":
                    var filter = workControls["sweep"].Enabled;
                    filter.IsToggled = !filter.IsToggled;
                    break;
                case "effect":
                    var parts = query.GetValueOrDefault("value", "").Split(':');
                    if (parts.Length == 2 && parts[0] is ("echo" or "delay" or "feedback" or "reverb" or "chop" or "sweep")
                        && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amount)
                        && double.IsFinite(amount))
                    {
                        var effect = workControls[parts[0]];
                        effect.Amount.Value = Math.Clamp(amount, effect.Amount.Minimum, effect.Amount.Maximum);
                        // Echo delay always has a value; the other effects switch off at zero.
                        effect.Enabled.IsToggled = parts[0] == "delay" || amount > 0;
                    }
                    break;
                case "reverb":
                    if (int.TryParse(query.GetValueOrDefault("value"), out var wet))
                    {
                        var effect = workControls["reverb"];
                        effect.Amount.Value = Math.Clamp(wet, 0, 80) / 100d;
                        effect.Enabled.IsToggled = wet > 0;
                    }
                    break;
                case "volume":
                    if (int.TryParse(query.GetValueOrDefault("value"), out var level)) volume.Value = Math.Clamp(level, 0, 100) / 100d;
                    break;
                case "step": remoteStep.Text = query.GetValueOrDefault("value", "9k"); break;
                case "dwell": remoteDwell.Text = query.GetValueOrDefault("value", "250"); break;
                case "minimum": remoteMin.Text = query.GetValueOrDefault("value", remoteMin.Text); break;
                case "maximum": remoteMax.Text = query.GetValueOrDefault("value", remoteMax.Text); break;
                case "mode":
                    var modeName = query.GetValueOrDefault("value", "AM");
                    remoteMode.SelectedIndex = Math.Max(0, Array.IndexOf(Ghostbox.Core.RemoteSdr.Modes, modeName));
                    break;
                case "direction":
                    var direction = query.GetValueOrDefault("value", "Bounce");
                    remotePattern.SelectedIndex = Math.Max(0, Array.IndexOf(Enum.GetNames<Ghostbox.Core.ScanPattern>(), direction));
                    break;
                case "receiver":
                    if (int.TryParse(query.GetValueOrDefault("value"), out var receiverIndex) && receiverIndex >= 0 && receiverIndex < remoteReceivers.Length)
                        remoteReceiverPicker.SelectedIndex = receiverIndex;
                    break;
            }
            await PushConsoleStateAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    async Task PushConsoleStateAsync()
    {
        if (consoleWebView is null) return;
        static string Js(string? value) => System.Text.Json.JsonSerializer.Serialize(value ?? "");
        var receiverState = cancellation is null ? "RECEIVER IDLE" : demo.IsToggled ? "DEMO · SYNTHETIC" : "RECEIVER ACTIVE";
        var recordingState = captureCancellation is not null ? "RECORDING" : microphoneCancellation is not null ? "MIC RECORDING" : "REC EVP";
        var script = $"window.whispersNativeUpdate?.({{frequency:{Js(frequency.Text)},receiver:{Js(receiverState)},nodes:{System.Text.Json.JsonSerializer.Serialize(remoteReceivers.Select(r => r.ToString()).ToArray())},nodeIndex:{remoteReceiverPicker.SelectedIndex},record:{Js(recordingState)},recordingDetail:{Js(error.IsVisible ? error.Text : this.recordingState.Text)},audio:{Js(audioStatus.Text)},echo:{(workControls["echo"].Enabled.IsToggled ? "true" : "false")},noise:{(reduceNoise.IsToggled ? "true" : "false")},filter:{(workControls["sweep"].Enabled.IsToggled ? "true" : "false")},hops:{hops},saved:{savedPaths.Length},takes:{System.Text.Json.JsonSerializer.Serialize(savedPaths.Select(Path.GetFileName).ToArray())},review:{Js(reviewFileLabel.Text)},playing:{(workPlaybackCancellation is not null ? "true" : "false")},paused:{(workPaused ? "true" : "false")},samples:{System.Text.Json.JsonSerializer.Serialize(centralWave.Samples.Where((_, i) => i % 16 == 0).ToArray())},deck:{DeckStateJson()},fx:{FxJson()},radio:{RadioJson()}}});";
        try { await consoleWebView.EvaluateJavaScriptAsync(script); } catch { }
    }

    static Dictionary<string, string> ParseConsoleQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            result[Uri.UnescapeDataString(parts[0])] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : "";
        }
        return result;
    }

    static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("The embedded console file " + name + " is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    static string? OptionalResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    static string LiveJson(Ghostbox.Core.AudioAnalysis m)
    {
        static string N(double v) => double.IsFinite(v) ? v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "0";
        var wave = string.Join(",", m.Waveform.Where((_, i) => i % 8 == 0).Select(v => N(v)));
        var bands = string.Join(",", m.Spectrum.Select(v => N(v)));
        return $"window.whispersLive?.({{rms:{N(m.RmsDb)},peak:{N(m.PeakDb)},hz:{N(m.DominantHz)},clip:{N(m.ClippedPercent)},bands:[{bands}],wave:[{wave}]}})";
    }

    string FxJson()
    {
        static string N(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        return $"{{echo:{N(EffectValue("echo"))},delay:{N(EffectValue("delay", 250))},feedback:{N(EffectValue("feedback"))},chop:{N(EffectValue("chop"))},reverb:{N(EffectValue("reverb"))}," +
               $"denoise:{N(noiseReduction.Value)},volume:{N(volume.Value)}}}";
    }

    string RadioJson()
    {
        static string J(string? v) => System.Text.Json.JsonSerializer.Serialize(v ?? "");
        bool local = sourcePicker.SelectedIndex == 2, internet = sourcePicker.SelectedIndex == 1;
        string modeName = local ? mode.SelectedItem as string ?? "" : internet ? "STREAM" : remoteMode.SelectedItem as string ?? "";
        return $"{{mode:{J(modeName)},step:{J(local ? step.Text : remoteStep.Text)},dwell:{J(local ? dwell.Text : remoteDwell.Text)},source:{sourcePicker.SelectedIndex}}}";
    }

    const string ConsoleBridgeScript = """
    (() => {
      if (window.__whispersBridgeInstalled) return;
      window.__whispersBridgeInstalled = true;
      const resetScroll = () => { window.scrollTo(0, 0); document.documentElement.scrollTop = 0; document.body.scrollTop = 0; };
      resetScroll(); requestAnimationFrame(resetScroll); setTimeout(resetScroll, 100); setTimeout(resetScroll, 500);
      const send = (action, value) => {
        const suffix = value === undefined ? '' : '?value=' + encodeURIComponent(value);
        window.location.href = 'whispers://' + action + suffix;
      };
      const text = el => (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
      const buttons = [...document.querySelectorAll('button')];
      buttons.forEach(button => {
        const label = text(button).toUpperCase();
        button.addEventListener('click', event => {
        if (label.includes('SCAN PUBLIC SDR')) { event.preventDefault(); send('scanreceivers'); }
        else if (label.includes('START SWEEP')) { event.preventDefault(); startRadio(); }
        else if (label.includes('REC EVP')) { event.preventDefault(); send('record'); }
        else if (label.includes('SAVE WAV')) { event.preventDefault(); send('save'); }
        else if (label.includes('CALIBRATE')) { event.preventDefault(); window.whispersDeckShow?.(); }
        });
        if (label.includes('CALIBRATE')) button.textContent = 'REVIEW AUDIO';
      });
      const selects = [...document.querySelectorAll('select')];
      if (selects[1]) selects[1].addEventListener('change', () => send('receiver', selects[1].selectedIndex));
      const textInputs = [...document.querySelectorAll('input')].filter(i => !['range','checkbox'].includes(i.type));
      if (textInputs[0]) textInputs[0].addEventListener('change', () => send('minimum', textInputs[0].value));
      if (textInputs[1]) textInputs[1].addEventListener('change', () => send('maximum', textInputs[1].value));
      const ranges = [...document.querySelectorAll('input[type=range]')];
      const source = selects[0];
      const remoteModes = ['AM','USB','LSB','CW','NFM'];
      const localModes = ['WFM','WFM_ST','FM','AM','USB','LSB','CW','CWL','CWU','RAW'];
      source.replaceChildren(...['KiwiSDR • AM / narrow FM','Internet radio streams','Gqrx • AM / broadcast FM'].map((name, i) => new Option(name, i)));
      const radio = document.createElement('div');
      radio.className = 'grid grid-cols-2 gap-2 rounded-lg border border-cyan-900 bg-[#05080e] p-3 font-mono text-xs text-cyan-300';
      const field = (name, element) => {
        const label = document.createElement('label');
        label.textContent = name;
        if (element.type === 'checkbox') { label.className = 'flex items-center gap-2 cursor-pointer'; element.className = 'h-4 w-4 rounded border-cyan-700 bg-[#0a1019] text-emerald-500'; }
        else element.className = 'block w-full rounded bg-[#0a1019] text-cyan-200';
        label.append(element); radio.append(label);
        return element;
      };
      const input = (name, value) => field(name, Object.assign(document.createElement('input'), {value}));
      const option = (name, values) => {
        const select = field(name, document.createElement('select'));
        select.replaceChildren(...values.map(value => new Option(value)));
        return select;
      };
      const step = input('STEP FREQUENCY', '9k');
      const dwell = input('DWELL (MS)', '250');
      const mode = option('DEMODULATOR', remoteModes);
      const direction = option('SWEEP DIRECTION', ['Bounce','Forward','Backward','Random']);
      const host = input('GQRX HOST','127.0.0.1');
      const port = input('GQRX TCP PORT','7356');
      const udp = input('GQRX AUDIO UDP PORT','7355');
      const stream = input('STREAM URL (OPTIONAL)','');
      const demo = field('TEST TONE (NO RADIO)', Object.assign(document.createElement('input'), {type:'checkbox'}));
      const switches = [...document.querySelectorAll('input[type=checkbox]')].filter(element => element !== demo);
      switches[0]?.addEventListener('change', () => send('noise'));
      switches[1]?.addEventListener('change', () => send('filtersweep'));
      const rockers = [...document.querySelectorAll('.toggle-rocker')];
      [['DENOISE','noise'], ['FILTER SWEEP','filtersweep'], ['ECHO','echo']].forEach(([name, action], i) => {
        const rocker = rockers[i];
        if (!rocker) return;
        rocker.previousElementSibling.textContent = name;
        rocker.setAttribute('role', 'switch');
        rocker.setAttribute('aria-label', name);
        rocker.tabIndex = 0;
        rocker.addEventListener('click', () => send(action));
        rocker.addEventListener('keydown', event => {
          if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); rocker.click(); }
        });
      });
      const fader = (track, name, action, initial) => {
        const thumb = track.querySelector('.fader-thumb');
        const labels = track.nextElementSibling.children;
        labels[0].textContent = name;
        labels[2].textContent = 'AUDIO';
        const slider = Object.assign(document.createElement('input'), {type:'range', min:'0', max:'100', value:initial});
        slider.setAttribute('aria-label', name);
        slider.style.cssText = 'position:absolute;inset:0;opacity:0;width:28px;height:100%;writing-mode:vertical-lr;direction:rtl;z-index:2';
        track.prepend(slider);
        thumb.style.pointerEvents = 'none';
        const draw = () => {
          thumb.style.top = 'calc(' + (100 - Number(slider.value)) + '% - ' + (38 * (1 - Number(slider.value) / 100)) + 'px)';
          labels[1].textContent = slider.value + '%';
        };
        slider.oninput = draw;
        slider.onchange = () => send(action, slider.value);
        draw();
        (window.__whispersFaders ||= {})[action] = { slider, draw };
      };
      const tracks = document.querySelectorAll('.fader-track');
      if (tracks.length === 2) {
        fader(tracks[0], 'DENOISE', 'denoiselevel', 35);
        fader(tracks[1], 'VOLUME', 'volume', 60);
        for (const track of tracks) {
          const scale = track.previousElementSibling;
          [...scale.children].forEach((tick, i) => tick.textContent = [100, 83, 67, 50, 33, 17, 0][i]);
        }
      }
      source.parentElement.after(radio);
      // The demodulator and direction buttons below already set these; keep the lists only as hidden state.
      mode.parentElement.hidden = true; direction.parentElement.hidden = true;
      function setSource() {
        const local = source.value === '2';
        const internet = source.value === '1';
        mode.replaceChildren(...(local ? localModes : remoteModes).map(value => new Option(value)));
        for (const element of [host,port,udp]) element.parentElement.hidden = !local;
        stream.parentElement.hidden = !internet;
        for (const element of [step,dwell]) element.parentElement.hidden = internet;
        for (const button of buttons) {
          const label = text(button).toUpperCase();
          if (localModes.includes(label)) button.disabled = internet || (!local && !remoteModes.includes(label) && label !== 'FM');
        }
        textInputs[0].value = local ? '87.5M' : '531k';
        textInputs[1].value = local ? '108M' : '1602k';
        step.value = local ? '100k' : '9k';
      }
      source.addEventListener('change', setSource);
      const modeButtons = buttons.filter(button => localModes.includes(text(button).toUpperCase()));
      const directionValues = {'⟳ BOUNCE':'Bounce','▲ UP':'Forward','▼ DOWN':'Backward','⤨ RANDOM':'Random'};
      const stepValues = ['5K','9K','10K'];
      const settingButtons = buttons.filter(button => modeButtons.includes(button) ||
        Object.hasOwn(directionValues, text(button).toUpperCase()) || stepValues.includes(text(button).toUpperCase()));
      const stepReadout = document.getElementById('radio-step-readout');
      const dwellReadout = document.getElementById('radio-dwell-readout');
      function updateRadioControls() {
        for (const button of settingButtons) {
          const label = text(button).toUpperCase();
          const selected = modeButtons.includes(button)
            ? mode.value === (source.value === '0' && label === 'FM' ? 'NFM' : label)
            : directionValues[label] ? direction.value === directionValues[label] : step.value.trim().toUpperCase() === label;
          button.setAttribute('aria-pressed', String(selected));
          button.classList.remove('bg-cyan-950', 'border-cyan-500', 'text-cyan-200', 'shadow-glow-cyan');
          button.style.backgroundColor = selected ? '#0a3445' : '#05080e';
          button.style.borderColor = selected ? '#38bdf8' : '#142033';
          button.style.color = selected ? '#a5f3fc' : '#94a3b8';
        }
        if (stepReadout) stepReadout.textContent = step.value.replace(/k$/i, ' kHz');
        if (dwellReadout) dwellReadout.textContent = dwell.value + ' ms';
      }
      setSource();
      source.addEventListener('change', updateRadioControls);
      for (const element of [mode,direction,step,dwell]) element.addEventListener('input', updateRadioControls);
      updateRadioControls();
      function startRadio() {
        send('start', JSON.stringify([source.value,textInputs[0].value,textInputs[1].value,step.value,dwell.value,mode.value,direction.value,host.value,port.value,udp.value,demo.checked ? '__demo__' : stream.value,String(selects[1].selectedIndex)]));
      }
      for (const button of buttons) {
        const label = text(button).toUpperCase();
        button.addEventListener('click', () => {
          if (['5K','9K','10K'].includes(label)) step.value = label.toLowerCase();
          const selected = label === 'FM' && source.value === '0' ? 'NFM' : label;
          if ([...mode.options].some(item => item.value === selected)) mode.value = selected;
          if (label.includes('BOUNCE')) direction.value = 'Bounce';
          if (label === '▲ UP') direction.value = 'Forward';
          if (label === '▼ DOWN') direction.value = 'Backward';
          if (label === '⤨ RANDOM') direction.value = 'Random';
          updateRadioControls();
        });
      }
      if (ranges[0]) {
        ranges[0].min = '100';
        ranges[0].value = dwell.value;
        ranges[0].addEventListener('input', () => { dwell.value = ranges[0].value; updateRadioControls(); });
      }
      const findExact = value => [...document.querySelectorAll('*')].find(el => el.children.length === 0 && text(el) === value);
      const frequency = [...document.querySelectorAll('div')].find(el =>
        [...el.childNodes].some(node => node.nodeType === 3 && node.textContent.trim() === '142.850'));
      const frequencyText = frequency && [...frequency.childNodes].find(node => node.nodeType === 3 && node.textContent.trim() === '142.850');
      const trace = document.getElementById('receiver-trace');
      const scan = buttons.find(b => text(b).toUpperCase().includes('START SWEEP')); if (scan) scan.id = 'native-start';
      const record = buttons.find(b => text(b).toUpperCase().includes('REC EVP')); if (record) record.id = 'native-record';
      const detail = document.createElement('p');
      detail.className = 'font-mono text-xs text-cyan-300 mt-3 break-words';
      detail.setAttribute('role', 'status');
      record?.parentElement?.after(detail);
      const review = document.createElement('section');
      review.id = 'review-console';
      review.className = 'mt-4 rounded-xl border border-cyan-900 bg-[#0a1019] p-4 font-mono text-xs text-cyan-300';
      review.innerHTML = '<p class="mb-3 text-sm tracking-widest">AUDIO REVIEW / RECORDER</p><div class="grid grid-cols-2 gap-2"></div><p class="mt-3" data-review-status role="status"></p>';
      detail.after(review);
      const controls = review.querySelector('div');
      const takes = document.createElement('select');
      takes.className = 'col-span-2 rounded bg-[#05080e]';
      takes.setAttribute('aria-label', 'Saved recordings');
      takes.onchange = () => { if (takes.value !== '') send('take', takes.value); };
      controls.append(takes);
      const control = (label, action) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'rounded border border-cyan-800 bg-[#07141e] p-2';
        button.textContent = label;
        button.onclick = () => send(action);
        controls.append(button);
        return button;
      };
      const openDeck = document.createElement('button');
      openDeck.type = 'button';
      openDeck.className = 'col-span-2 rounded border border-emerald-500 bg-[#06261c] p-2 text-emerald-300';
      openDeck.textContent = 'OPEN REVIEW DECK • SLOW DOWN & ANALYSE';
      openDeck.onclick = () => window.whispersDeckShow?.();
      controls.append(openDeck);
      control('IMPORT MP3 / WAV / FLAC', 'import');
      const play = control('PLAY REVIEW', 'play');
      const pause = control('PAUSE REVIEW', 'pause');
      control('EXPORT MP3', 'mp3');
      control('RECORD QUESTION', 'mic');
      control('TOGGLE NOISE FILTER', 'noise');
      const monitor = document.createElement('input');
      Object.assign(monitor, {type:'range', min:'0', max:'100', value:'60'});
      monitor.setAttribute('aria-label','Monitor volume');
      monitor.onchange = () => send('volume', monitor.value);
      const monitorLabel = document.createElement('label');
      monitorLabel.textContent = 'MONITOR VOLUME ';
      monitorLabel.append(monitor);
      controls.append(monitorLabel);
      // Live effect sliders live in panel 04 (console-live.js).
      const speedSlider = ranges.at(-1);
      if (speedSlider) {
        Object.assign(speedSlider, { min: '.1', max: '1', step: '.05', value: '1' });
        speedSlider.setAttribute('aria-label', 'Review playback speed');
        speedSlider.addEventListener('change', () => { send('speed', speedSlider.value); setTimeout(() => send('deckspeed', speedSlider.value), 80); });
      }
      window.whispersNativeUpdate = state => {
        [state.noise, state.filter, state.echo].forEach((on, i) => {
          const rocker = rockers[i];
          if (!rocker) return;
          rocker.classList.toggle('is-off', !on);
          rocker.setAttribute('aria-checked', String(on));
          rocker.nextElementSibling.textContent = on ? 'ON' : 'OFF';
        });
        const active = state.receiver !== 'RECEIVER IDLE';
        for (const element of [source, selects[1], ...textInputs, step, dwell, mode, direction, host, port, udp, stream, demo, ranges[0]])
          if (element) element.disabled = active;
        for (const button of settingButtons) {
          const label = text(button).toUpperCase();
          button.disabled = active || (modeButtons.includes(button) &&
            (source.value === '1' || (source.value === '0' && !remoteModes.includes(label) && label !== 'FM')));
          button.title = active ? 'Stop the sweep to change settings' :
            button.disabled ? 'Choose Gqrx for broadcast FM' : '';
        }
        if (switches[0]) switches[0].checked = state.noise;
        if (switches[1]) switches[1].checked = state.filter;
        const takeSignature = JSON.stringify(state.takes);
        if (takes.dataset.items !== takeSignature) {
          takes.replaceChildren(new Option('Choose a saved take', ''), ...state.takes.map((name, i) => new Option(name, i)));
          takes.dataset.items = takeSignature;
        }
        review.querySelector('[data-review-status]').textContent = state.review;
        play.textContent = state.playing ? 'STOP REVIEW' : 'PLAY REVIEW';
        pause.textContent = state.paused ? 'RESUME REVIEW' : 'PAUSE REVIEW';
        pause.disabled = !state.playing;
        const nodes = selects[1];
        const signature = JSON.stringify(state.nodes);
        if (nodes && nodes.dataset.nativeNodes !== signature) {
          nodes.replaceChildren(...state.nodes.map(name => new Option(name)));
          if (!state.nodes.length) nodes.add(new Option('Find public SDR receivers first'));
          nodes.dataset.nativeNodes = signature;
        }
        if (nodes && state.nodes.length) nodes.selectedIndex = state.nodeIndex;
        if (frequencyText) frequencyText.textContent = (state.frequency || '—').replace(/\s*MHz$/i, '') + ' ';
        if (trace) {
          const samples = state.receiver === 'RECEIVER IDLE' ? [] : state.samples;
          trace.setAttribute('d', samples.length < 2 ? 'M0,60 L500,60' : samples.map((sample, i) =>
            (i ? 'L' : 'M') + (i * 500 / (samples.length - 1)).toFixed(1) + ',' + (60 - sample * 55).toFixed(1)).join(' '));
        }
        const start = document.getElementById('native-start'); if (start) start.textContent = state.receiver === 'RECEIVER IDLE' ? '↻ START SWEEP' : '■ STOP SWEEP';
        const rec = document.getElementById('native-record'); if (rec) rec.textContent = state.record;
        detail.textContent = state.receiver + ' • ' + state.audio + ' • ' + state.recordingDetail;
      };
    })();
    """;
}
