using System.Text.Json;
using System.Text.Json.Serialization;

namespace beampdf;

/// <summary>
/// Loads a pdf file and allows rendering its pages in a desired resolution.
/// </summary>
public class PdfSlide : IDisposable
{
    MuPDF.NET.Document openDoc;
    string pdfFilename;
    List<MuPDF.NET.Label> pageLabels;
    List<int> pageNumbers = [];

    public PdfSlide(string filename)
    {
        openDoc = new MuPDF.NET.Document(filename);
        pdfFilename = filename;

        ResolvePageLabels();
        ExtractNotes();
        ExtractVideos();
    }

    public MuPDF.NET.Page this[int page] => openDoc[page];

    public async Task<Bitmap> RenderPage(
        int page,
        MuPDF.NET.Rect clipRect,
        double targetWidth,
        double targetHeight,
        double dpiScaling,
        bool showAnnotations = false
    )
    {
        float yscale = clipRect == null ? 1.0f : (openDoc[page].Rect.Height / clipRect.Height);
        float xscale = clipRect == null ? 1.0f : (openDoc[page].Rect.Width / clipRect.Width);

        float zoomX = (float)(targetWidth * dpiScaling * xscale) / openDoc[page].Rect.Width;
        float zoomY = (float)(targetHeight * dpiScaling * yscale) / openDoc[page].Rect.Height;
        float zoom = float.Min(zoomX, zoomY);

        return await Task.Run(() =>
        {
            lock (openDoc)
            {
                MuPDF.NET.Pixmap pixmap = openDoc[page]
                    .GetPixmap(
                        matrix: new MuPDF.NET.Matrix(zoom, zoom),
                        colorSpace: "rgb",
                        alpha: false,
                        annots: showAnnotations,
                        clip: clipRect
                    );
                return new Bitmap(
                    PixelFormats.Rgb24,
                    AlphaFormat.Opaque,
                    (nint)pixmap.SamplesPtr,
                    new(pixmap.W, pixmap.H),
                    new(pixmap.Xres, pixmap.Yres),
                    pixmap.W * 3
                );
            }
        });
    }

    /// <summary>
    /// The number of pages in the PDF.
    /// </summary>
    public int NumPages => pageNumbers.Count;

    /// <summary>
    /// Retrieves the slide index that contains the given page. Animated slides
    /// consist of multiple pages. This is determined from the page labels
    /// in the PDF.
    /// </summary>
    /// <param name="page">Number between 0 (inclusive) and <see cref="NumPages" /> (exclusive). Clamping is performed for values outside the range.</param>
    public int GetSlideNumber(int page)
    {
        if (page < 0)
            return 0;
        if (page >= NumPages)
            return pageNumbers[^1];
        return pageNumbers[page];
    }

    /// <returns>
    /// Page number of the last page in the PDF that is part of the
    /// given slide number.
    /// </returns>
    public int GetLastPageOfSlide(int slide)
    {
        int n = pageNumbers.FindLastIndex(idx => idx == slide);
        return Math.Clamp(n, 0, pageNumbers.Count - 1);
    }

    void ResolvePageLabels()
    {
        pageLabels = openDoc.GetPageLabels();

        if (pageLabels.Count == 0)
        {
            pageNumbers = [];
            for (int i = 0; i < openDoc.PageCount; ++i)
                pageNumbers.Add(i + 1);
            return;
        }

        // TODO not sure if sorting is required or already guaranteed...
        var sortedLabels = pageLabels.OrderBy(lbl => lbl.StartPage).ToList();
        int nextLabel = 0;

        pageNumbers = [];
        int p = 1;
        for (int i = 0; i < openDoc.PageCount; ++i)
        {
            if (nextLabel < sortedLabels.Count && i == sortedLabels[nextLabel].StartPage)
            {
                p = sortedLabels[nextLabel].FirstPageNum;
                nextLabel++;
            }

            pageNumbers.Add(p++);
        }
    }

    Dictionary<int, string> notes;

    /// <param name="page">Raw page number, starting at 0</param>
    public string TryGetSpeakerNote(int page)
    {
        if (notes.TryGetValue(page + 1, out string note))
            return note;
        return "";
    }

    struct SpeakerNote
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }
    }

    void ExtractNotes()
    {
        notes = [];

        int num = openDoc.GetEmbfileCount();
        for (int i = 0; i < num; ++i)
        {
            var info = openDoc.GetEmbfileInfo(i);
            if (info.Desc != "speaker-note-list")
                continue;

            string content = System.Text.Encoding.UTF8.GetString(openDoc.GetEmbfile(i));
            var noteList = JsonSerializer.Deserialize<SpeakerNote[]>(content);
            foreach (var n in noteList)
            {
                notes.Add(n.Page, n.Note);
            }
        }
    }

    public record struct VideoInfo(string Filename, float X, float Y, float W, bool IsLoop) { }

    Dictionary<int, VideoInfo> videos;
    private bool disposedValue;

    /// <param name="page">Raw PDF page number, starts at 0</param>
    public VideoInfo? TryGetVideo(int page)
    {
        if (videos.TryGetValue(page + 1, out var vid))
            return vid;
        return null;
    }

    void ExtractVideos()
    {
        videos = [];

        int num = openDoc.GetEmbfileCount();
        for (int i = 0; i < num; ++i)
        {
            var info = openDoc.GetEmbfileInfo(i);
            if (info.Desc != "video-list")
                continue;

            string content = System.Text.Encoding.UTF8.GetString(openDoc.GetEmbfile(i));
            using StringReader reader = new(content);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var vid = line.Split('*');
                if (!int.TryParse(vid[0], out int slideNum))
                    continue;

                bool isVideoLoop = bool.Parse(vid[1]);

                var box = vid[2].Split(',');
                if (box.Length != 3)
                    continue;
                if (!float.TryParse(box[0], out float x))
                    continue;
                if (!float.TryParse(box[1], out float y))
                    continue;
                if (!float.TryParse(box[2], out float w))
                    continue;

                string filename = Path.Join(Path.GetDirectoryName(pdfFilename), vid[3]);

                videos.Add(slideNum, new(filename, x, y, w, isVideoLoop));
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) { }
            openDoc.Close();
            disposedValue = true;
        }
    }

    ~PdfSlide()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
