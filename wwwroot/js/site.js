const toggle = document.querySelector('.menu-toggle');
const sidebar = document.querySelector('.sidebar');
const narrowViewport = matchMedia('(max-width: 1000px)');

function closeNavigation() {
    const returnFocus = narrowViewport.matches && sidebar.contains(document.activeElement);
    document.body.classList.remove('navigation-open');
    toggle.setAttribute('aria-expanded', 'false');
    if (returnFocus) toggle.focus();
}

toggle.addEventListener('click', () => {
    if (document.body.classList.contains('navigation-open')) {
        closeNavigation();
        return;
    }
    document.body.classList.add('navigation-open');
    toggle.setAttribute('aria-expanded', 'true');
    // The sidebar precedes the toggle in document order. Move focus into it so
    // keyboard users can reach the primary links immediately after opening it.
    (sidebar.querySelector('nav [aria-current="page"]') ?? sidebar.querySelector('nav a'))?.focus();
});

document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && document.body.classList.contains('navigation-open')) {
        closeNavigation();
        toggle.focus();
    }
});

document.addEventListener('click', event => {
    if (document.body.classList.contains('navigation-open') &&
        !sidebar.contains(event.target) && !toggle.contains(event.target)) {
        closeNavigation();
    }
});

narrowViewport.addEventListener('change', closeNavigation);
