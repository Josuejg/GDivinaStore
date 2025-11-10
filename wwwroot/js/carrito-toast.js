// ========== Toast del carrito con imagen y nombre ==========

(function () {
    window.mostrarToast = function (mensaje, tipo = "success", imgUrl = null) {
        const colorClase = {
            success: "text-bg-success",
            danger: "text-bg-danger",
            info: "text-bg-info",
            warning: "text-bg-warning"
        }[tipo] || "text-bg-success";

        let toastEl = document.getElementById("toastCarrito");
        if (!toastEl) {
            toastEl = document.createElement("div");
            toastEl.id = "toastCarrito";
            toastEl.className = `toast align-items-center ${colorClase} border-0 position-fixed end-0 m-4 toast-cart`;
            toastEl.style.zIndex = "1100";
            toastEl.innerHTML = `
                <div class="d-flex align-items-center">
                    <img class="toast-img rounded me-3 d-none" alt="producto" />
                    <div class="toast-body fw-semibold"></div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto"
                            data-bs-dismiss="toast" aria-label="Cerrar"></button>
                </div>`;
            document.body.appendChild(toastEl);
        }

        // Aplica color según tipo
        toastEl.className = `toast align-items-center ${colorClase} border-0 position-fixed end-0 m-4 toast-cart`;

        const toastBody = toastEl.querySelector(".toast-body");
        const toastImg = toastEl.querySelector(".toast-img");

        toastBody.textContent = mensaje;

        // Muestra imagen si se pasó una URL válida
        if (imgUrl) {
            toastImg.src = imgUrl;
            toastImg.classList.remove("d-none");
        } else {
            toastImg.classList.add("d-none");
        }

        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();
    };
})();
