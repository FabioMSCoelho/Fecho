"use strict";
document.querySelectorAll("[data-auto-submit]").forEach(element => element.addEventListener("change", () => element.form.requestSubmit()));
document.querySelectorAll("form[data-confirm]").forEach(form => form.addEventListener("submit", event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
document.querySelectorAll("[data-password]").forEach(button => button.addEventListener("click", () => {
    const input = document.getElementById(button.dataset.password);
    const mostrar = input.type === "password";
    input.type = mostrar ? "text" : "password";
    button.textContent = mostrar ? "Ocultar" : "Mostrar";
    button.setAttribute("aria-label", mostrar ? "Ocultar senha" : "Mostrar senha");
}));
document.querySelectorAll("[data-dismiss]").forEach(button => button.addEventListener("click", () => button.closest(".notice").remove()));
document.querySelectorAll("input[data-preview]").forEach(input => {
    let anterior;
    input.addEventListener("change", () => {
        const arquivo = input.files[0];
        if (!arquivo) return;
        if (anterior) URL.revokeObjectURL(anterior);
        anterior = URL.createObjectURL(arquivo);
        document.getElementById(input.dataset.preview).src = anterior;
    });
});
document.addEventListener("error", event => {
    if (event.target instanceof HTMLImageElement && !event.target.src.endsWith("/arte/objeto.svg")) event.target.src = "/arte/objeto.svg";
}, true);
