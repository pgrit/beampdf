/// Adds an invisible speaker note, i.e., text that is displayed in the presenter view
///
/// - text: A single-line string or single-line content with the speaker notes.
///         Line breaks can be added via `\`, e.g. `speaker-note("first line\second line")`
#let speaker-note(text) = context {
  // Convert content to raw text
  let raw-text = if type(text) == content and text.has("text") {
    text.text
  } else {
    text
  }
  // Add a metadata block (invisible) with a label so we can locate this note
  [#metadata(raw-text)<speaker-note>]
}

/// Add the list of speaker notes to an embedded file in the output pdf
#let embed-speaker-notes() = context {
  // Concatenate video information into a list of strings of form:
  // [page]*[slide]*[note]
  let list-str = "[" + query(<speaker-note>)
    .map(it => {
      let page = "\"page\":" + str(it.location().page())
      let note = "\"note\":\"" + it.value.replace("\n", "\\n").replace("\"", "\\\"") + "\""
      "{" + page + "," + note + "}"
    })
    .join(",\n") + "]"

  if list-str != none {
    pdf.attach(
      "speaker-note-list",
      bytes(list-str),
      description: "speaker-note-list",
      mime-type: "text/plain",
    )
  }
}

/// Adds a video
///
/// Additional arguments will be passed to the `image()` function
///
/// - static: The preview image to put into the static output file
///
/// - filename: Video filename relative to the output file
///
/// - width: Size of the video relative to the parent
///
/// - loop: Set to true to add a hint that the video should loop during playback
#let video(static, filename, width, loop: false, ..args) = block(
  layout(imgsize => {
    let pos = here().position()
    let size-hint = str(pos.x.pt()) + "," + str(pos.y.pt()) + "," + str(imgsize.width.pt())

    // Display static preview and the filename of the video for manual playback in handouts
    image(static, width: 100%, ..args)
    [#metadata((filename, loop, size-hint)) <video-placeholder>]
    place(dy: -1.5em - 8pt, dx: 1em, box(link(filename, filename), fill: white, inset: 8pt))
  }),
  width: width,
)

/// Add the list of videos and their positions to an embedded file in the output pdf
#let embed-video-list() = context {
  // Concatenate video information into a list of strings of form:
  // [slide]*[isLooping]*[x],[y],[w]*[filename]
  let list-str = query(<video-placeholder>)
    .map(it => {
      (
        str(it.location().page())
          + "*"
          + if it.value.at(1) { "true" } else { "false" }
          + "*"
          + it.value.at(2)
          + "*"
          + it.value.at(0)
          + "\n"
      )
    })
    .join()

  if list-str != none {
    pdf.attach(
      "video-list",
      bytes(list-str),
      description: "video-list",
      mime-type: "text/plain",
    )
  }
}
