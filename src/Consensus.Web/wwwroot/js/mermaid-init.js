// Mermaid bootstrap. Initialized once when the library loads with config that
// matches the MudBlazor dark/light theme. Tutorial pages call
// `mermaidRunOnPage()` from OnAfterRenderAsync to render any <div
// class="mermaid"> blocks present in the current DOM. Re-running is safe —
// Mermaid skips already-rendered diagrams (it sets data-processed="true").
(function () {
    if (typeof mermaid === 'undefined') {
        console.warn('Mermaid library not loaded; skipping init');
        return;
    }
    mermaid.initialize({
        startOnLoad: false,
        // Use the current Mud theme background hint. The tutorial wiki is
        // visually light-mode-first; dark-mode users see a darker palette.
        theme: document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'default',
        themeVariables: {
            fontFamily: "'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif",
        },
        flowchart: { useMaxWidth: true, htmlLabels: true, curve: 'basis' },
        sequence: { useMaxWidth: true, showSequenceNumbers: true },
        securityLevel: 'loose',
    });
})();

window.mermaidRunOnPage = async function () {
    if (typeof mermaid === 'undefined') return;
    try {
        await mermaid.run({ querySelector: '.mermaid:not([data-processed="true"])' });
    } catch (e) {
        console.warn('mermaid.run failed', e);
    }
};
