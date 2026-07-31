# X-Splinter

**X-Splinter** is a .NET 10 command-line tool that splits a single monolithic Enterprise Architect (EA) XMI export into separate, per-package XMI files. Cross-package references are rewritten from internal `xmi:idref`s into cross-file `href="targetFile.xmi#id"` references, so the resulting files can be loaded independently while types still resolve across files (e.g. with UML4NET).

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

## Features

- Splits one EA XMI export into one output file per configured UML package.
- Rewrites cross-package references into resolvable cross-file `href`s.
- Rebuilds a filtered EA `xmi:Extension` section (elements and connectors) for each package.
- Optionally emits a package as a plain reusable `uml:Package` library (`convertToLibrary`).

## Build

Requires the **.NET 10 SDK**.

```bash
dotnet build X-Splinter.sln
```

## Usage

```bash
dotnet run --project XSplinter/XSplinter.csproj -- <input.xmi> <config.json> [--output <dir>]
```

- `<input.xmi>` — the monolithic XMI file exported from Enterprise Architect.
- `<config.json>` — the splitter configuration (see below).
- `--output <dir>` — output directory (optional; defaults to the current directory).

## Configuration

The configuration is a JSON file describing the root container package and the child packages to extract:

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
- `packages[]` — the packages to extract, each with a `name`, an `outputFile`, and an optional
  `convertToLibrary` flag. When `convertToLibrary` is `true`, the package is written as a plain
  `uml:Package` without the EA model wrapper or extension metadata; when `false` (default), the
  full EA model structure is preserved.

A sample configuration is available in [`example/packages.json`](example/packages.json).

## Build Status

GitHub actions are used to build and test the solution.

Branch | Build Status
------- | :------------
Development | ![Build Status](https://github.com/STARIONGROUP/X-Splinter/actions/workflows/CodeQuality.yml/badge.svg?branch=development)

# License

X-Splinter is provided to the community under the Apache License 2.0. See the [LICENSE](LICENSE) file for the full text.
