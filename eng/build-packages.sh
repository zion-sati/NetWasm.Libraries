#!/usr/bin/env bash

set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
OUTPUT_DIR="${REPOSITORY_ROOT}/artifacts/packages"
RELEASE_VERSION="$(tr -d '[:space:]' < "${REPOSITORY_ROOT}/eng/NetWasm.ReleaseVersion.txt")"
output_was_set=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --output)
      [[ $# -ge 2 ]] || { echo "--output requires a directory." >&2; exit 2; }
      OUTPUT_DIR="$2"
      output_was_set=true
      shift 2
      ;;
    --version)
      [[ $# -ge 2 ]] || { echo "--version requires a value." >&2; exit 2; }
      RELEASE_VERSION="$2"
      shift 2
      ;;
    --help|-h)
      echo "Usage: $0 [--version VERSION] [--output DIRECTORY] [DIRECTORY]"
      exit 0
      ;;
    --*)
      echo "Unknown option: $1" >&2
      exit 2
      ;;
    *)
      if [[ "${output_was_set}" == true ]]; then
        echo "Package output was specified more than once." >&2
        exit 2
      fi
      OUTPUT_DIR="$1"
      output_was_set=true
      shift
      ;;
  esac
done
mkdir -p "${OUTPUT_DIR}"
OUTPUT_DIR="$(cd "${OUTPUT_DIR}" && pwd -P)"

case "${OUTPUT_DIR}" in
  "${REPOSITORY_ROOT}/artifacts"|"${REPOSITORY_ROOT}/artifacts"/*)
    ;;
  "${REPOSITORY_ROOT}"|"${REPOSITORY_ROOT}"/*)
    echo "Package output inside the repository must stay under artifacts/: ${OUTPUT_DIR}" >&2
    exit 2
    ;;
esac

find "${OUTPUT_DIR}" -maxdepth 1 -type f \
  \( -name 'NetWasm.*.nupkg' -o -name 'NetWasm.*.snupkg' \) -delete

build_root="$(mktemp -d "${TMPDIR:-/tmp}/netwasm-libraries-build.XXXXXX")"
build_root="$(cd "${build_root}" && pwd -P)"
source_root="${build_root}/source"
cleanup() {
  if [[ -d "${source_root}" ]]; then
    git -C "${REPOSITORY_ROOT}" worktree remove --force "${source_root}" >/dev/null 2>&1 || true
  fi
  rm -rf "${build_root}"
}
trap cleanup EXIT

for command in dotnet git python3; do
  command -v "${command}" >/dev/null 2>&1 || {
    echo "Missing required command: ${command}" >&2
    exit 2
  }
done

[[ "$(git -C "${REPOSITORY_ROOT}" rev-parse --is-inside-work-tree 2>/dev/null || true)" == "true" ]] || {
  echo "Package construction requires a Git checkout." >&2
  exit 2
}
[[ -z "$(git -C "${REPOSITORY_ROOT}" status --porcelain=v1)" ]] || {
  echo "Package construction requires a clean checkout." >&2
  exit 2
}

repository_commit="$(git -C "${REPOSITORY_ROOT}" rev-parse HEAD)"
git -C "${REPOSITORY_ROOT}" worktree add --quiet --detach "${source_root}" "${repository_commit}"
python3 "${source_root}/eng/project-release-version.py" \
  --source-root "${source_root}" \
  --version "${RELEASE_VERSION}" \
  --receipt "${OUTPUT_DIR}/NetWasm.Libraries.release-version-projection.json"
# global.json discovery follows the process working directory, not an absolute
# project argument. Anchor every dotnet invocation to the detached source.
cd "${source_root}"

nuget_config="${build_root}/NuGet.Config"
xml_escape() {
  printf '%s' "$1" | sed -e 's/&/\&amp;/g' -e 's/"/\&quot;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g'
}
output_dir_xml="$(xml_escape "${OUTPUT_DIR}")"
ci_package_source_xml="$(xml_escape "${NETWASM_CI_PACKAGE_SOURCE:-}")"
{
  printf '%s\n' \
    '<?xml version="1.0" encoding="utf-8"?>' \
    '<configuration>' \
    '  <packageSources>' \
    '    <clear />' \
    "    <add key=\"current-build\" value=\"${output_dir_xml}\" />"
  if [[ -n "${NETWASM_CI_PACKAGE_SOURCE:-}" ]]; then
    printf '    <add key="ci-artifacts" value="%s" />\n' "${ci_package_source_xml}"
  fi
  printf '%s\n' \
    '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' \
    '  </packageSources>' \
    '</configuration>'
} > "${nuget_config}"

package_cache="${NUGET_PACKAGES:-${build_root}/packages}"
sdk_version="$(sed -n 's/.*"NetWasm.Sdk": "\([^"]*\)".*/\1/p' "${source_root}/global.json")"
if [[ -z "${sdk_version}" ]]; then
  echo "Unable to read the NetWasm.Sdk version from global.json." >&2
  exit 1
fi

seed_project="${build_root}/SeedSdk.csproj"
printf '%s\n' \
  '<Project Sdk="Microsoft.NET.Sdk">' \
  '  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>' \
  "  <ItemGroup><PackageReference Include=\"NetWasm.Sdk\" Version=\"${sdk_version}\" PrivateAssets=\"all\" /></ItemGroup>" \
  '</Project>' > "${seed_project}"
NUGET_PACKAGES="${package_cache}" dotnet restore "${seed_project}" \
  --configfile "${nuget_config}" \
  --disable-build-servers \
  --nologo

root_projects=(
  src/NetWasm.System.Linq/NetWasm.System.Linq.csproj
  src/NetWasm.System.Memory/NetWasm.System.Memory.csproj
  src/NetWasm.System.Text.Encodings.Web/NetWasm.System.Text.Encodings.Web.csproj
  src/NetWasm.System.Net.Http/NetWasm.System.Net.Http.csproj
  src/NetWasm.System.Text.RegularExpressions/NetWasm.System.Text.RegularExpressions.csproj
  src/NetWasm.System.Xml/NetWasm.System.Xml.csproj
  src/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions.csproj
)

dependent_projects=(
  src/NetWasm.System.Linq.AsyncEnumerable/NetWasm.System.Linq.AsyncEnumerable.csproj
  src/NetWasm.System.IO.Pipelines/NetWasm.System.IO.Pipelines.csproj
  src/NetWasm.System.IO.Hashing/NetWasm.System.IO.Hashing.csproj
  src/NetWasm.System.Text.Json/NetWasm.System.Text.Json.csproj
  src/NetWasm.Microsoft.Extensions.DependencyInjection/NetWasm.Microsoft.Extensions.DependencyInjection.csproj
  src/NetWasm.Microsoft.Extensions.Options/NetWasm.Microsoft.Extensions.Options.csproj
  src/NetWasm.Microsoft.Extensions.Logging.Abstractions/NetWasm.Microsoft.Extensions.Logging.Abstractions.csproj
  src/NetWasm.Microsoft.Extensions.Logging/NetWasm.Microsoft.Extensions.Logging.csproj
)

pack_project() {
  local project="$1"
  NUGET_PACKAGES="${package_cache}" \
    dotnet pack "${source_root}/${project}" \
      -c Release \
      --configfile "${nuget_config}" \
      --nologo \
      -o "${OUTPUT_DIR}" \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${build_root}/artifacts"
}

for project in "${root_projects[@]}"; do
  pack_project "${project}"
done
for project in "${dependent_projects[@]}"; do
  pack_project "${project}"
done

package_count="$(find "${OUTPUT_DIR}" -maxdepth 1 -type f -name 'NetWasm.*.nupkg' | wc -l | tr -d ' ')"
if [[ "${package_count}" -ne 15 ]]; then
  echo "Expected exactly 15 NetWasm library packages, found ${package_count}." >&2
  exit 1
fi

echo "Built 15 NetWasm library packages at ${RELEASE_VERSION} in ${OUTPUT_DIR}"
