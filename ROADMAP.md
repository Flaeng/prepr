# Todo

- [ ] Function/method length rule — Similar to file line limits but scoped to individual methods. Long methods are a common code smell prepr doesn't yet catch.
  Targets C# only initially, detecting method boundaries via brace-matching or syntax heuristics. Reports methods exceeding a configurable line threshold with Low/Medium/High severity levels, reusing the same threshold model as existing rules. Configurable via `.preprrc` with a default max method length.

- [ ] SARIF output format — SARIF is the standard for static analysis results. GitHub, Azure DevOps, and VS Code all consume it natively, giving prepr immediate IDE/PR integration.
  Adds a new `--output sarif` option that produces SARIF v2.1.0 JSON output. Each rule violation maps to a SARIF `result` with location, message, and severity. This enables direct upload to GitHub Code Scanning, Azure DevOps, and VS Code's SARIF Viewer extension without any conversion step.

- [x] Comment density rule — Flag files with very low comment-to-code ratios.
  Implemented as its own independently toggleable rule. Counts single-line and block comments relative to total code lines and reports files below a configurable minimum ratio. Useful for catching under-documented areas of a codebase.

- [ ] Magic number detection — Flag hardcoded numeric literals that should be named constants.
  A separate rule from comment density, independently configurable. Scans for raw numeric literals outside of common safe contexts (0, 1, array indices, etc.) and reports them with file location. Helps enforce the "no magic numbers" coding guideline.

- [ ] Magic string detection — Flag repeated hardcoded string literals that should be named constants.
  Companion rule to magic number detection, independently configurable. Detects string literals that appear more than a configurable number of times across a file or project, excluding common safe values (empty string, single characters, etc.). Reports each occurrence with file location to help enforce consistent use of named constants.

- [ ] Watch mode — `--watch` to re-scan on file changes during development, showing live feedback in the terminal.
  Uses a file system watcher to detect changes, then performs incremental re-scans of only the modified files (leveraging the existing `ScanCache`). Clears the terminal between scans for a clean view. Supports all existing output formats and exits cleanly on Ctrl+C.

