using Ghostbox.Core;

namespace Ghostbox.App;

public partial class MainPage
{
    sealed record EffectWidgets(Switch Enabled, Slider Amount, double Default);
    readonly Dictionary<string,EffectWidgets> workControls=[];
    readonly Switch preservePitch = new() { IsToggled=true }, reverseAudio=new(), includeCreative=new(), loopRange=new(), recordEffects=new();
    readonly Picker outputChannels=new() { ItemsSource=new[]{"Mono","Stereo (mono sources duplicated)"}, SelectedIndex=0 };
    readonly Entry enginePath=Input("ffmpeg","Local FFmpeg executable");
    readonly Label processState=new() { Text="Original audio • processing bypassed",FontSize=12 };
    double? noiseProfileStart,noiseProfileEnd;
    string? noiseProfileSource;
    View EffectControl(string key,string title,double min,double max,double initial,string units,string help)
    {
        var enabled=new Switch(); var amount=new Slider { Minimum=min,Maximum=max,Value=initial,IsEnabled=false };
        var number=new Label { Text=$"{initial:0.##} {units}",FontSize=12,TextColor=Color.FromArgb("A6DCED") };
        var reset=new Button { Text="↺",WidthRequest=35,Padding=0,FontSize=16 };
        workControls.Add(key,new(enabled,amount,initial));
        void Changed()
        {
            amount.IsEnabled=enabled.IsToggled; number.Text=$"{amount.Value:0.##} {units}";
            InvalidateWorkPreview();
            UpdateEffects();
        }
        enabled.Toggled+=(_,_)=>Changed(); amount.ValueChanged+=(_,_)=>Changed();
        reset.Clicked+=(_,_)=> { enabled.IsToggled=false;amount.Value=initial;Changed(); };
        foreach(var view in new View[]{enabled,amount,reset}) ToolTipProperties.SetText(view,help);
        SemanticProperties.SetDescription(enabled,title+" enabled"); SemanticProperties.SetDescription(amount,title);
        var line=new Grid { ColumnSpacing=8,ColumnDefinitions=[new(GridLength.Auto),new(GridLength.Star),new(GridLength.Auto),new(GridLength.Auto)] };
        line.Add(enabled,0,0);line.Add(new Label {Text=title,VerticalOptions=LayoutOptions.Center,FontSize=13},1,0);line.Add(number,2,0);line.Add(reset,3,0);
        return new VerticalStackLayout { Spacing=0,Children={line,amount} };
    }
    ProcessingRecipe CurrentRecipe() => new()
    {
        Controls=workControls.ToDictionary(p=>p.Key,p=>new ProcessingControl(p.Value.Enabled.IsToggled,p.Value.Amount.Value)),
        PreservePitch=preservePitch.IsToggled,Reverse=reverseAudio.IsToggled,Channels=outputChannels.SelectedIndex==1?2:1,
        NoiseStart=noiseProfileStart,NoiseEnd=noiseProfileEnd,NoiseSource=noiseProfileSource
    };
    double EffectValue(string key,double fallback=0) => workControls.TryGetValue(key,out var w)&&w.Enabled.IsToggled?w.Amount.Value:fallback;
    View BuildCreativePanel()
    {
        var panel=Section("Creative effects • default off",
            new Label {Text="Creative playback effects are separate from cleanup. Synthetic white noise is never labelled reception.",FontSize=12},
            EffectControl("white","White noise",0,1,.1,"mix","Adds synthetic white noise to monitored audio. Never changes the received original."),
            EffectControl("reverb","Reverb",0,.8,.25,"mix","Simulated acoustic reflections; a creative effect, not recovered sound."),
            EffectControl("echo","Echo",0,.8,.25,"mix","Delayed repetitions, separate from original reception."),
            EffectControl("delay","Echo delay",40,1200,250,"ms","Delay for live monitoring and recorded-audio rendering."),
            EffectControl("feedback","Echo feedback",0,.7,.2,"mix","Controls decay of repeated echoes in recorded previews."),
            EffectControl("distortion","Distortion",0,1,.2,"mix","Controlled saturation; can add harmonics absent from the source."),
            EffectControl("chop","Chop rate",1,12,4,"Hz","Rhythmic gating of monitored and processed audio; original reception is preserved."),
            EffectControl("sweep","Sweeping filter",0,1,1,"on","Optional moving low-pass on monitored audio; offline preview uses a sweeping filter."),
            Row(new Label {Text="Include creative effects in processed preview/export"},includeCreative),
            Row(new Label {Text="Also save live effects recording"},recordEffects),
            new Label {Text="Original received audio is always saved separately. Above choices are explicit opt-ins; raw WAV and conversation WAV stay clean.",FontSize=12},
            SliderField("Monitor volume",volume,v=>$"{v:P0}"),
            Row(new Label {Text="Mute playback while speaking"},muteWhenSpeaking),Row(new Label {Text="Capture radio after my question"},autoListen));
        return panel;
    }
    View BuildCleanupPanel()
    {
        var learn=new Button {Text="Learn noise from selected range"};learn.Clicked+=(_,_)=>
        {
            try {var range=ReadRange();if(reviewOriginal is null)throw new IOException("Import or load a recording first.");if(range.end-range.start<.5)throw new IOException("Select at least 0.5 seconds containing only background noise.");
                noiseProfileSource=reviewOriginal;noiseProfileStart=range.start;noiseProfileEnd=range.end;InvalidateWorkPreview();processState.Text=$"Noise profile selected: {range.start:F2}–{range.end:F2}s. It will be sampled when applying preview.";}
            catch(Exception ex){ShowError(ex.Message);}
        };
        var preset=new Picker {Title="Noise reduction preset",ItemsSource=new[]{"Gentle • 6 dB","Medium • 12 dB","Strong • 24 dB"}};
        preset.SelectedIndexChanged+=(_,_)=> {if(preset.SelectedIndex<0)return;var w=workControls["denoise"];w.Amount.Value=new[]{6,12,24}[preset.SelectedIndex];w.Enabled.IsToggled=true;};
        return Section("Audio cleanup • recorded originals",
            new Label {Text="Select a range in Review, choose tools here, then Apply preview. Cleanup runs locally in the background; live reception continues.",FontSize=12},
            preset,learn,
            EffectControl("denoise","Spectral noise reduction",1,30,12,"dB","Learns the selected noise-only range; without a profile uses adaptive noise tracking. Strong reduction can erase speech."),
            EffectControl("hum","Hum frequency",50,60,50,"Hz","Mains hum notch; set 50 or 60 Hz. Values are snapped to the nearest mains frequency."),
            EffectControl("humdepth","Hum reduction depth",0,36,24,"dB","Adjusts notch depth when hum removal is enabled; default is 24 dB."),
            EffectControl("harmonics","Hum harmonics",0,4,4,"count","Number of additional mains harmonics to reduce; rounded to a whole number."),
            EffectControl("hiss","Hiss/static reduction",1,24,6,"dB","Adaptive FFT denoise. Cannot perfectly separate speech from radio static."),
            EffectControl("highpass","High-pass cutoff",20,1500,180,"Hz","Attenuates frequencies below the cutoff."),
            EffectControl("lowpass","Low-pass cutoff",1500,18000,4200,"Hz","Attenuates frequencies above the cutoff."),
            EffectControl("clicks","Click/pop reduction",0,100,25,"%","Short impulse suppression. Strong values can affect consonants."),
            new Button { Text="Speech-band preset",Command=new Command(()=> {foreach(var k in new[]{"highpass","lowpass"})workControls[k].Enabled.IsToggled=true;workControls["highpass"].Amount.Value=180;workControls["lowpass"].Amount.Value=4200;}) },
            new Label {Text="No cleanup can establish words or a speaker's identity. Compare with Original before exporting.",FontSize=12});
    }
    View BuildVoicePanel()=>Section("Voice processing • recorded audio",
        EffectControl("voice","Voice enhancement",0,8,3,"dB EQ","Speech-band EQ and moderate compression; does not reconstruct missing speech."),
        EffectControl("attenuate","Speech-band attenuation",0,24,6,"dB","Reduces a broad speech band; NOT speaker separation. Mono radio cannot isolate a particular voice."),
        EffectControl("speed","Playback speed",.25,2,1,"×","Apply preview after changing. Pitch-preserving and tape modes are distinct."),
        Row(new Label {Text="Preserve pitch (off = tape speed)"},preservePitch),
        new Label {Text="Common speeds: 0.25, 0.50, 0.75, 1.00, 1.25, 1.50, 2.00×. Reverse affects only the selected preview/clip, never the original.",FontSize=12},
        Row(new Label {Text="Reverse selected recording/range"},reverseAudio),
        EffectControl("bass","Bass EQ",-12,12,0,"dB","Low-frequency equalizer."),
        EffectControl("treble","Treble EQ",-12,12,0,"dB","High-frequency equalizer."),
        EffectControl("compressor","Compressor ratio",1,6,2,"ratio","Controls louder passages; can make background noise more audible."),
        EffectControl("normalize","Normalize loudness",-30,-14,-20,"LUFS","Offline loudness normalization. Adds no speech content."),
        EffectControl("gain","Gain",-18,18,0,"dB","Output gain; positive values can clip. Check processed peak level."),
        EffectControl("limiter","Peak limiter",-12,-.1,-1.5,"dBFS","Limits output peaks when enabled."),
        Field("Export channels",outputChannels));
    void ResetWorkProcessing()
    {
        foreach(var w in workControls.Values){w.Enabled.IsToggled=false;w.Amount.Value=w.Default;}
        reverseAudio.IsToggled=false;preservePitch.IsToggled=true;includeCreative.IsToggled=false;recordEffects.IsToggled=false;
        noiseProfileSource=null;noiseProfileStart=null;noiseProfileEnd=null;UpdateEffects();
        processState.Text="Processing reset • all effects bypassed";
    }
}
