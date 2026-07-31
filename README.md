# Matrix Stencil

A reusable .NET 8 console animation that renders layered Matrix-style rain around a hidden message stencil.

The current message is `HELLO WORLD`, but the renderer supports any short message made from printable ASCII characters.

## Demo

![Matrix Stencil animation](media/matrix-stencil-demo.gif)

## How it works

The animation runs as an endless heat cycle.

It begins with a sparse, distant Matrix layer. Additional layers gradually enter from the top of the terminal, increasing the density and brightness of the scene until the hidden message becomes visible.

When the animation cools, new streams stop entering. Existing characters continue falling until they naturally leave the bottom of the terminal.

Nothing abruptly fades in or disappears.

## Matrix layers

The scene contains three independent rain layers.

### Far layer

The far layer is always active.

It is:

- Slow
- Sparse
- Dim
- Visually stable
- Visible both inside and outside the message stencil

This layer provides background depth and keeps the screen from becoming completely empty during the cold portion of the cycle.

### Middle layer

The middle layer begins entering during warm-up.

It adds:

- Moderate density
- Muted green trails
- Occasional brighter characters
- Additional depth behind the foreground rain

When cooling begins, the middle layer stops producing new streams. Existing streams continue falling until they leave the screen.

### Foreground layer

The foreground layer enters later in the heat cycle.

It contains:

- Brighter Matrix characters
- Highlighted stream heads
- Sparse high-intensity characters
- Faster and more visually prominent trails

Foreground highlights do not immediately appear when a stream enters the screen. They mature after the stream has traveled several rows.

## Message stencil

The message is always present, even when it is difficult to see.

Matrix characters inside the stencil are pushed back to the far-layer intensity. Characters near the stencil edges are promoted to brighter intensities, allowing the surrounding Matrix rain to gradually reveal the shape of the letters.

The result is similar to viewing the Matrix through a dark plastic stencil.

During the hottest part of the animation, the stencil interior becomes slightly brighter so the complete phrase can be read without filling the entire screen with additional rain.

## Outline animation

At peak heat, the perimeter of the message begins charging into view.

The outline:

1. Appears gradually instead of popping onto the screen.
2. Uses a controlled set of characters such as `0`, `1`, `|`, and `:`.
3. Progresses through the console intensity levels until it reaches the brightest highlight color.
4. Holds briefly as a complete readable outline.
5. Detaches when cooling begins.
6. Breaks apart into independently falling characters.
7. Changes into the normal Matrix alphabet after falling several rows.
8. Continues downward until every outline character leaves the screen.

This makes the message appear to form from the Matrix and then physically dissolve back into it.

## Heat cycle

The animation progresses through these phases:

```text
Cold hold
Opening middle layer
Opening foreground layer
Hot hold
Peak reveal
Closing foreground layer
Closing middle layer
Cold hold