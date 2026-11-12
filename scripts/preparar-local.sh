#!/usr/bin/env bash
set -euo pipefail
raiz="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"
if [[ "${1:-}" != "" && "${1:-}" != "--demo" ]]; then
    printf '%s\n' 'Uso: bash scripts/preparar-local.sh [--demo]' >&2
    exit 2
fi
if [[ ! -f .env ]]; then
    command -v openssl >/dev/null || { printf '%s\n' 'Instale OpenSSL para gerar as senhas locais.' >&2; exit 1; }
    (
        umask 077
        {
        printf '%s\n' 'FECHO_DB_HOST=127.0.0.1' 'FECHO_DB_PORT=3306' 'FECHO_DB_NAME=fecho' 'FECHO_DB_USER=fecho'
        printf 'FECHO_DB_PASSWORD=%s\n' "$(openssl rand -hex 24)"
        printf 'MYSQL_ROOT_PASSWORD=%s\n' "$(openssl rand -hex 24)"
        printf 'FECHO_DEMO_PASSWORD=%s\n' "$(openssl rand -hex 12)"
        } > .env
    )
    printf '%s\n' 'Configuração .env criada. As senhas são locais e o arquivo é ignorado pelo Git.'
fi
if [[ "${1:-}" == "--demo" ]]; then
    tem_senha_demo=false
    while IFS= read -r linha || [[ -n "$linha" ]]; do
        chave="${linha%%=*}"
        if [[ "${chave//[[:space:]]/}" == FECHO_DEMO_PASSWORD ]]; then
            valor="${linha#*=}"
            valor="${valor//[[:space:]]/}"
            if [[ -n "$valor" && "$valor" != "''" && "$valor" != '""' ]]; then tem_senha_demo=true; else tem_senha_demo=false; fi
        fi
    done < .env
    if [[ "$tem_senha_demo" == false && -z "${FECHO_DEMO_PASSWORD:-}" ]]; then
        printf '\nFECHO_DEMO_PASSWORD=%s\n' "$(openssl rand -hex 12)" >> .env
    fi
fi
if ! docker compose up -d --wait; then
    printf '%s\n' \
        'Não foi possível iniciar o MySQL do Docker.' \
        'Se a porta 3306 estiver ocupada, escolha uma porta livre no .env, por exemplo FECHO_DB_PORT=3307, e execute novamente.' \
        'O volume existente é mantido. Não use docker compose down -v para corrigir a conexão.' >&2
    exit 1
fi
if [[ "${1:-}" == "--demo" ]]; then
    if ! bash scripts/fecho.sh seed; then
        printf '%s\n' \
            'O MySQL do Docker iniciou, mas a demonstração não foi preparada.' \
            'Confira se FECHO_DB_HOST, FECHO_DB_PORT, FECHO_DB_NAME, FECHO_DB_USER e FECHO_DB_PASSWORD apontam para esse banco.' \
            'Variáveis exportadas no terminal prevalecem sobre o .env. O volume existente foi mantido.' >&2
        exit 1
    fi
    printf '%s\n' 'As contas de exemplo usam a senha FECHO_DEMO_PASSWORD do seu .env.'
fi
printf '%s\n' 'Banco preparado. Execute: bash scripts/fecho.sh web' 'Abra: http://localhost:5070'
