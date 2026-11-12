"use strict";
(() => {
    if (!document.querySelector("[data-discovery]")) return;
    const feedback = document.getElementById("discovery-feedback");
    const dialog = document.getElementById("discovery-match");
    let busy = false;

    function avisar(mensagem, erro = false, recarregar = false) {
        feedback.textContent = mensagem;
        feedback.classList.toggle("is-error", erro);
        if (recarregar) {
            const link = document.createElement("a");
            link.href = window.location.href;
            link.textContent = "Recarregar objetos";
            feedback.append(link);
        }
    }

    function mostrarMatch(match) {
        if (!match || !dialog) return;
        dialog.querySelector("[data-match-person]").textContent = match.pessoa;
        for (const [prefixo, objeto] of [["own", match.seuObjeto], ["other", match.outroObjeto]]) {
            const img = dialog.querySelector(`[data-match-${prefixo}-image]`);
            img.src = objeto.imagem;
            img.alt = objeto.nome;
            dialog.querySelector(`[data-match-${prefixo}-name]`).textContent = objeto.nome;
        }
        dialog.querySelector("[data-match-link]").href = `/matches/${match.id}`;
        dialog.showModal();
    }

    async function avaliar(form, tipo) {
        if (busy) return;
        busy = true;
        const root = document.querySelector("[data-discovery]");
        const card = root.querySelector("[data-swipe-card]");
        const dados = new FormData(form);
        dados.set("tipo", tipo);
        root.setAttribute("aria-busy", "true");
        form.querySelectorAll("button").forEach(b => b.disabled = true);
        let salvo = false;
        try {
            const resposta = await fetch(form.action, {
                method: "POST", body: dados, headers: { Accept: "application/json" }, credentials: "same-origin"
            });
            if (resposta.redirected || resposta.status === 401) {
                window.location.assign(`/conta/entrar?retorno=${encodeURIComponent(location.pathname + location.search + "#achados")}`);
                return;
            }
            if (!resposta.headers.get("Content-Type")?.includes("application/json"))
                throw new Error("Não foi possível confirmar sua escolha. Recarregue os objetos e tente novamente.");
            const resultado = await resposta.json();
            if (!resposta.ok) throw new Error(resultado.mensagem || "Não foi possível salvar sua escolha.");
            salvo = true;
            avisar(resultado.mensagem);
            card.style.setProperty("--drag-x", tipo === "LIKE" ? "120%" : "-120%");
            card.style.setProperty("--drag-rotate", tipo === "LIKE" ? "16deg" : "-16deg");
            card.classList.add("is-leaving");
            const proxima = await fetch(root.dataset.reload, { credentials: "same-origin", cache: "no-store" });
            if (!proxima.ok || proxima.redirected) throw new Error("Sua escolha foi salva. Recarregue para ver o próximo objeto.");
            const pagina = new DOMParser().parseFromString(await proxima.text(), "text/html");
            const novo = pagina.querySelector("[data-discovery]");
            if (!novo) throw new Error("Sua escolha foi salva. Recarregue para continuar descobrindo.");
            if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches)
                await new Promise(resolve => setTimeout(resolve, 220));
            root.replaceWith(novo);
            const categorias = pagina.querySelector("#categoria");
            if (categorias) document.getElementById("categoria").replaceWith(categorias);
            prepararCard();
            novo.querySelector("[data-swipe-card], [data-discovery-empty]")?.focus({ preventScroll: true });
            mostrarMatch(resultado.match);
        } catch (erro) {
            card?.classList.remove("is-leaving");
            limparArrasto(card);
            avisar(erro instanceof Error && erro.message !== "Failed to fetch" ? erro.message
                : "A conexão falhou. Recarregue os objetos para conferir se sua escolha foi salva.", true, true);
            // Após confirmar a gravação, não permita repetir a escolha no card antigo.
            if (!salvo) form.querySelectorAll("button").forEach(b => b.disabled = false);
        } finally {
            root.removeAttribute("aria-busy");
            busy = false;
        }
    }

    function limparArrasto(card) {
        if (!card) return;
        card.classList.remove("is-dragging");
        for (const propriedade of ["--drag-x", "--drag-rotate", "--like-opacity", "--pass-opacity"])
            card.style.removeProperty(propriedade);
    }

    function prepararCard() {
        const root = document.querySelector("[data-discovery]");
        const card = root.querySelector("[data-swipe-card]");
        const form = root.querySelector("[data-discovery-form]");
        if (!card || !form) return;
        form.addEventListener("submit", event => {
            event.preventDefault();
            const tipo = event.submitter?.value;
            if (tipo === "LIKE" || tipo === "DISLIKE") avaliar(form, tipo);
        });
        let arrasto = null;
        card.addEventListener("pointerdown", event => {
            if (busy || !event.isPrimary || event.button !== 0) return;
            arrasto = { id: event.pointerId, x: event.clientX, y: event.clientY, dx: 0, horizontal: false };
        });
        card.addEventListener("pointermove", event => {
            if (!arrasto || event.pointerId !== arrasto.id || busy) return;
            const dx = event.clientX - arrasto.x;
            const dy = event.clientY - arrasto.y;
            if (!arrasto.horizontal) {
                if (Math.abs(dy) > Math.abs(dx) && Math.abs(dy) > 12) { arrasto = null; return; }
                if (Math.abs(dx) < 10) return;
                arrasto.horizontal = true;
                card.setPointerCapture(event.pointerId);
                card.classList.add("is-dragging");
            }
            arrasto.dx = dx;
            card.style.setProperty("--drag-x", `${dx}px`);
            card.style.setProperty("--drag-rotate", `${Math.max(-18, Math.min(18, dx / 18))}deg`);
            card.style.setProperty("--like-opacity", String(Math.max(0, Math.min(1, dx / 90))));
            card.style.setProperty("--pass-opacity", String(Math.max(0, Math.min(1, -dx / 90))));
        });
        function terminar(event) {
            if (!arrasto || event.pointerId !== arrasto.id) return;
            const dx = arrasto.dx;
            const confirmar = event.type === "pointerup" && arrasto.horizontal
                && Math.abs(dx) >= Math.min(100, card.offsetWidth * .25);
            arrasto = null;
            limparArrasto(card);
            if (confirmar) form.requestSubmit(form.querySelector(`[value="${dx > 0 ? "LIKE" : "DISLIKE"}"]`));
        }
        card.addEventListener("pointerup", terminar);
        card.addEventListener("pointercancel", terminar);
    }

    document.addEventListener("keydown", event => {
        if (busy || dialog?.open || event.repeat || event.altKey || event.ctrlKey || event.metaKey
            || !["ArrowLeft", "ArrowRight"].includes(event.key)
            || event.target.closest("input, select, textarea, button, a, summary, [contenteditable]")) return;
        const form = document.querySelector("[data-discovery-form]");
        if (!form) return;
        event.preventDefault();
        form.requestSubmit(form.querySelector(`[value="${event.key === "ArrowRight" ? "LIKE" : "DISLIKE"}"]`));
    });
    dialog?.querySelectorAll("[data-close-match]").forEach(b => b.addEventListener("click", () => dialog.close()));
    prepararCard();
})();
