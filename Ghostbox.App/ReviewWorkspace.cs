using System.Globalization;
using System.Text.Json;
using Ghostbox.Core;

namespace Ghostbox.App;

public partial class MainPage
{
    string? reviewOriginal,reviewCanonical,reviewProcessed;
    string reviewHash="";
    double reviewDuration;
    readonly Entry rangeStart=Input("0","Range start seconds",true),rangeEnd=Input("0","Range end seconds",true),sessionName=Input("Investigation","Session name"),markerNote=Input("","Marker note");
    readonly Picker compareAudio=new() {ItemsSource=new[]{"Original","Processed preview"},SelectedIndex=0};
    readonly Picker namedSessions=new() {Title="Saved investigation sessions"};
    readonly Picker reviewMarkers=new() {Title="Annotated markers"};
    readonly List<InvestigationMarker> investigationMarkers=[];
    readonly ReviewDrawing reviewDrawing=new();
    readonly GraphicsView reviewGraph=new() {HeightRequest=220};
    readonly Label reviewFileLabel=new() {Text="No review recording loaded",FontSize=12};
    readonly Label reviewMetrics=new() {Text="",FontSize=12};
    readonly ActivityIndicator workBusy=new();
    readonly Button applyPreview=new() {Text="Apply preview • selected range"};
    CancellationTokenSource? processingCancellation,workPlaybackCancellation;
    volatile bool workPaused;
    double workPlaySeconds;
    string workPlaybackLabel="Original";
    bool selectEnd;
    int processingRevision;
    readonly Picker exportFormat=new() {ItemsSource=new[]{"WAV (lossless)","MP3 (192 kbps)"},SelectedIndex=0};
    string? lastSavedExport;
    bool sharingExport;
    void InvalidateWorkPreview()
    {
        processingRevision++;reviewProcessed=null;compareAudio.SelectedIndex=0;workPlaybackCancellation?.Cancel();
        processState.Text="Settings changed • Apply preview to update recorded audio";
    }
    static string WorkDirectory=>Path.Combine(FileSystem.AppDataDirectory,"Investigation");
    static Button ActionButton(string title,Action action,string tooltip)
    {var b=new Button {Text=title};b.Clicked+=(_,_)=>action();ToolTipProperties.SetText(b,tooltip);return b;}
    View BuildReviewWorkspace(View legacy)
    {
        Directory.CreateDirectory(WorkDirectory);
        enginePath.Text=Preferences.Default.Get("work-engine","ffmpeg");
        Investigation.EnginePath=enginePath.Text;
        reviewGraph.Drawable=reviewDrawing;
        compareAudio.SelectedIndexChanged+=(_,_)=>workPlaybackCancellation?.Cancel();
        workBusy.SetBinding(IsVisibleProperty,new Binding(nameof(ActivityIndicator.IsRunning),source:workBusy));
        preservePitch.Toggled+=(_,_)=>InvalidateWorkPreview();reverseAudio.Toggled+=(_,_)=>InvalidateWorkPreview();includeCreative.Toggled+=(_,_)=>InvalidateWorkPreview();
        reviewGraph.StartInteraction+=(_,e)=>
        {
            if(reviewDuration<=0||e.Touches.Length==0)return;
            double time=Math.Clamp(e.Touches[0].X/Math.Max(1,reviewGraph.Width),0,1)*(reviewDrawing.ViewEnd-reviewDrawing.ViewStart)+reviewDrawing.ViewStart;
            if(selectEnd) rangeEnd.Text=time.ToString("F3",CultureInfo.InvariantCulture);else rangeStart.Text=time.ToString("F3",CultureInfo.InvariantCulture);
            selectEnd=!selectEnd;UpdateSelectionDrawing();
        };
        rangeStart.TextChanged+=(_,_)=>{UpdateSelectionDrawing();InvalidateWorkPreview();};rangeEnd.TextChanged+=(_,_)=>{UpdateSelectionDrawing();InvalidateWorkPreview();};
        applyPreview.Clicked+=async(_,_)=>await PreparePreviewAsync();
        var legacyToggle=new Button {Text="Recording, conversation & speech tools ▸"};legacy.IsVisible=false;legacyToggle.Clicked+=(_,_)=>legacy.IsVisible=!legacy.IsVisible;
        RefreshSessions();
        var import=ActionButton("Import WAV / MP3 / FLAC",async()=>
        {
            try {var file=await FilePicker.Default.PickAsync(new PickOptions {PickerTitle="Import audio for investigation"});if(file is null)return;
                string extension=Path.GetExtension(file.FileName).ToLowerInvariant();if(extension is not (".wav" or ".mp3" or ".flac"))throw new IOException("Choose WAV, MP3 or FLAC audio.");
                string target=Path.Combine(WorkDirectory,Guid.NewGuid().ToString("N")+"-"+Path.GetFileName(file.FileName));using(var source=await file.OpenReadAsync())using(var dest=File.Create(target))await source.CopyToAsync(dest);
                await LoadReviewAsync(target);}
            catch(Exception ex){ShowError("Import: "+ex.Message);}
        },"Copies the source into the isolated investigation folder; the selected original is untouched.");
        var load=ActionButton("Load selected saved take",async()=> {if(SelectedRecording is null){ShowError("Choose a saved recording in Recording tools first.");return;}await LoadReviewAsync(SelectedRecording);},"Loads a receiver or microphone WAV already recorded by this development app.");
        return Section("Review & export",reviewFileLabel,Row(import,load),processState,workBusy,applyPreview,reviewGraph,
            new Label {Text="Waveform and time-aligned spectrogram from the original. Click once for range start, again for end; or enter seconds. Yellow range = selected audio.",FontSize=12},
            Row(Field("Start (seconds)",rangeStart),Field("End (seconds)",rangeEnd)),
            Row(ActionButton("Zoom to range",()=> {try{var r=ReadRange();reviewDrawing.ViewStart=r.start;reviewDrawing.ViewEnd=r.end;reviewGraph.Invalidate();}catch(Exception ex){ShowError(ex.Message);}},"Zooms the original waveform/spectrogram to the selected time range."),
                ActionButton("Show full recording",()=> {reviewDrawing.ViewStart=0;reviewDrawing.ViewEnd=reviewDuration;reviewGraph.Invalidate();},"Restores full timeline without changing selection.")),
            Row(new Label {Text="Loop selected range"},loopRange),Field("Original / Processed comparison",compareAudio),
            Row(ActionButton("Play comparison",()=>PlayWorkbench(),"Plays Original range or the last Processed preview, as selected."),ActionButton("Pause / resume",()=>workPaused=!workPaused,"Pauses/resumes review playback; does not retune or stop the live receiver.")),
            ActionButton("Stop review / processing",()=>{processingCancellation?.Cancel();workPlaybackCancellation?.Cancel();},"Cancels only offline processing/review playback."),
            reviewMetrics,
            Field("Export format",exportFormat),
            Row(ActionButton("Export selected clip",async()=>await ExportWorkAsync(false),"Explicitly exports the selected Original or Processed version to a new file."),ActionButton("Export full recording",async()=>await ExportWorkAsync(true),"Exports the full original or generates a full processed copy without overwriting the source.")),
            Row(ActionButton("Share saved audio",async()=>await ShareSavedAudioAsync(),"Opens the system share picker with only the saved audio, without session or settings files. Choose the recipient yourself."),ActionButton("Show saved file",async()=>await ShowSavedExportAsync(),"Selects the saved audio in File Explorer. Attach this file using WhatsApp or another app's attachment button if Windows Share does not respond.")),
            ActionButton("Reset processing",ResetWorkProcessing,"Disables every processing and creative effect; original audio remains unchanged."),
            Field("Marker note",markerNote),
            ActionButton("Add annotated marker",()=> {try{var r=ReadRange();investigationMarkers.Add(new(r.start,markerNote.Text??"",RecipeSummary()));UpdateReviewMarkers();}catch(Exception ex){ShowError(ex.Message);}},"Adds a marker at the range start with your note and current processing settings."),reviewMarkers,
            ActionButton("Go to marker",()=> {int i=reviewMarkers.SelectedIndex;if(i<0)return;rangeStart.Text=investigationMarkers[i].Seconds.ToString("F3",CultureInfo.InvariantCulture);},"Moves range start to the selected marker."),
            Field("Session name",sessionName),Row(ActionButton("Save session",async()=>await SaveWorkSessionAsync(),"Saves source path/hash, selection, processing and annotated markers in the isolated app folder."),ActionButton("Open session",async()=>await OpenWorkSessionAsync(),"Restores a selected session and verifies the original file hash.")),namedSessions,
            Field("Local audio engine",enginePath),
            new Label {Text="Requires the installed free FFmpeg engine with rubberband support. Uses local files only. Source separation is not included. Reversed ranges are limited to ten minutes.",FontSize=12},
            legacyToggle,legacy);
    }
    string RecipeSummary()=>string.Join(", ",CurrentRecipe().Controls.Where(p=>p.Value.Enabled).Select(p=>$"{p.Key}={p.Value.Value:0.##}"))+(reverseAudio.IsToggled?", reverse":"");
    (double start,double end) ReadRange()
    {
        if(reviewOriginal is null||!double.TryParse(rangeStart.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double s)||!double.TryParse(rangeEnd.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double e)||!double.IsFinite(s)||!double.IsFinite(e)||s<0||e<=s||e>reviewDuration+.001)throw new ArgumentException("Select a valid range inside the loaded recording.");
        return(s,Math.Min(e,reviewDuration));
    }
    void UpdateSelectionDrawing(){if(double.TryParse(rangeStart.Text,out double s))reviewDrawing.SelectionStart=s;if(double.TryParse(rangeEnd.Text,out double e))reviewDrawing.SelectionEnd=e;reviewGraph.Invalidate();}
    void UpdateReviewMarkers(){reviewMarkers.ItemsSource=investigationMarkers.Select(m=>$"{m.Seconds:F2}s • {m.Note}").ToList();reviewMarkers.SelectedIndex=investigationMarkers.Count-1;}
    async Task LoadReviewAsync(string file)
    {
        if(processingCancellation is not null){ShowError("Wait for the current processing job or cancel it.");return;}
        using var active=new CancellationTokenSource();processingCancellation=active;workBusy.IsRunning=true;
        try
        {
            workPlaybackCancellation?.Cancel();Investigation.EnginePath=enginePath.Text??"ffmpeg";
            string canonical=Path.Combine(WorkDirectory,Guid.NewGuid().ToString("N")+"-canonical.wav");
            // WAV opens on every platform without FFmpeg; MP3/FLAC need the FFmpeg engine (Windows).
            if(WavDecoder.LooksLikeWav(file)) await Task.Run(()=>WavDecoder.ToCanonical(file,canonical),active.Token);
            else
            {
                try { await Investigation.DecodeAsync(file,canonical,active.Token); }
                catch(Exception ex) when (ex is System.ComponentModel.Win32Exception or PlatformNotSupportedException)
                { throw new IOException("MP3 and FLAC need the FFmpeg audio engine, which is not available on this device. WAV recordings open everywhere."); }
            }
            var wave=ConversationFile.Inspect(canonical);reviewOriginal=file;reviewCanonical=canonical;reviewProcessed=null;reviewHash=await Investigation.HashAsync(file);reviewDuration=wave.Length/96000.0;
            rangeStart.Text="0";rangeEnd.Text=reviewDuration.ToString("F6",CultureInfo.InvariantCulture);
            await Task.Run(()=>reviewDrawing.Load(canonical,reviewDuration),active.Token);reviewGraph.Invalidate();
            reviewFileLabel.Text=Path.GetFileName(file)+$" • {reviewDuration:F2}s • original preserved";
            compareAudio.SelectedIndex=0;investigationMarkers.Clear();
            LoadSavedEventMarkers(file);
            UpdateReviewMarkers();
            processState.Text="Original loaded • choose tools, then Apply preview";
            await DeckLoadedAsync();
        }
        catch(Exception ex){ShowError("Loading audio: "+ex.Message);}
        finally{processingCancellation=null;workBusy.IsRunning=false;}
    }
    async Task PreparePreviewAsync()
    {
        if(processingCancellation is not null)return;
        using var active=new CancellationTokenSource();processingCancellation=active;workBusy.IsRunning=true;applyPreview.IsEnabled=false;
        try
        {
            var range=ReadRange();Investigation.EnginePath=enginePath.Text??"ffmpeg";Preferences.Default.Set("work-engine",Investigation.EnginePath);
            string target=Path.Combine(WorkDirectory,Guid.NewGuid().ToString("N")+"-preview.wav");
            processState.Text="Processing selected range locally… receiver remains independent";
            int revision=processingRevision;
            var recipe=CurrentRecipe() with { Channels=1 };await Investigation.RenderAsync(reviewOriginal!,target,recipe,range.start,range.end,includeCreative.IsToggled,active.Token);
            if(revision!=processingRevision){processState.Text="Settings changed during processing; apply a fresh preview.";return;}
            reviewProcessed=target;compareAudio.SelectedIndex=1;
            await File.WriteAllTextAsync(target+".recipe.json",JsonSerializer.Serialize(recipe,SettingsJsonContext.Default.ProcessingRecipe),active.Token);
            var metrics=await Task.Run(()=>ReviewDrawing.Measure(target),active.Token);reviewMetrics.Text=metrics;
            processState.Text="Processed preview ready • compare with Original";
        }
        catch(Exception ex){ShowError("Processing: "+ex.Message);processState.Text="Preview not updated; original unchanged";}
        finally{processingCancellation=null;workBusy.IsRunning=false;applyPreview.IsEnabled=true;}
    }
    async void PlayWorkbench()
    {
        if(workPlaybackCancellation is not null){workPlaybackCancellation.Cancel();return;}
        if(replayCancellation is not null){ShowError("Stop original replay before playing the review range.");return;}
        using var active=new CancellationTokenSource();workPlaybackCancellation=active;workPaused=false;
        try
        {
            var range=ReadRange();bool processed=compareAudio.SelectedIndex==1;string? path=processed?reviewProcessed:reviewCanonical;
            workPlaybackLabel=processed?"Processed":"Original";
            if(path is null)throw new IOException("Apply preview first.");
            await Task.Run(async()=>
            {
                do
                {
                    var wave=ConversationFile.Inspect(path);long first=processed?0:(long)(range.start*48000)*2,last=processed?wave.Length:(long)(range.end*48000)*2;
                    using var input=File.OpenRead(path);input.Position=wave.Offset+first;
                    using var output=new AudioOutput();var buffer=new byte[1920];
                    var rate = new PlaybackRate();
                    for(long position=first;position<last;)
                    {
                        while(workPaused)await Task.Delay(30,active.Token);
                        int count=await input.ReadAsync(buffer.AsMemory(0,(int)Math.Min(buffer.Length,last-position)),active.Token);if(count==0)break;
                        var audio = count == buffer.Length ? buffer : buffer[..count];
                        if (!processed) audio = rate.Process(audio, Volatile.Read(ref replaySpeed));
                        output.Write(audio); position += count; workPlaySeconds = position / 96000.0;
                        await Task.Delay(TimeSpan.FromSeconds(audio.Length / 96000.0), active.Token);
                    }
                    if (!processed) output.Write(rate.Flush(Volatile.Read(ref replaySpeed)));
                    await Task.Delay(100,active.Token);
                }while(loopRange.IsToggled&&!active.IsCancellationRequested);
            },active.Token);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){ShowError("Review playback: "+ex.Message);}
        finally{workPlaybackCancellation=null;workPaused=false;}
    }
    async Task ExportWorkAsync(bool full)
    {
        if(processingCancellation is not null){ShowError("Finish processing first.");return;}
        try
        {
            var range=ReadRange();bool processed=compareAudio.SelectedIndex==1;string output=Path.Combine(WorkDirectory,Guid.NewGuid().ToString("N")+(processed?"-processed-export.wav":"-original-export.wav"));
            using var active=new CancellationTokenSource();processingCancellation=active;workBusy.IsRunning=true;
            try
            {
                var recipe=processed?CurrentRecipe():new ProcessingRecipe();
                // Always render from the original; selected export and full export are explicitly separate.
                await Investigation.RenderAsync(reviewOriginal!,output,recipe,full?0:range.start,full?reviewDuration:range.end,processed&&includeCreative.IsToggled,active.Token);
                if(exportFormat.SelectedIndex==1)
                {
                    string mp3=Path.ChangeExtension(output,".mp3");
                    await Investigation.RunAsync(["-hide_banner","-loglevel","error","-nostdin","-n","-i",output,"-map","0:a:0","-vn","-c:a","libmp3lame","-b:a","192k",mp3],active.Token);
                    output=mp3;
                }
                await File.WriteAllTextAsync(output+".recipe.json",JsonSerializer.Serialize(recipe,SettingsJsonContext.Default.ProcessingRecipe),active.Token);
                string name=string.Concat((sessionName.Text??"Investigation").Where(c=>!Path.GetInvalidFileNameChars().Contains(c)))+(processed?"-processed":"-original")+(full?"-full.wav":"-clip.wav");
                name=Path.ChangeExtension(name,Path.GetExtension(output));
                string? saved=await SaveExportFileAsync(output,name,output+".recipe.json");
                if(saved is not null)lastSavedExport=saved;
                processState.Text=saved is null?"Save cancelled • original audio remains available":"Audio saved: "+saved+" • Share saved audio or attach it directly from Show saved file";
            }
            finally{processingCancellation=null;workBusy.IsRunning=false;}
        }
        catch(Exception ex){ShowError("Export: "+ex.Message);}
    }
    void RefreshSessions(){namedSessions.ItemsSource=Directory.GetFiles(WorkDirectory,"*.session.json").Select(Path.GetFileName).ToList();}
    async Task SaveWorkSessionAsync()
    {
        try
        {
            var range=ReadRange();string name=string.Concat((sessionName.Text??"Investigation").Where(c=>!Path.GetInvalidFileNameChars().Contains(c)));if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a session name.");
            var session=new InvestigationSession {Name=name,OriginalFile=reviewOriginal!,OriginalSha256=reviewHash,Start=range.start,End=range.end,Recipe=CurrentRecipe(),Markers=investigationMarkers.ToList()};
            string path=Path.Combine(WorkDirectory,name+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".session.json");
            await File.WriteAllTextAsync(path,JsonSerializer.Serialize(session,SettingsJsonContext.Default.InvestigationSession));RefreshSessions();processState.Text="Session saved: "+Path.GetFileName(path);
        }
        catch(Exception ex){ShowError("Saving session: "+ex.Message);}
    }
    async Task OpenWorkSessionAsync()
    {
        try
        {
            if(namedSessions.SelectedItem is not string name)throw new ArgumentException("Choose a saved session.");
            var session=JsonSerializer.Deserialize(await File.ReadAllTextAsync(Path.Combine(WorkDirectory,Path.GetFileName(name))),SettingsJsonContext.Default.InvestigationSession)??throw new IOException("Invalid session.");
            if(await Investigation.HashAsync(session.OriginalFile)!=session.OriginalSha256)throw new IOException("Original audio hash changed. Session not restored.");
            await LoadReviewAsync(session.OriginalFile);if(reviewOriginal!=session.OriginalFile)return;
            ResetWorkProcessing();foreach(var (key,value) in session.Recipe.Controls)if(workControls.TryGetValue(key,out var w)){w.Amount.Value=Math.Clamp(value.Value,w.Amount.Minimum,w.Amount.Maximum);w.Enabled.IsToggled=value.Enabled;}
            preservePitch.IsToggled=session.Recipe.PreservePitch;reverseAudio.IsToggled=session.Recipe.Reverse;outputChannels.SelectedIndex=session.Recipe.Channels==2?1:0;
            noiseProfileSource=session.Recipe.NoiseSource;noiseProfileStart=session.Recipe.NoiseStart;noiseProfileEnd=session.Recipe.NoiseEnd;
            rangeStart.Text=session.Start.ToString(CultureInfo.InvariantCulture);rangeEnd.Text=session.End.ToString(CultureInfo.InvariantCulture);sessionName.Text=session.Name;
            investigationMarkers.Clear();investigationMarkers.AddRange(session.Markers);UpdateReviewMarkers();processState.Text="Session restored • original hash verified. Creative export opt-in remains off.";
        }
        catch(Exception ex){ShowError("Opening session: "+ex.Message);}
    }
}

sealed class ReviewDrawing:IDrawable
{
    float[] peaks=[];float[][] bands=[];
    public double ViewStart,ViewEnd,SelectionStart,SelectionEnd;
    double duration;
    public void Load(string path,double seconds)
    {
        var wave=ConversationFile.Inspect(path);duration=seconds;ViewStart=0;ViewEnd=seconds;SelectionStart=0;SelectionEnd=seconds;
        var values=new float[1024];var spectra=new float[160][];
        using var input=File.OpenRead(path);var buffer=new byte[12288];
        for(int i=0;i<values.Length;i++)
        {
            long start=(wave.Length/2*i/values.Length)*2,end=(wave.Length/2*(i+1)/values.Length)*2;input.Position=wave.Offset+start;float peak=0;
            while(start<end){int n=input.Read(buffer,0,(int)Math.Min(buffer.Length,end-start));if(n==0)break;for(int p=0;p+1<n;p+=2)peak=Math.Max(peak,Math.Abs(BitConverter.ToInt16(buffer,p)/32768f));start+=n;}
            values[i]=peak;
        }
        for(int i=0;i<spectra.Length;i++){long offset=Math.Min(Math.Max(0,wave.Length-buffer.Length),(wave.Length/2*i/spectra.Length)*2);input.Position=wave.Offset+offset;int n=input.Read(buffer);var analyzer=new AudioAnalyzer();spectra[i]=analyzer.Add(buffer[..n])?.Spectrum??new float[32];}
        peaks=values;bands=spectra;
    }
    public static string Measure(string path)
    {
        // FFmpeg exports may be mono or stereo; NAudio is not needed to inspect this PCM output.
        using var input=File.OpenRead(path);using var reader=new BinaryReader(input);input.Position=12;long remaining=0;
        while(input.Position+8<=input.Length){string chunk=System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));uint n=reader.ReadUInt32();if(chunk=="data"){remaining=n;break;}input.Position+=n+n%2;}
        double energy=0,peak=0;long samples=0,clipped=0;var bytes=new byte[32768];
        while(remaining>0){int n=input.Read(bytes,0,(int)Math.Min(bytes.Length,remaining));if(n==0)break;remaining-=n;for(int i=0;i+1<n;i+=2){double v=BitConverter.ToInt16(bytes,i)/32768.0;energy+=v*v;peak=Math.Max(peak,Math.Abs(v));if(Math.Abs(v)>=.999)clipped++;samples++;}}
        return $"Processed RMS {20*Math.Log10(Math.Max(1e-12,Math.Sqrt(energy/Math.Max(1,samples)))):F1} dBFS • peak {20*Math.Log10(Math.Max(1e-12,peak)):F1} dBFS • clipped {100.0*clipped/Math.Max(1,samples):F3}%";
    }
    public void Draw(ICanvas c,RectF r)
    {
        c.FillColor=Color.FromArgb("060F18");c.FillRectangle(r);if(peaks.Length==0||duration<=0||ViewEnd<=ViewStart)return;
        float waveHeight=r.Height*.4f;
        for(int x=0;x<(int)r.Width;x++)
        {
            double time=ViewStart+x/Math.Max(1,r.Width)*(ViewEnd-ViewStart);int p=Math.Clamp((int)(time/duration*peaks.Length),0,peaks.Length-1);float v=peaks[p]*waveHeight*.47f;
            c.StrokeColor=Color.FromArgb("70D8F1");c.DrawLine(r.X+x,r.Y+waveHeight/2-v,r.X+x,r.Y+waveHeight/2+v);
            var frame=bands[Math.Clamp((int)(time/duration*bands.Length),0,bands.Length-1)];
            for(int b=0;b<32;b++){float e=frame[b];c.FillColor=new Color(e*.25f,e*.8f,.1f+e*.65f);c.FillRectangle(r.X+x,r.Y+waveHeight+(31-b)*(r.Height-waveHeight)/32,1,(r.Height-waveHeight)/32+1);}
        }
        float left=(float)((SelectionStart-ViewStart)/(ViewEnd-ViewStart)*r.Width),right=(float)((SelectionEnd-ViewStart)/(ViewEnd-ViewStart)*r.Width);
        c.StrokeColor=Color.FromArgb("E5BE76");c.StrokeSize=2;c.DrawRectangle(r.X+Math.Clamp(left,0,r.Width),r.Y,Math.Clamp(right,0,r.Width)-Math.Clamp(left,0,r.Width),r.Height);
    }
}
