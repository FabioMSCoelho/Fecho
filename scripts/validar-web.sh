#!/usr/bin/env bash
set -euo pipefail
raiz="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
[[ "${1:-}" == "--no-build" ]] || bash scripts/fecho.sh build
if command -v dotnet >/dev/null 2>&1; then dotnet_exe="$(command -v dotnet)"; else dotnet_exe="$raiz/.tools/dotnet/dotnet"; fi
export FECHO_TEST_WEB_URL="${FECHO_TEST_WEB_URL:-http://127.0.0.1:5071}"
if [[ ! "$FECHO_TEST_WEB_URL" =~ ^http://(127\.0\.0\.1|localhost):[0-9]+$ ]]; then
    printf '%s\n' 'Use uma URL de teste local, como http://127.0.0.1:5071.' >&2
    exit 2
fi
if curl --silent --max-time 1 "$FECHO_TEST_WEB_URL/saude" >/dev/null; then
    printf '%s\n' 'A porta de teste já responde. Escolha outra porta; nenhum servidor existente será usado.' >&2
    exit 1
fi
export ASPNETCORE_URLS="$FECHO_TEST_WEB_URL"
export ASPNETCORE_ENVIRONMENT=Production
mkdir -p .tools/logs
arquivo_log="$(mktemp "$raiz/.tools/logs/web-test-XXXXXX.log")"
(
    cd "$raiz/src/Fecho.Web"
    exec "$dotnet_exe" bin/Release/net10.0/Fecho.Web.dll
) > "$arquivo_log" 2>&1 &
processo_web=$!
limpar() { kill "$processo_web" 2>/dev/null || true; wait "$processo_web" 2>/dev/null || true; }
trap limpar EXIT INT TERM
pronto=false
for tentativa in {1..40}; do
    if ! kill -0 "$processo_web" 2>/dev/null; then break; fi
    if curl --fail --silent "$FECHO_TEST_WEB_URL/saude" >/dev/null; then pronto=true; break; fi
    sleep 0.5
done
if [[ "$pronto" == false ]]; then
    printf 'Servidor de teste não iniciou. Consulte: %s\n' "$arquivo_log" >&2
    exit 1
fi
"$dotnet_exe" "$raiz/tests/Fecho.Web.Testes/bin/Release/net10.0/Fecho.Web.Testes.dll"
