import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell">
      <header class="topbar">
        <a class="brand" routerLink="/" aria-label="Sheetwise home">
          <span class="brand-mark"><span></span><span></span><span></span><span></span></span>
          <span>sheetwise<span class="brand-dot">.</span></span>
        </a>
        <nav class="main-nav" aria-label="Main navigation">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Import data</a>
          <a routerLink="/employees" routerLinkActive="active">Employee directory</a>
          <a routerLink="/imports" routerLinkActive="active">Import history</a>
          <a routerLink="/charts" routerLinkActive="active">Charts</a>
        </nav>
        <div class="topbar-status"><span class="status-pulse"></span> Workspace ready</div>
      </header>
      <main class="page-content">
        <router-outlet />
      </main>
      <footer class="site-footer"><span>Sheetwise <span class="brand-dot">·</span> Employee analytics</span><span>Make every row count.</span></footer>
    </div>
  `
})
export class AppComponent {}
