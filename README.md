# Matrix Stencil

A reusable .NET 8 console animation that renders layered Matrix-style rain through a dark message stencil.

The hard-coded message is currently `HELLO WORLD`, but the core renderer accepts any short message made from printable ASCII characters.

## Visual model

The scene has three independent rain layers:

- **Far layer:** always active, slow, sparse, and barely visible.
- **Middle layer:** enters gradually from the top during warm-up and drains naturally through the bottom during cool-down.
- **Foreground layer:** enters later with brighter characters and sparse highlight heads. Highlights mature only after a stream has traveled several rows.

The message is always present as a stencil. Characters are not erased inside it. Instead, middle and foreground characters are demoted to distant intensities as they pass behind the stencil. This leaves subtle motion inside the letters while the hotter surrounding Matrix rain reveals the phrase by contrast.

## Projects

```text
MatrixStencil.sln
src/
  MatrixStencil.Core/       Platform-independent simulation and rendering
  MatrixStencil.Console/    ANSI console host for Windows and Linux

tests/
  MatrixStencil.Core.Tests/ NUnit tests for glyphs, masks, layers, lifecycle, and stencil behavior
```

## Run

```powershell
dotnet restore
dotnet test
dotnet run --project src/MatrixStencil.Console
```

The best results come from a modern terminal with ANSI true-color support. Windows Terminal, current PowerShell terminals, and common Linux terminals work well.

Controls:

- `R`: restart the heat cycle
- `Space`: pause or resume
- `Q` or `Esc`: quit

## Change the message

Edit `Message` in `src/MatrixStencil.Console/Program.cs`:

```csharp
private const string Message = "Tony-Devs";
```

The `GlyphCatalog` stores every printable ASCII glyph from space (`U+0020`) through tilde (`U+007E`) as an 8-byte bitmap. Each byte is one glyph row, with the most significant bit representing the leftmost pixel.

## Main tuning points

- `HeatCycleOptions.Default`: phase timing
- `MatrixLayerOptions.CreateFar/CreateMiddle/CreateForeground`: density, speed, trail length, and spawn rate
- `ConsolePalette`: RGB colors for depth and highlight levels
- `StencilMapper`: how each layer is visually demoted inside the message
