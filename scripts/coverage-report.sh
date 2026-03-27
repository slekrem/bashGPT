#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${ROOT_DIR}/TestResults"
COVERAGE_DIR="${ROOT_DIR}/coverage"
REPORT_DIR="${COVERAGE_DIR}/report"

cd "${ROOT_DIR}"

echo "[coverage] Cleaning old artifacts..."
rm -rf "${RESULTS_DIR}" "${COVERAGE_DIR}"
mkdir -p "${RESULTS_DIR}" "${REPORT_DIR}"

echo "[coverage] Restoring .NET dependencies..."
dotnet restore

declare -a TEST_PROJECTS=()
while IFS= read -r project; do
  TEST_PROJECTS+=("${project}")
done < <(find tests -type f -name "*.csproj" | sort)

if [[ ${#TEST_PROJECTS[@]} -eq 0 ]]; then
  echo "[coverage] No test projects found under tests/."
  exit 1
fi

echo "[coverage] Running tests with OpenCover output..."
for project in "${TEST_PROJECTS[@]}"; do
  echo "[coverage] dotnet test ${project}"
  dotnet test "${project}" \
    --nologo \
    --collect:"XPlat Code Coverage;Format=opencover" \
    --results-directory "${RESULTS_DIR}"
done

declare -a COVERAGE_FILES=()
while IFS= read -r file; do
  COVERAGE_FILES+=("${file}")
done < <(find "${RESULTS_DIR}" -type f -name "coverage.opencover.xml" | sort)

if [[ ${#COVERAGE_FILES[@]} -eq 0 ]]; then
  echo "[coverage] No coverage.opencover.xml files were generated."
  exit 1
fi

if [[ ! -f "${ROOT_DIR}/.config/dotnet-tools.json" ]]; then
  echo "[coverage] Initializing local dotnet tool manifest..."
  dotnet new tool-manifest
fi

if ! dotnet tool list --local | grep -q "dotnet-reportgenerator-globaltool"; then
  echo "[coverage] Installing ReportGenerator tool..."
  dotnet tool install dotnet-reportgenerator-globaltool --local --version "5.*"
fi

echo "[coverage] Restoring local dotnet tools..."
dotnet tool restore

# Convert a path to the native format expected by .NET tools.
# On Windows (Git Bash / MSYS2) cygpath translates POSIX → Windows paths.
# On Linux/macOS the function is a no-op.
to_native_path() {
  if command -v cygpath &>/dev/null; then
    cygpath -w "$1"
  else
    echo "$1"
  fi
}

declare -a NATIVE_COVERAGE_FILES=()
for f in "${COVERAGE_FILES[@]}"; do
  NATIVE_COVERAGE_FILES+=("$(to_native_path "${f}")")
done

REPORTS_INPUT="$(printf '%s;' "${NATIVE_COVERAGE_FILES[@]}")"
REPORTS_INPUT="${REPORTS_INPUT%;}"

NATIVE_REPORT_DIR="$(to_native_path "${REPORT_DIR}")"

echo "[coverage] Generating HTML report..."
dotnet tool run reportgenerator \
  "-reports:${REPORTS_INPUT}" \
  "-targetdir:${NATIVE_REPORT_DIR}" \
  "-reporttypes:Html;HtmlSummary;TextSummary" \
  "-riskhotspotassemblyfilters:+*" \
  "-riskhotspotclassfilters:+*" \
  "-title:bashGPT Coverage Report"

echo
if [[ -f "${REPORT_DIR}/Summary.txt" ]]; then
  echo "[coverage] Summary:"
  cat "${REPORT_DIR}/Summary.txt"
  echo
fi

echo "[coverage] HTML report: ${REPORT_DIR}/index.html"
