# Project Guidelines

## Overview

prepr is a .NET 10.0 CLI tool that detects and reports duplicated code blocks across files in a directory tree. It uses `System.CommandLine` for CLI parsing and supports multiple output formats (console, HTML, Markdown, CSV, prompt).

## Architecture

- **Program.cs** — Entry point, CLI option definitions, config merging (CLI > config file > defaults), orchestration
- **Models.cs** — Immutable domain records (`DuplicateBlock`, `ScanResult`, `FileDuplicationInfo`, `SummaryStatistics`, etc.)
- **FileDiscovery.cs** — Directory traversal with include/exclude glob filtering
- **DuplicateDetector.cs** — Core duplicate-line detection algorithm
- **RuleChecker.cs** — Orchestrator for duplication and line-limit rules; integrates caching
- **ScanCache.cs** — Incremental scanning cache to skip unchanged files
- **Reporters/** — Output format implementations behind `IReporter` interface

## Code Style

- Use C# records for immutable data types
- Nullable reference types are enabled project-wide
- Implicit usings are enabled
- Normalize file paths with forward slashes for cross-platform consistency

## Terminal Usage

- Use relative paths instead of absolute paths when changing directories
- Run one command at a time; only pipe or chain commands when genuinely necessary

## Build and Test

```bash
dotnet build prepr/prepr.csproj
dotnet test prepr.Tests/prepr.Tests.csproj
dotnet run --project prepr -- <path> [options]
```

## Conventions

- **Config hierarchy:** CLI args override `.preprrc` config file, which overrides built-in defaults
- **Exit codes:** 0 = success, 1 = config/directory error, 2 = thresholds exceeded
- **Severity levels:** Low/Medium/High based on configurable duplication percentage thresholds
- **Package versions** are centrally managed in `Directory.Packages.props`
- **Shared build properties** live in `Directory.Build.props`
- Tests use xUnit; each core component and reporter has a corresponding test file in `prepr.Tests/`
