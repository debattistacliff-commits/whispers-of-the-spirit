using Ghostbox.Core;

namespace Ghostbox.App;

sealed class AudioHeatDrawing : IDrawable
{
    const int Capacity = 120;
    readonly Queue<float[]> history = new();
    float[] waveform = [];
    public void Reset() { history.Clear(); waveform = []; }
    public void Add(AudioAnalysis frame)
    {
        waveform = frame.Waveform;
        history.Enqueue(frame.Spectrum);
        while (history.Count > Capacity) history.Dequeue();
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        canvas.FillColor = Color.FromArgb("081219"); canvas.FillRectangle(bounds);
        float waveHeight = 72, middle = bounds.Y + waveHeight / 2;
        canvas.StrokeColor = Color.FromArgb("33454F"); canvas.StrokeSize = 1;
        canvas.DrawLine(bounds.Left, middle, bounds.Right, middle);
        canvas.StrokeColor = Color.FromArgb("42EFAD");
        if (waveform.Length > 1)
        {
            var path = new PathF();
            for (int i = 0; i < waveform.Length; i++)
            {
                float x = bounds.X + i * bounds.Width / (waveform.Length - 1), y = middle - waveform[i] * 32;
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
            canvas.DrawPath(path);
        }
        float top = bounds.Y + waveHeight + 8, height = bounds.Height - waveHeight - 8;
        int column = Capacity - history.Count;
        foreach (var bands in history)
        {
            for (int band = 0; band < bands.Length; band++)
            {
                float energy = Math.Clamp(bands[band], 0, 1);
                canvas.FillColor = energy < .5f
                    ? new Color(.02f, .08f + energy * 1.5f, .12f + energy * .6f)
                    : new Color((energy - .5f) * 2, .83f, .42f - (energy - .5f) * .7f);
                canvas.FillRectangle(bounds.X + column * bounds.Width / Capacity, top + (bands.Length - 1 - band) * height / bands.Length,
                    bounds.Width / Capacity + .5f, height / bands.Length + .5f);
            }
            column++;
        }
    }
}
