namespace Ghostbox.App;

sealed class SpectrumDrawing : IDrawable
{
    public float[] Bands { get; set; } = new float[32];
    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = Color.FromArgb("142C26"); canvas.FillRectangle(rect);
        canvas.StrokeColor = Color.FromArgb("2A4940"); canvas.StrokeSize = 1;
        for (int i = 1; i < 4; i++) canvas.DrawLine(rect.Left, rect.Top + rect.Height * i / 4, rect.Right, rect.Top + rect.Height * i / 4);
        float width = rect.Width / Bands.Length;
        canvas.FillColor = Color.FromArgb("53C9AA");
        for (int i = 0; i < Bands.Length; i++)
        {
            float height = Math.Max(2, Bands[i] * (rect.Height - 8));
            canvas.FillRectangle(rect.Left + i * width + 1, rect.Bottom - height, Math.Max(1, width - 3), height);
        }
    }
}
