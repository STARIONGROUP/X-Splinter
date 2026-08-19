# X-Splinter

**X-Splinter** is a .NET 10 command-line tool that splits a monolithic XMI export into separate, per-package XMI files. Cross-package references become cross-file `href="targetFile.xmi#id"` references, so the resulting files load independently while types still resolve across them. Built on [UML4NET](https://github.com/STARIONGROUP/uml4net).

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=coverage)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=STARIONGROUP_X-Splinter&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=STARIONGROUP_X-Splinter)

## Usage

Requires the **.NET 10 SDK**.

```bash
dotnet build X-Splinter.sln
dotnet run --project XSplinter/XSplinter.csproj -- <input.xmi> <config.json> [--output <dir>]
```

- `<input.xmi>` — the monolithic XMI file exported from Enterprise Architect.
- `<config.json>` — the splitter configuration (see below).
- `--output <dir>` — output directory (optional; defaults to the current directory).

## Configuration

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

- `rootPackageName` — *optional*; the root container package to scope the search to. Omit it when
  the packages sit directly under the model.
- `packages[]` — the packages to extract, each with a `name`, an `outputFile` and an optional
  `convertToLibrary` flag. When `true`, the package is written as a plain `uml:Package` without
  the EA model wrapper or extension metadata; when `false` (default), the full EA model
  structure is preserved.

A sample is available in [`example/packages.json`](example/packages.json).

Any UML XMI is supported. Enterprise Architect exports additionally get their `xmi:Extension`
filtered per package; the UML namespace, the `uml:Model` wrapper and the presence of an
extension are all taken from the source document.

## Build Status

Branch | Build Status
------- | :------------
Development | ![Build Status](https://github.com/STARIONGROUP/X-Splinter/actions/workflows/CodeQuality.yml/badge.svg?branch=development)

# License

X-Splinter is provided to the community under the Apache License 2.0. See the [LICENSE](LICENSE) file for the full text.
