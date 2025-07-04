#import "@preview/touying:0.6.1": *
#import themes.simple: *
#import "beampdf.typ": *

#show: simple-theme.with(
  aspect-ratio: "16-9",
  footer: [Powered by Typst],
)

#title-slide[
  #v(6em)
  = The perfect slides
  #v(.3em)
  #text([BeamPDF test slides], size: 1.2em)

  #v(1.5em)
  Pascal Grittmann

  #place(
    top + left,
    dx: 80%,
    image("256x256.png", width: 5cm),
  )
]

== This is a presentation

- What are we talking about today?
#only(1, speaker-note[Deliver a great pun])
#pause
- Stay and find out!
#only(2, speaker-note("Second note on the \\ same slide"))

== Videos are everywhere

// Videos and notes are not supported by touying's pause
#only(1, place(top + right, video("beampdf-logo-static.png", "beampdf-logo.mkv", width: 40%, loop: false)))
- First, we play it once
#pause
#only(2, place(top + right, video("beampdf-logo-static.png", "beampdf-logo.mkv", width: 40%, loop: true)))
- Then, we play it forever
#pause
That's enough videos for today!

#speaker-note("First video should be looping now, then the second plays once")

== A video that goes on and on


#only("2-", place(top + right, video("beampdf-logo-static.png", "beampdf-logo.mkv", width: 40%, loop: true)))
- We are
#pause
- updating
#pause
- the slide
#pause
- yet the
#pause
- video stays

== Good Bye!

- More content on a slide, hopefully the video is gone now #emoji.face.peek
#speaker-note("This is the last slide, we made it!!")


#embed-video-list()
#embed-speaker-notes()
