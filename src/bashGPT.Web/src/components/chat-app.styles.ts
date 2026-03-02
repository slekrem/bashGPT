import { css } from 'lit'

export const chatAppStyles = css`
  /* ── CSS custom properties (cascade to all child components) ─────────── */
  :host {
    display: flex;
    flex-direction: column;
    height: 100dvh;
    font-family: ui-sans-serif, system-ui, sans-serif;
    background: radial-gradient(circle at top, #1e293b, #020617);
    color: #e5e7eb;
    --color-border: #374151;
    --color-user: #1f2937;
    --color-assistant: #0b1220;
    --color-text: #e5e7eb;
    --color-muted: #6b7280;
    --color-accent: #22c55e;
    --sidebar-width: 220px;
  }

  /* ── Shared header ───────────────────────────────────────────────────── */
  header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 20px;
    border-bottom: 1px solid #1e293b;
    background: rgba(15, 23, 42, 0.9);
    backdrop-filter: blur(8px);
    flex-shrink: 0;
    z-index: 10;
  }
  .logo {
    font-size: 18px;
    font-weight: 700;
    color: #f1f5f9;
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    user-select: none;
  }
  .logo-dot { color: var(--color-accent); }
  .header-actions { display: flex; gap: 8px; align-items: center; }

  button {
    background: #111827;
    color: #e5e7eb;
    border: 1px solid #374151;
    border-radius: 8px;
    padding: 7px 14px;
    font-size: 13px;
    cursor: pointer;
    transition: background 0.15s, border-color 0.15s;
    font-family: inherit;
  }
  button:hover:not(:disabled) { background: #1f2937; border-color: #4b5563; }
  button:focus-visible { outline: 2px solid #22c55e; outline-offset: 2px; }
  button:disabled { opacity: 0.4; cursor: not-allowed; }
  button.primary {
    background: #14532d;
    border-color: #16a34a;
    color: #dcfce7;
    font-weight: 600;
    padding: 7px 20px;
  }
  button.primary:hover:not(:disabled) { background: #166534; }

  /* ── Shell layout ─────────────────────────────────────────────────────── */
  .shell {
    display: flex;
    flex: 1;
    overflow: hidden;
  }
  .content {
    flex: 1;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  /* ── Mobile: sidebar overlay ─────────────────────────────────────────── */
  .mobile-overlay {
    display: none;
  }
  @media (max-width: 768px) {
    .mobile-overlay {
      display: block;
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.5);
      z-index: 20;
    }
    bashgpt-sidebar {
      position: fixed;
      top: 0;
      left: 0;
      bottom: 0;
      z-index: 30;
      transform: translateX(-100%);
      transition: transform 0.2s ease;
      width: 260px !important;
    }
    bashgpt-sidebar.open {
      transform: translateX(0);
    }
    .hamburger { display: flex !important; }
  }
  .hamburger {
    display: none;
    background: none;
    border: none;
    color: #94a3b8;
    font-size: 20px;
    padding: 4px 8px;
    cursor: pointer;
  }
`
