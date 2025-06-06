# beampdf

A simple cross-platform app to present a PDF slideshow. Based on [Avalonia](https://github.com/AvaloniaUI/Avalonia) and [MuPDF.NET](https://github.com/ArtifexSoftware/MuPDF.NET).

## Features

- Presenter view
  - Current slide
  - Next slide
  - Timer
  - Clock
- Navigate slides
  - Forward and backwards via arrow keys
  - Type number and hit `Enter` to jump
  - Slide number based on PDF page labels
- Fullscreen slide view
  - In a separate window
  - Keyboard shortcuts (`Shift + N` to move to the n-th screen)
- Simple drawing tool
  - Sketch temporary drawings on the slide
  - Resets on slide navigation or via `Backspace`
  - Supports pen pressure

## Using beampdf

Currently, no binaries are provided. Only tested on Windows, but should run on any platform supported by Avalonia and MuPDF.NET.

To run, install the [.NET SDK](https://dotnet.microsoft.com/en-us/download) and run
```
dotnet run -c Release
```