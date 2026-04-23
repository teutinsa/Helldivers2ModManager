export function clickOutside(node: HTMLElement, onClose: () => void) {
    function handler(e: MouseEvent) {
        if (!node.contains(e.target as Node)) onClose();
    }
    document.addEventListener("mousedown", handler);
    return { destroy() { document.removeEventListener("mousedown", handler); } };
}

export function scrollOutside(node: HTMLElement, onClose: () => void) {
    function handler(e: Event) {
        if (!node.contains(e.target as Node)) onClose();
    }
    document.addEventListener("scroll", handler);
    return { destroy() { document.removeEventListener("scroll", handler); } }
}

export function windowResize(_: HTMLElement, onClose: () => void) {
    function handler(_: UIEvent) {
        onClose();
    }
    window.addEventListener("resize", handler);
    return { destroy() { window.removeEventListener("resize", handler); } }
}

export function portal(node: HTMLElement, target: string | HTMLElement) {
    function update(target: string | HTMLElement) {
        const elm = typeof target == "string"
            ? document.querySelector(target) as HTMLElement
            : target;
        elm.appendChild(node);
    }
    update(target);
    return { update, destroy() { node.remove(); } }
}