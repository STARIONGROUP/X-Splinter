# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Overview

**X-Splinter** is a .NET 10 command-line utility that splits a single monolithic
Enterprise Architect (EA) XMI export into multiple, smaller XMI files — one per
UML package. As it splits, it rewrites cross-package references so the resulting
files still resolve against each other: an intra-file `xmi:idref` that points to
an element now living in a *different* output file is converted into a cross-file
`href="targetFile.xmi#id"`. This lets a consumer (e.g. UML4NET) load the pieces
independently while types still resolve across files.

## Build & Run

Requires the **.NET 10 SDK**.

```bash
# Build
dotnet build X-Splinter.sln

# Run
dotnet run --project XSplinter/XSplinter.csproj -- <input.xmi> <config.json> [--output <dir>]
```

- `<input.xmi>` — the monolithic XMI file exported from Enterprise Architect.
- `<config.json>` — the splitter configuration (see below).
- `--output <dir>` — output directory (optional; defaults to the current directory `.`).
- Exit code: `0` on success, `1` on error (missing files, bad config, or a splitting exception).

## Configuration

The config file is JSON deserialized into `SplitterConfig` (property matching is
case-insensitive). A sample lives at `example/packages.json` — note this is a
*splitter config sample*, **not** an npm/Node file.

```json
{
  "rootPackageName": "5. Data Structure",
  "packages": [
    { "name": "Primitives", "outputFile": "CSharp_Primitives.xmi", "convertToLibrary": true },
    { "name": "Forge", "outputFile": "Forge.xmi" },
    { "name": "FunctionalData", "outputFile": "FunctionalData.xmi" }
  ]
}
```

- `rootPackageName` — name of the root container package inside the XMI's `uml:Model`.
- `packages[]` — the child packages to extract, each with:
  - `name` — the UML package name as it appears in the XMI.
  - `outputFile` — the filename to write this package to.
  - `convertToLibrary` (optional, default `false`):
    - `false` → full EA model output: `uml:Model name="EA_Model"` wrapper plus a
      filtered `xmi:Extension` section.
    - `true` → plain `uml:Package` output without the model wrapper or EA metadata,
      suitable for a reusable library.

## Architecture

The processing pipeline lives in the `XSplinter` project. Each service exposes an
interface (`IElementIndexer`, `IReferenceRewriter`, `IExtensionBuilder`,
`IXmiFileService`, `IXmiSplitterService`) and dependencies are passed via constructor
injection so the orchestration can be unit tested with mocks.

- **`XSplinter/Program.cs`** (`XSplinter`) — entry point. Parses args, loads/validates
  the input and config files, deserializes `SplitterConfig`, and delegates to
  `XmiSplitterService.Split(inputPath, config, outputDirectory)`. Logging via
  `Microsoft.Extensions.Logging` console logger.
- **`XSplinter/Configuration/SplitterConfig.cs`**, **`XSplinter/Configuration/PackageConfig.cs`**
  (`XSplinter.Configuration`) — the config models described above.
- **`XSplinter/Services/XmiSplitterService.cs`** (`IXmiSplitterService`) — the orchestrator.
  Loads the XMI and, per run: finds the root package by `rootPackageName` under `uml:Model`,
  locates each configured child package, builds the global element index and per-package id
  sets, maps EA connectors to packages, then for each package clones the node, rewrites
  cross-package references, and emits either a library document (plain `uml:Package`) or a
  full EA document (`uml:Model` + filtered `xmi:Extension`). It has a convenience constructor
  that wires up the concrete collaborators, plus a constructor that accepts injected
  collaborators for testing.
- **`XSplinter/Services/XmiFileService.cs`** (`IXmiFileService`) — abstracts the file-system
  interactions (`Load`, `EnsureDirectory`, `Save`) so `Split` can be tested without touching
  the disk. Registers the `CodePagesEncodingProvider` so the `windows-1252` XML declaration
  used by the outputs is honoured.
- **`XSplinter/Services/PackageEntry.cs`** — `record PackageEntry(string PackageName, string OutputFile)`;
  associates an element id with its owning package and output file.
- **`XSplinter/Services/ElementIndexer.cs`** (`IElementIndexer`) — `IndexElementIds` recursively
  maps every `xmi:id` in a package subtree to a `PackageEntry`; `CollectAllIds` gathers all
  `xmi:id` + `xmi:idref` values in a subtree (used to assign EA Extension entries).
- **`XSplinter/Services/ReferenceRewriter.cs`** (`IReferenceRewriter`) — walks a cloned package
  tree and, for `type` and `constrainedElement` nodes whose `xmi:idref` points into a
  *different* package, replaces the idref with `href="outputFile.xmi#id"`. Intra-package
  references are left untouched. `Rewrite` takes the element index as a parameter, keeping the
  service stateless.
- **`XSplinter/Services/ExtensionBuilder.cs`** (`IExtensionBuilder`) — `BuildConnectorPackageMap`
  assigns each EA connector to the package that owns both/either endpoint; `Build` produces a
  filtered `xmi:Extension` element (elements + connectors for one package, plus a stub
  `EA_PrimitiveTypes_Package`).

## Conventions

- `using` directives placed **inside** the namespace.
- Explicit `this.` qualification for instance members.
- XML-doc comments on all public types and members.
- One type per file.
- `record` for immutable data.
- Starion Group copyright file header on every file.
- Nullable reference types and ImplicitUsings enabled.

## Testing

Unit tests live in the **`XSplinter.Tests`** project (a sibling in `X-Splinter.sln`),
using **NUnit** and **Moq**.

```bash
dotnet test X-Splinter.sln
```

- One `[TestFixture]` per service (`<Sut>TestFixture`), with a `[SetUp] public void Setup()`.
- The pure transformation services (`ElementIndexer`, `ReferenceRewriter`, `ExtensionBuilder`)
  are tested directly against crafted `XElement`/`XDocument` inputs — no mocks needed.
- `XmiSplitterServiceTestFixture` drives the orchestrator with **Moq** mocks of the service
  interfaces (including `IXmiFileService`, so no disk access), asserting the wiring and the
  `convertToLibrary` vs full-EA branching. Assertions use the classic NUnit constraint model
  (`Assert.That` / `Assert.Multiple`).

For an end-to-end check, run the CLI against a representative monolithic EA XMI export with a
matching config, writing to a temporary directory, then compare each per-package output file
against a known-good baseline (byte-for-byte hash or `diff`) to catch regressions.
