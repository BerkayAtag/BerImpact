(() => {
  const root = document.documentElement;
  const button = document.getElementById('theme-toggle');
  function apply(dark) {
    root.dataset.theme = dark ? 'dark' : 'light';
    button.setAttribute('aria-pressed', String(dark));
    button.setAttribute('aria-label', dark ? 'Aydınlık temaya geç' : 'Karanlık temaya geç');
    document.querySelectorAll('img[data-light-src]').forEach(img => {
      img.src = dark ? img.dataset.darkSrc : img.dataset.lightSrc;
    });
  }
  let dark = false;
  try { dark = localStorage.getItem('berimpact.site.theme') === 'dark'; } catch { /* Optional preference storage. */ }
  apply(dark);
  button.addEventListener('click', () => {
    dark = !dark; apply(dark);
    try { localStorage.setItem('berimpact.site.theme', dark ? 'dark' : 'light'); } catch { /* Theme still works without storage. */ }
  });
})();
