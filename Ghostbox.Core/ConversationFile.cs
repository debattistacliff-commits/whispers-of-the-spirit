using System.Text;

namespace Ghostbox.Core;

public sealed record ConversationSegment(string Path, string Source, DateTime RecordedUtc);
public sealed record PcmWaveData(long Offset, long Length);

public static class ConversationFile
{
    public static PcmWaveData Inspect(string path)
    {
        using var input = File.OpenRead(path);
        using var reader = new BinaryReader(input, Encoding.ASCII);
        if (input.Length < 12 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") throw new IOException("Not a WAV file.");
        uint riffSize = reader.ReadUInt32();
        if (riffSize + 8L > input.Length || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") throw new IOException("Incomplete WAV file.");
        bool formatOk = false; PcmWaveData? data = null;
        while (input.Position + 8 <= input.Length)
        {
            string chunk = Encoding.ASCII.GetString(reader.ReadBytes(4)); uint size = reader.ReadUInt32();
            long end = input.Position + size;
            if (end > input.Length) throw new IOException("WAV chunk is incomplete.");
            if (chunk == "fmt ")
            {
                if (size < 16) throw new IOException("Invalid WAV format.");
                int format = reader.ReadUInt16(), channels = reader.ReadUInt16(), rate = reader.ReadInt32();
                reader.ReadInt32(); reader.ReadUInt16(); int bits = reader.ReadUInt16();
                formatOk = format == 1 && channels == 1 && rate == 48000 && bits == 16;
            }
            if (chunk == "data") data = new(input.Position, size);
            input.Position = end + (size % 2);
        }
        if (!formatOk || data is null || data.Length % 2 != 0) throw new IOException("Expected a mono 48 kHz 16-bit PCM WAV.");
        return data;
    }

    public static string Save(string destination, IReadOnlyList<ConversationSegment> segments)
    {
        if (segments.Count == 0) throw new ArgumentException("Record a microphone or receiver clip first.");
        var audio = segments.Select(s => Inspect(s.Path)).ToArray();
        if (audio.Sum(a => a.Length) > uint.MaxValue - 1_000_000) throw new IOException("Conversation is too large for WAV. Start a new file.");
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".building";
        var notes = new StringBuilder("VOX CONVERSATION RECORDING\nOriginal microphone and received audio in recorded order.\nGaps between clips are omitted; no extra voices or replies are generated.\n\n");
        long bytes = 0;
        try
        {
            using (var output = new PcmWaveWriter(temporary))
            {
                var buffer = new byte[32768];
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i]; var part = audio[i];
                    notes.AppendLine(FormattableString.Invariant($"{bytes / 96000.0:F3}s | {part.Length / 96000.0:F3}s | {segment.Source} | UTC {segment.RecordedUtc:O} | {Path.GetFileName(segment.Path)}"));
                    using var source = File.OpenRead(segment.Path); source.Position = part.Offset;
                    long remaining = part.Length;
                    while (remaining > 0)
                    {
                        int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                        if (read == 0) throw new IOException("Source audio changed while saving.");
                        output.Write(buffer, read); remaining -= read; bytes += read;
                    }
                }
            }
            // Embed the source/timestamp index inside RIFF LIST/INFO/ICMT as well as a readable sidecar.
            byte[] comment = Encoding.UTF8.GetBytes(notes.ToString() + "\0");
            using (var file = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(file, Encoding.ASCII, leaveOpen: true))
            {
                file.Position = file.Length;
                writer.Write(Encoding.ASCII.GetBytes("LIST")); writer.Write(12 + comment.Length + comment.Length % 2);
                writer.Write(Encoding.ASCII.GetBytes("INFOICMT")); writer.Write(comment.Length); writer.Write(comment);
                if (comment.Length % 2 != 0) writer.Write((byte)0);
                file.Position = 4; writer.Write(checked((uint)(file.Length - 8)));
            }
            File.Move(temporary, destination, overwrite: true);
            File.WriteAllText(Path.ChangeExtension(destination, ".markers.txt"), notes.ToString());
            return notes.ToString();
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
