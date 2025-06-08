# beampdf

A simple cross-platform app to present a PDF slideshow. Based on [Avalonia](https://github.com/AvaloniaUI/Avalonia) and [MuPDF.NET](https://github.com/ArtifexSoftware/MuPDF.NET).

## Features

- Presenter view
  - Displays current slide and previews the next one
  - Timer (starts automatically on first slide switch, start or reset manually with `S`) and current system time in 24h format
- Navigate slides
  - Slide number based on PDF page labels; pages with same label are assumed to be part of a single animated slide
  - Forward and backwards via arrow keys
  - Type number and hit `Enter` to jump
  - Clickable slide-thumbnails with current slide highlighted
- Fullscreen slide view
  - In a separate window
  - Open with `F5` close with `ESC`
  - Keyboard shortcuts (`Shift + N` to move to the n-th screen)
- Simple drawing tool
  - Sketch temporary drawings on the slide
  - Resets on slide navigation or via `Backspace`
  - Supports pen pressure
- Magnifier
  - Select a crop via right-click and drag or `Ctrl+` left-click and drag
  - Reset via right-click or `Ctrl+left`
- Recent files (open dialog with `R`)
  - List of opened files is tracked in a `.csv` file in the `ApplicationData` folder (as reported by `Environment.SpecialFolder.ApplicationData`)
- Speaker notes
  - As embedded files in the PDF
    - filename must be `XX-speaker-note` where XX is the slide number (page label, not "real" page)
    - content is interpreted as UTF-8 string
  - As annotations: annotations in the PDF are rendered into the presenter view only

## Screenshots
### The presenter view in action
<img src="Screenshots/PresenterView.png" width="650" alt="Screenshot of the presenter view with an ongoing slidshow, showing the below mentioned components" />

### Recent files dialog for quick PDF switching
<img src="Screenshots/RecentFiles.png" width="350" alt="Screenshot of the recent files dialog showing thumbnails and filename for two recently opened .pdf files" />

## Using beampdf

Currently, no binaries are provided. Only tested on Windows, but should run on any platform supported by Avalonia and MuPDF.NET.

To run from source, install the [.NET SDK](https://dotnet.microsoft.com/en-us/download) and run
```
dotnet run -c Release
```

To build deployable binaries
```
dotnet publish -c Release
```