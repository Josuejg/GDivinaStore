// RUTA: wwwroot/js/cart-count.js
(function () {
    function refreshCartCount() {
        fetch('/Carrito/Count', { cache: 'no-store' })
            .then(r => r.json())
            .then(n => {
                var el = document.getElementById('miniCartCount');
                if (el) el.textContent = n || 0;
            })
            .catch(() => { /* silencio para no romper nada */ });
    }

    // Refresco inicial al cargar la página
    document.addEventListener('DOMContentLoaded', refreshCartCount);

    // Lo expongo por si quieres llamar esto después de un "Agregar" vía AJAX
    window.RefreshCartCount = refreshCartCount;
})();
