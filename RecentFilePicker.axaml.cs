namespace beampdf;

public partial class RecentFilePicker : Window
{
    static FileInfo GetRecentFile()
    {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string filename = Path.Join(appdata, "beampdf", "recent.csv");
        if (!File.Exists(filename)) {
            Directory.CreateDirectory(Path.Join(appdata, "beampdf"));
            File.WriteAllText(filename, "");
        }
        return new(filename);
    }

    static List<(DateTime Time, string Name)> entries = [];

    static RecentFilePicker()
    {
        // Configure the TextFieldParser to parse CSV files
        var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(GetRecentFile().FullName)
        {
            TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited,
            TrimWhiteSpace = true,
            HasFieldsEnclosedInQuotes = true,
            Delimiters = [","]
        };

        // Read all data lines
        while (!parser.EndOfData)
        {
            try
            {
                var line = parser.ReadFields();
                entries.Add((DateTime.Parse(line[0]), line[1]));
            }
            catch
            {
                Console.WriteLine("Invalid data in recent.csv -- skipped");
            }
        }
    }

    public static void AddFile(string name, DateTime time)
    {
        int i = entries.FindIndex(v => v.Name == name);
        if (i >= 0)
            entries[i] = entries[i] with { Time = time };
        else
        {
            entries.Add((time, name));
            File.AppendAllLines(GetRecentFile().FullName, [ $"{time},{name}" ]);
        }
    }

    const double thumbnailSize = 128;

    async Task<Bitmap> RenderThumbnail(string filename)
    {
        MuPDF.NET.Document openDoc = new(filename);

        float zoomX = (float)(thumbnailSize * VisualRoot.RenderScaling) / openDoc[0].Rect.Width;
        float zoomY = (float)(thumbnailSize * VisualRoot.RenderScaling) / openDoc[0].Rect.Height;
        float zoom = float.Min(zoomX, zoomY);

        var result = await Task.Run(() =>
        {
            MuPDF.NET.Pixmap pixmap = openDoc[0].GetPixmap(matrix: new MuPDF.NET.Matrix(zoom, zoom), colorSpace: "rgb",
                alpha: false, annots: false);
            return new Bitmap(PixelFormats.Rgb24, AlphaFormat.Opaque, (nint)pixmap.SamplesPtr,
                new(pixmap.W, pixmap.H), new(pixmap.Xres, pixmap.Yres), pixmap.W * 3);
        });

        openDoc.Close();
        return result;
    }

    public RecentFilePicker()
    {
        InitializeComponent();

        Populate();
    }

    public string SelectedFilename { get; private set; }

    async void Populate()
    {
        var sorted = entries.OrderBy(v => v.Time).Where(v => Path.Exists(v.Name));
        foreach (var e in sorted)
        {
            var entry = e; // for the lambda capture
            var bmp = await RenderThumbnail(entry.Name);

            var txt = new TextBlock()
            {
                TextAlignment = TextAlignment.Center,
                Margin = new(0, 1, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Text = Path.GetFileName(entry.Name),
            };
            var img = new Image()
            {
                Width = thumbnailSize,
                Source = bmp,
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            ToolTip.SetTip(txt, entry.Name);
            ToolTip.SetTip(img, entry.Name);
            void open()
            {
                SelectedFilename = entry.Name;
                Close();
            }
            img.PointerReleased += (_,_) => open();
            txt.PointerReleased += (_,_) => open();

            StackPanel stack = new()
            {
                Margin = new(30)
            };
            stack.Children.Add(img);
            stack.Children.Add(txt);
            Container.Children.Add(stack);

        }
    }
}
