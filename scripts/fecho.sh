#!/usr/bin/env bash
set -euo pipefail

raiz="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"

if command -v dotnet >/dev/null 2>&1; then
    dotnet_exe="$(command -v dotnet)"
elif [[ -x "$raiz/.tools/dotnet/dotnet" ]]; then
    dotnet_exe="$raiz/.tools/dotnet/dotnet"
else
    printf '%s\n' 'Instale o SDK .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0' >&2
    exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$raiz/.tools/cli-home}"

comando="${1:-demo}"
case "$comando" in
    build) projeto="Fecho.slnx" ;;
    demo) projeto="src/Fecho.Console/Fecho.Console.csproj" ;;
    test) projeto="tests/Fecho.Testes/Fecho.Testes.csproj" ;;
    *) printf '%s\n' 'Uso: bash scripts/fecho.sh [demo|test|build]' >&2; exit 2 ;;
esac

"$dotnet_exe" build "$projeto" --configuration Release --nologo --disable-build-servers -m:1
case "$comando" in
    demo) exec "$dotnet_exe" src/Fecho.Console/bin/Release/net10.0/Fecho.Console.dll ;;
    test) exec "$dotnet_exe" tests/Fecho.Testes/bin/Release/net10.0/Fecho.Testes.dll ;;
esac
