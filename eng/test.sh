#!/usr/bin/env bash

set -euo pipefail

REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
PACKAGE_DIR="${1:-${REPOSITORY_ROOT}/artifacts/packages}"
TEST_VERSION="${NETWASM_LIBRARIES_TEST_VERSION:-$(tr -d '[:space:]' < "${REPOSITORY_ROOT}/eng/NetWasm.ReleaseVersion.txt")}"
TEST_ARTIFACTS_DIR="${NETWASM_TEST_ARTIFACTS_DIR:-${REPOSITORY_ROOT}/artifacts/test}"
mkdir -p "${PACKAGE_DIR}" "${TEST_ARTIFACTS_DIR}"
PACKAGE_DIR="$(cd "${PACKAGE_DIR}" && pwd -P)"
TEST_ARTIFACTS_DIR="$(cd "${TEST_ARTIFACTS_DIR}" && pwd -P)"
MAX_PARALLELISM="${NETWASM_TEST_MAX_PARALLELISM:-3}"
if [[ ! "${MAX_PARALLELISM}" =~ ^[1-9][0-9]*$ ]]; then
  echo "NETWASM_TEST_MAX_PARALLELISM must be a positive integer." >&2
  exit 2
fi
"${REPOSITORY_ROOT}/eng/build-packages.sh" \
  --version "${TEST_VERSION}" \
  --output "${PACKAGE_DIR}"

test_root="$(mktemp -d "${TMPDIR:-/tmp}/netwasm-libraries-test.XXXXXX")"
test_root="$(cd "${test_root}" && pwd -P)"
cleanup() {
  rm -rf "${test_root}"
}
trap cleanup EXIT

nuget_config="${test_root}/NuGet.Config"
test_logs="${test_root}/logs"
mkdir -p "${test_logs}"
xml_escape() {
  printf '%s' "$1" | sed -e 's/&/\&amp;/g' -e 's/"/\&quot;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g'
}
package_dir_xml="$(xml_escape "${PACKAGE_DIR}")"
ci_package_source_xml="$(xml_escape "${NETWASM_CI_PACKAGE_SOURCE:-}")"
{
  printf '%s\n' \
    '<?xml version="1.0" encoding="utf-8"?>' \
    '<configuration>' \
    '  <packageSources>' \
    '    <clear />' \
    "    <add key=\"current-build\" value=\"${package_dir_xml}\" />"
  if [[ -n "${NETWASM_CI_PACKAGE_SOURCE:-}" ]]; then
    printf '    <add key="ci-artifacts" value="%s" />\n' "${ci_package_source_xml}"
  fi
  printf '%s\n' \
    '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' \
    '  </packageSources>' \
    '</configuration>'
} > "${nuget_config}"

package_cache="${NUGET_PACKAGES:-${test_root}/packages}"
sdk_version="$(sed -n 's/.*"NetWasm.Sdk": "\([^"]*\)".*/\1/p' "${REPOSITORY_ROOT}/global.json")"
if [[ -z "${sdk_version}" ]]; then
  echo "Unable to read the NetWasm.Sdk version from global.json." >&2
  exit 1
fi

seed_project="${test_root}/SeedSdk.csproj"
printf '%s\n' \
  '<Project Sdk="Microsoft.NET.Sdk">' \
  '  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>' \
  "  <ItemGroup><PackageReference Include=\"NetWasm.Sdk\" Version=\"${sdk_version}\" PrivateAssets=\"all\" /></ItemGroup>" \
  '</Project>' > "${seed_project}"
NUGET_PACKAGES="${package_cache}" dotnet restore "${seed_project}" \
  --configfile "${nuget_config}" \
  --disable-build-servers \
  --nologo
export NUGET_PACKAGES="${package_cache}"

tunit_test_projects=(
  tests/NetWasm.System.Linq.Tests/NetWasm.System.Linq.Tests.csproj
  tests/NetWasm.System.Linq.AsyncEnumerable.Tests/NetWasm.System.Linq.AsyncEnumerable.Tests.csproj
  tests/NetWasm.System.Text.Encodings.Web.Tests/NetWasm.System.Text.Encodings.Web.Tests.csproj
  tests/NetWasm.System.IO.Pipelines.Tests/NetWasm.System.IO.Pipelines.Tests.csproj
  tests/NetWasm.System.Net.Http.Tests/NetWasm.System.Net.Http.Tests.csproj
  tests/NetWasm.System.Net.Http.Json.Tests/NetWasm.System.Net.Http.Json.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Primitives.Tests/NetWasm.Microsoft.Extensions.Primitives.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Configuration.Tests/NetWasm.Microsoft.Extensions.Configuration.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Caching.Memory.Tests/NetWasm.Microsoft.Extensions.Caching.Memory.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Http.Tests/NetWasm.Microsoft.Extensions.Http.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Options.Tests/NetWasm.Microsoft.Extensions.Options.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.Logging.Tests/NetWasm.Microsoft.Extensions.Logging.Tests.csproj
  tests/NetWasm.System.Text.RegularExpressions.Tests/NetWasm.System.Text.RegularExpressions.Tests.csproj
  tests/NetWasm.System.Text.Json.Tests/NetWasm.System.Text.Json.Tests.csproj
  tests/NetWasm.System.Xml.Tests/NetWasm.System.Xml.Tests.csproj
  tests/NetWasm.System.IO.Hashing.Tests/NetWasm.System.IO.Hashing.Tests.csproj
  tests/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions.CompatibilityTests/NetWasm.Microsoft.Extensions.DependencyInjection.Abstractions.CompatibilityTests.csproj
  tests/NetWasm.Microsoft.Extensions.DependencyInjection.CompatibilityTests/NetWasm.Microsoft.Extensions.DependencyInjection.CompatibilityTests.csproj
)

restore_and_run_desktop() {
  local project="$1"
  local project_name
  project_name="$(basename "${project}" .csproj)"
  local lane_artifacts="${TEST_ARTIFACTS_DIR}/${project_name}"
  local log="${test_logs}/${project_name}.desktop.log"
  (
    cd "${REPOSITORY_ROOT}"
    dotnet restore "${project}" \
      --configfile "${nuget_config}" \
      --disable-build-servers \
      --nologo \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
    dotnet run \
      --project "${project}" \
      --framework net10.0 \
      --configuration Release \
      --no-restore \
      --no-launch-profile \
      --disable-build-servers \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
  ) > "${log}" 2>&1
}

restore_and_test_netwasm() {
  local project="$1"
  local project_name
  project_name="$(basename "${project}" .csproj)"
  local lane_artifacts="${TEST_ARTIFACTS_DIR}/${project_name}"
  local log="${test_logs}/${project_name}.netwasm.log"
  (
    cd "${REPOSITORY_ROOT}"
    dotnet restore "${project}" \
      --configfile "${nuget_config}" \
      --disable-build-servers \
      --nologo \
      -p:NetWasmOptimization=None \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
    dotnet test "${project}" \
      --framework netwasm0.1 \
      --configuration Release \
      --no-restore \
      --disable-build-servers \
      --nologo \
      -p:NetWasmOptimization=None \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
  ) > "${log}" 2>&1
}

restore_and_test_desktop_xunit() {
  local project="tests/NetWasm.Microsoft.Extensions.DependencyInjection.Tests/NetWasm.Microsoft.Extensions.DependencyInjection.Tests.csproj"
  local project_name="NetWasm.Microsoft.Extensions.DependencyInjection.Tests"
  local lane_artifacts="${TEST_ARTIFACTS_DIR}/${project_name}"
  local log="${test_logs}/${project_name}.desktop.log"
  (
    cd "${REPOSITORY_ROOT}"
    dotnet restore "${project}" \
      --configfile "${nuget_config}" \
      --disable-build-servers \
      --nologo \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
    dotnet test "${project}" \
      --configuration Release \
      --no-restore \
      --disable-build-servers \
      --nologo \
      -p:UseArtifactsOutput=true \
      -p:ArtifactsPath="${lane_artifacts}"
  ) > "${log}" 2>&1
}

print_logs() {
  local log
  while IFS= read -r log; do
    printf '\n===== %s =====\n' "$(basename "${log}" .log)"
    cat "${log}"
  done < <(find "${test_logs}" -maxdepth 1 -type f -name '*.log' | LC_ALL=C sort)
}

desktop_status=0
for project in "${tunit_test_projects[@]}"; do
  restore_and_run_desktop "${project}" || desktop_status=$?
done
restore_and_test_desktop_xunit || desktop_status=$?

if [[ "${desktop_status}" -ne 0 ]]; then
  print_logs
  echo "One or more public desktop library test lanes failed." >&2
  exit 1
fi

export REPOSITORY_ROOT TEST_ARTIFACTS_DIR nuget_config test_logs
export -f restore_and_test_netwasm
test_status=0
printf '%s\0' "${tunit_test_projects[@]}" |
  xargs -0 -n 1 -P "${MAX_PARALLELISM}" bash -c \
    'set -euo pipefail; restore_and_test_netwasm "$1"' _ || test_status=$?

print_logs

if [[ "${test_status}" -ne 0 ]]; then
  echo "One or more public NetWasm library test lanes failed." >&2
  exit 1
fi
