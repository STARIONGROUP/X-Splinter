# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Overview

**X-Splinter** is a .NET 10 CLI that splits a monolithic XMI export into one XMI file per
UML package, rewriting cross-package references into cross-file `href="targetFile.xmi#id"`
so the pieces still resolve against each other.

Any UML XMI is supported. Enterprise Architect (EA) is the primary case and gets extra
handling — the EA `xmi:Extension` is filtered per package — but nothing EA-specific is
assumed: the UML namespace, the `uml:Model` wrapper and the presence of an extension are
all taken from the source document.

## Build & Run

Requires the **.NET 10 SDK**.

```bash
dotnet build X-Splinter.sln
dotnet run --project XSplinter/XSplinter.csproj -- <input.xmi> <config.json> [--output <dir>]
```

`--output` defaults to `.`. Exit code `0` on success, `1` on error.

## Configuration

JSON, deserialized into `SplitterConfig` (case-insensitive). Sample: `example/packages.json`
— a *splitter config*, **not** an npm file.

```json
{
  "rootPackageName": "5. Data Structure",
  "packages": [
    { "name": "Primitives", "outputFile": "CSharp_Primitives.xmi", "convertToLibrary": true },
    { "name": "Forge", "outputFile": "Forge.xmi" }
  ]
}
```

- `rootPackageName` — *optional* root container package to scope the search to. Omit it when
  the packages sit directly under the model.
- `modelName` — *optional* name for the `uml:Model` wrapper. Defaults to the name of the model
  enclosing the package in the source. Not used for `convertToLibrary` packages.
- `packages[]` — `name`, `outputFile`, and optional `convertToLibrary` (default `false`):
  - `false` → the `uml:Model` wrapper (named by `modelName`, else the source's model name)
    plus the filtered `xmi:Extension`. No wrapper is written if the source had none.
  - `true` → plain `uml:Package`, no model wrapper or extension.

## Architecture

Reading and writing go through **UML4NET** (`uml4net.xmi`,
`uml4net.xmi.Extensions.EnterpriseArchitect`) — the tool works on the UML object model,
not raw XML.

**Key mechanism:** cross-package references are not rewritten by hand. UML4NET resolves an
external reference as `{DocumentName}#{XmiId}`, so each package subtree is stamped with the
file it will be written to and UML4NET emits the `href` itself
(`ExternalReferenceResolution = Href`).

Every service has an interface and is constructor-injected, so the orchestration is
mockable.

| File | Role |
| --- | --- |
| `Program.cs` | Entry point: parses args, deserializes the config, calls `Split`. |
| `Configuration/SplitterConfig.cs`, `PackageConfig.cs` | Config models. |
| `Services/XmiSplitterService.cs` | Orchestrator: load → find packages (recursively, optionally scoped by `rootPackageName`) → stamp → map connectors → write. Names the `uml:Model` wrapper from `modelName`, else mirrors the source's. Has a convenience ctor and an injectable ctor. |
| `Services/XmiModelLoader.cs` | UML4NET read, with the EA extender + extension content reader (inert for non-EA files). Also detects the source's UML namespace so output matches the input's UML version. |
| `Services/PackageDocumentAssigner.cs` | Stamps `IXmiElement.DocumentName`. Traverses only `AggregationKind.Composite` properties (from UML4NET `[Property]` metadata) so *referenced* elements keep pointing at their own document. Returns contained + referenced ids. |
| `Services/ExtensionBuilder.cs` | Filters the EA extension per package, deciding membership from the parsed EA `Element`/`Connector` and their resolved `ExtendedElement`. `CanFilter` is false for a foreign extension, which is then copied unchanged into every output. |
| `Services/XmiDocumentWriter.cs` | UML4NET write + `EnsureDirectory`. |
| `Services/NonDisposingLoggerFactory.cs` | UML4NET scopes register the supplied `ILoggerFactory` without marking it externally owned, so disposing a scope would dispose the caller's factory. |

### Gotchas

- **`System.Xml.Linq` survives in `ExtensionBuilder.SliceContent`/`ParseContent` only.**
  `XmiExtensionWriter` emits `ContentRawXmi` verbatim and ignores the parsed `Content`, and
  UML4NET has no EA extension *writer*, so entries are sliced out of the raw markup. The
  reader declares the `xmi` prefix on each entry, which is what keeps `xmi:idref` intact when
  an entry is detached — the synthesized `EA_PrimitiveTypes_Package` stub declares it
  explicitly for the same reason.
- **Output is not byte-identical to EA.** Model, ids, EA extension entries, the
  `xmi:Documentation` header and `href` targets all match, but expect `utf-8` instead of
  `windows-1252` (UML4NET hardcodes it), different attribute order, `type` as an attribute
  rather than a nested `<type>`, and defaults such as `Generalization::isSubstitutable`
  elided. UML4NET also promotes EA `<documentation>` into UML `ownedComment`.

## Conventions

- `using` directives **inside** the namespace; explicit `this.`; one type per file.
- XML-doc comments on all public types and members.
- `record` for immutable data.
- Starion Group copyright header + `SPDX-License-Identifier: Apache-2.0` on every file.
- `Nullable` is **disabled** — reference types are nullable by nature, so no `?`/`!` annotations.

## Testing

**NUnit** + **Moq** in `XSplinter.Tests` (sibling project in the solution).

```bash
dotnet test X-Splinter.sln
```

- One `[TestFixture]` per service (`<Sut>TestFixture`) with `[SetUp] public void Setup()`.
- `ExtensionBuilder` and `PackageDocumentAssigner` are tested directly against constructed
  UML4NET objects; `XmiSplitterServiceTestFixture` mocks the service interfaces so no disk
  access occurs. Classic constraint model (`Assert.That` / `Assert.Multiple`).
- End-to-end: run the CLI against a real EA export into a temp directory and compare the
  id sets, `href` values and EA element/connector ids against a known-good baseline.
