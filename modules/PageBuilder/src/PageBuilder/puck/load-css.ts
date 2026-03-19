const CSS_ID = 'puck-editor-css';

export function loadPuckCss(): void {
  if (document.getElementById(CSS_ID)) return;
  const link = document.createElement('link');
  link.id = CSS_ID;
  link.rel = 'stylesheet';
  link.href = '/_content/PageBuilder/pagebuilder.css';
  document.head.appendChild(link);
}
