#!/usr/bin/env bash
#
# Prints a Markdown summary of a Playwright JSON report: time per project and the slowest tests.
#
# Usage: ci-timings.sh <results.json>
#
set -euo pipefail

results="$1"

jq -r '
  def fmt(ms): ms / 1000 | floor | "\(. / 60 | floor)m \(. % 60)s";

  [.suites[] | .. | objects | select(has("specs")) | .specs[] as $spec | $spec.tests[] | {
    project: .projectName,
    test: "\($spec.file):\($spec.line) \($spec.title)",
    ms: ([.results[].duration] | add // 0)
  }] as $tests

  | "### Time per project",
    "",
    "| Project | Tests | Time |",
    "|---|---:|---:|",
    ($tests | group_by(.project) | map({project: .[0].project, count: length, ms: (map(.ms) | add)})
      | sort_by(-.ms)[] | "| \(.project) | \(.count) | \(fmt(.ms)) |"),
    "",
    "### Slowest tests",
    "",
    "| Test | Project | Time |",
    "|---|---|---:|",
    ($tests | sort_by(-.ms) | .[:10][] | "| `\(.test)` | \(.project) | \(fmt(.ms)) |")
' "$results"
