using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Kinect;

namespace Ghostbox.Kinect;

/// <summary>
/// Reads a Kinect v1 (Xbox 360 Kinect with its PC adapter, or Kinect for Windows) and writes to
/// standard output, about 30 times a second:
///   'I' + int32 length + JPEG picture (infrared night vision, or depth shown as distance colours)
///   'S' + int32 length + UTF-8 JSON  {"people":[[[x,y,state] × 20 joints], …]}  (x, y from 0 to 1)
/// and status lines 'T' + int32 length + UTF-8 text ("ready", "no-kinect", "error: …").
/// Standard input: "ir" or "depth" switches the picture; closing it (the app ending) stops the helper.
/// Nothing is saved; the Kinect's infrared light is on only while the helper runs.
/// </summary>
static class Program
{
    static readonly Stream Out = Console.OpenStandardOutput();
    static readonly object Gate = new();
    static volatile string mode = "ir";
    static volatile bool running = true;

    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "ir" or "depth") mode = args[0];
        var sensor = KinectSensor.KinectSensors.FirstOrDefault(k => k.Status == KinectStatus.Connected);
        if (sensor is null) { Status("no-kinect"); return 2; }
        try
        {
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.SkeletonStream.Enable(new TransformSmoothParameters { Smoothing = 0.5f, Correction = 0.5f, Prediction = 0.4f, JitterRadius = 0.05f, MaxDeviationRadius = 0.04f });
            sensor.SkeletonStream.EnableTrackingInNearRange = true;
            sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
            sensor.Start();
        }
        catch (Exception e) { Status("error: " + e.Message); return 3; }
        Status("ready");

        // Commands from the app; when the app closes the pipe, stop.
        new Thread(() =>
        {
            try
            {
                string? line;
                while ((line = Console.ReadLine()) is not null)
                    if (line.Trim() is "ir" or "depth") mode = line.Trim();
            }
            catch { }
            running = false;
        }) { IsBackground = true }.Start();

        var depthPixels = new DepthImagePixel[320 * 240];
        var skeletons = new Skeleton[6];
        byte[] irBytes = new byte[640 * 480 * 2];
        try
        {
            while (running)
            {
                bool newDepth = false;
                using (var f = sensor.DepthStream.OpenNextFrame(40))
                    if (f is not null) { f.CopyDepthImagePixelDataTo(depthPixels); newDepth = true; }
                using (var f = sensor.SkeletonStream.OpenNextFrame(0))
                    if (f is not null) { f.CopySkeletonDataTo(skeletons); Send('S', Encoding.UTF8.GetBytes(SkeletonJson(sensor, skeletons))); }
                if (mode == "ir")
                {
                    using var f = sensor.ColorStream.OpenNextFrame(0);
                    if (f is not null) { f.CopyPixelDataTo(irBytes); Send('I', InfraredJpeg(irBytes)); }
                }
                else if (newDepth) Send('I', DepthJpeg(depthPixels));
            }
        }
        catch (Exception e) { Status("error: " + e.Message); }
        finally { try { sensor.Stop(); } catch { } }
        return 0;
    }

    static readonly JointType[] Order = (JointType[])Enum.GetValues(typeof(JointType));

    static string SkeletonJson(KinectSensor sensor, Skeleton[] all)
    {
        var sb = new StringBuilder("{\"people\":[");
        bool first = true;
        foreach (var s in all.Where(s => s is not null && s.TrackingState == SkeletonTrackingState.Tracked))
        {
            if (!first) sb.Append(','); first = false;
            sb.Append('[');
            for (int i = 0; i < Order.Length; i++)
            {
                var j = s.Joints[Order[i]];
                var p = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(j.Position, DepthImageFormat.Resolution320x240Fps30);
                if (i > 0) sb.Append(',');
                sb.Append('[').Append((p.X / 320.0).ToString("0.####", CultureInfo.InvariantCulture)).Append(',')
                  .Append((p.Y / 240.0).ToString("0.####", CultureInfo.InvariantCulture)).Append(',')
                  .Append(j.TrackingState == JointTrackingState.Tracked ? 2 : j.TrackingState == JointTrackingState.Inferred ? 1 : 0).Append(']');
            }
            sb.Append(']');
        }
        return sb.Append("]}").ToString();
    }

    // Infrared: 16-bit brightness → grey picture (the Kinect's own dot pattern shows as fine speckle).
    static readonly Bitmap IrBitmap = new(640, 480, PixelFormat.Format24bppRgb);
    static readonly byte[] IrRgb = new byte[640 * 480 * 3];
    static readonly int[] Histogram = new int[1024];
    static readonly byte[] Curve = new byte[1024];
    static double gain = 4;
    static byte[] InfraredJpeg(byte[] ir)
    {
        // Automatic brightness: the brightest 1 % becomes white (smoothed between frames), with a gentle curve
        // so dark rooms still show detail.
        Array.Clear(Histogram, 0, Histogram.Length);
        for (int i = 0; i < 640 * 480; i++) Histogram[(ir[i * 2] | ir[i * 2 + 1] << 8) >> 6]++;
        int top = 1023, seen = 0;
        while (top > 8 && (seen += Histogram[top]) < 640 * 480 / 100) top--;
        gain = gain * 0.85 + Math.Min(40, 1023.0 / Math.Max(8, top)) * 0.15;
        for (int v = 0; v < 1024; v++) Curve[v] = (byte)(255 * Math.Pow(Math.Min(1, v * gain / 1023.0), 0.7));
        for (int i = 0, o = 0; i < 640 * 480; i++, o += 3)
            IrRgb[o] = IrRgb[o + 1] = IrRgb[o + 2] = Curve[(ir[i * 2] | ir[i * 2 + 1] << 8) >> 6];
        return Jpeg(IrBitmap, IrRgb, 640, 480);
    }

    // Depth: near = warm, far = cool, unknown = black.
    static readonly Bitmap DepthBitmap = new(320, 240, PixelFormat.Format24bppRgb);
    static readonly byte[] DepthRgb = new byte[320 * 240 * 3];
    static byte[] DepthJpeg(DepthImagePixel[] px)
    {
        for (int i = 0, o = 0; i < px.Length; i++, o += 3)
        {
            var p = px[i];
            if (!p.IsKnownDepth || p.Depth <= 0) { DepthRgb[o] = DepthRgb[o + 1] = DepthRgb[o + 2] = 0; continue; }
            double t = Math.Max(0, Math.Min(1, (p.Depth - 800) / 3700.0));   // 0.8 m … 4.5 m
            var (r, g, b) = Ramp(t);
            if (p.PlayerIndex > 0) { r = (byte)Math.Min(255, r + 60); g = (byte)Math.Min(255, g + 60); b = (byte)Math.Min(255, b + 60); }
            DepthRgb[o] = b; DepthRgb[o + 1] = g; DepthRgb[o + 2] = r;          // BGR in memory
        }
        return Jpeg(DepthBitmap, DepthRgb, 320, 240);
    }

    static (byte, byte, byte) Ramp(double t)
    {
        // white → yellow → orange → red → purple → deep blue
        (double, double, double)[] stops = { (1, 1, .9), (1, .85, .1), (1, .45, .05), (.85, .05, .2), (.4, .05, .55), (.05, .05, .35) };
        double x = t * (stops.Length - 1); int i = Math.Min(stops.Length - 2, (int)x); double f = x - i;
        var a = stops[i]; var b = stops[i + 1];
        return ((byte)(255 * (a.Item1 + (b.Item1 - a.Item1) * f)), (byte)(255 * (a.Item2 + (b.Item2 - a.Item2) * f)), (byte)(255 * (a.Item3 + (b.Item3 - a.Item3) * f)));
    }

    static readonly ImageCodecInfo JpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
    static readonly EncoderParameters JpegQuality = new(1) { Param = { [0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L) } };
    static byte[] Jpeg(Bitmap bmp, byte[] rgb, int w, int h)
    {
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        for (int y = 0; y < h; y++) Marshal.Copy(rgb, y * w * 3, data.Scan0 + y * data.Stride, w * 3);
        bmp.UnlockBits(data);
        using var ms = new MemoryStream();
        bmp.Save(ms, JpegCodec, JpegQuality);
        return ms.ToArray();
    }

    static void Status(string text) => Send('T', Encoding.UTF8.GetBytes(text));

    static void Send(char type, byte[] payload)
    {
        lock (Gate)
        {
            try
            {
                Out.WriteByte((byte)type);
                Out.Write(BitConverter.GetBytes(payload.Length), 0, 4);
                Out.Write(payload, 0, payload.Length);
                Out.Flush();
            }
            catch { running = false; }
        }
    }
}
