// Smooth Mobile Touch & Drag Sortable for Route Stops
window.touchSortable = {
    init: function (containerId, dotNetHelper) {
        const container = document.getElementById(containerId);
        if (!container) return;

        let draggedEl = null;
        let placeholder = null;
        let startY = 0;
        let originalNextSibling = null;
        let touchTargetCard = null;

        if (container._touchCleanup) {
            container._touchCleanup();
        }

        function getCard(el) {
            return el.closest('[data-draggable-stop]');
        }

        function onTouchStart(e) {
            // If delete button clicked, ignore drag
            if (e.target.closest('[data-delete-btn]')) return;

            const card = getCard(e.target);
            if (!card) return;

            draggedEl = card;
            startY = e.touches[0].clientY;
            originalNextSibling = card.nextSibling;
            touchTargetCard = card;

            card.style.opacity = '0.65';
            card.style.transform = 'scale(1.02)';
            card.style.transition = 'transform 0.1s ease, opacity 0.1s ease';
            card.style.zIndex = '999';
        }

        function onTouchMove(e) {
            if (!draggedEl) return;
            const touch = e.touches[0];

            // Prevent window scroll while dragging
            if (e.cancelable) {
                e.preventDefault();
            }

            const target = document.elementFromPoint(touch.clientX, touch.clientY);
            if (!target) return;

            const hoverCard = getCard(target);
            if (hoverCard && hoverCard !== draggedEl && hoverCard.parentNode === container) {
                touchTargetCard = hoverCard;
                const rect = hoverCard.getBoundingClientRect();
                const mid = rect.top + rect.height / 2;
                if (touch.clientY > mid) {
                    container.insertBefore(draggedEl, hoverCard.nextSibling);
                } else {
                    container.insertBefore(draggedEl, hoverCard);
                }
            }
        }

        function onTouchEnd(e) {
            if (!draggedEl) return;

            const droppedCard = draggedEl;
            droppedCard.style.opacity = '';
            droppedCard.style.transform = '';
            droppedCard.style.transition = '';
            droppedCard.style.zIndex = '';
            draggedEl = null;

            // Collect ordered IDs
            const cards = Array.from(container.querySelectorAll('[data-draggable-stop]'));
            const orderedIds = cards.map(c => c.getAttribute('data-stop-id')).filter(id => !!id);

            if (dotNetHelper && orderedIds.length > 0) {
                dotNetHelper.invokeMethodAsync('OnReorderComplete', orderedIds);
            }
        }

        container.addEventListener('touchstart', onTouchStart, { passive: false });
        container.addEventListener('touchmove', onTouchMove, { passive: false });
        container.addEventListener('touchend', onTouchEnd, { passive: false });
        container.addEventListener('touchcancel', onTouchEnd, { passive: false });

        container._touchCleanup = function () {
            container.removeEventListener('touchstart', onTouchStart);
            container.removeEventListener('touchmove', onTouchMove);
            container.removeEventListener('touchend', onTouchEnd);
            container.removeEventListener('touchcancel', onTouchEnd);
        };
    }
};
