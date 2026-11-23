import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="app-shell">
      <!-- App Opening Automatic Splash Screen -->
      <div class="app-loading-splash" *ngIf="showSplash" [class.fade-out]="isSplashFading" aria-hidden="true">
        <div class="splash-card">
          <div class="splash-logo-container">
            <img src="/assets/excel-3d-logo.png" alt="Excel 3D Logo" class="splash-excel-img" />
            <div class="splash-ring"></div>
            <div class="splash-glow"></div>
          </div>
          <div class="splash-brand">
            <span class="brand-mark"><span></span><span></span><span></span><span></span></span>
            <h1>sheetwise<span class="brand-dot">.</span></h1>
          </div>
          <p class="splash-subtitle">Workforce Intelligence &amp; Analytics</p>
          <div class="splash-progress">
            <div class="splash-progress-bar"></div>
          </div>
        </div>
      </div>

      <header class="topbar">
        <button
          class="mobile-menu-toggle"
          type="button"
          (click)="toggleSidebar()"
          aria-label="Toggle navigation menu"
          [attr.aria-expanded]="isSidebarOpen">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="3" y1="6" x2="21" y2="6"></line>
            <line x1="3" y1="12" x2="21" y2="12"></line>
            <line x1="3" y1="18" x2="21" y2="18"></line>
          </svg>
        </button>

        <a class="brand" routerLink="/" aria-label="Sheetwise home" (click)="closeSidebar()">
          <span class="brand-mark"><span></span><span></span><span></span><span></span></span>
          <span>sheetwise<span class="brand-dot">.</span></span>
        </a>

        <!-- Desktop Navigation -->
        <nav class="main-nav desktop-nav" aria-label="Main navigation">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Import data</a>
          <a routerLink="/employees" routerLinkActive="active">Employee directory</a>
          <a routerLink="/imports" routerLinkActive="active">Import history</a>
          <a routerLink="/charts" routerLinkActive="active">Charts</a>
        </nav>

        <div class="topbar-status"><span class="status-pulse"></span> Workspace ready</div>
      </header>

      <!-- Mobile Sidebar Backdrop Overlay -->
      <div
        class="mobile-sidebar-overlay"
        [class.open]="isSidebarOpen"
        (click)="closeSidebar()"
        aria-hidden="true"></div>

      <!-- Mobile Sidebar Drawer -->
      <aside class="mobile-sidebar" [class.open]="isSidebarOpen" aria-label="Mobile navigation">
        <div class="mobile-sidebar-header">
          <a class="brand" routerLink="/" (click)="closeSidebar()">
            <span class="brand-mark"><span></span><span></span><span></span><span></span></span>
            <span>sheetwise<span class="brand-dot">.</span></span>
          </a>
          <button
            class="mobile-sidebar-close"
            type="button"
            (click)="closeSidebar()"
            aria-label="Close navigation menu">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        <div class="mobile-sidebar-body">
          <p class="sidebar-section-title">Navigation</p>
          <nav class="mobile-nav-links">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="closeSidebar()">
              <span class="nav-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                  <polyline points="17 8 12 3 7 8"></polyline>
                  <line x1="12" y1="3" x2="12" y2="15"></line>
                </svg>
              </span>
              <span>Import data</span>
            </a>
            <a routerLink="/employees" routerLinkActive="active" (click)="closeSidebar()">
              <span class="nav-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                  <circle cx="9" cy="7" r="4"></circle>
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                </svg>
              </span>
              <span>Employee directory</span>
            </a>
            <a routerLink="/imports" routerLinkActive="active" (click)="closeSidebar()">
              <span class="nav-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                  <polyline points="14 2 14 8 20 8"></polyline>
                  <line x1="16" y1="13" x2="8" y2="13"></line>
                  <line x1="16" y1="17" x2="8" y2="17"></line>
                  <polyline points="10 9 9 9 8 9"></polyline>
                </svg>
              </span>
              <span>Import history</span>
            </a>
            <a routerLink="/charts" routerLinkActive="active" (click)="closeSidebar()">
              <span class="nav-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="18" y1="20" x2="18" y2="10"></line>
                  <line x1="12" y1="20" x2="12" y2="4"></line>
                  <line x1="6" y1="20" x2="6" y2="14"></line>
                </svg>
              </span>
              <span>Charts</span>
            </a>
          </nav>
        </div>

        <div class="mobile-sidebar-footer">
          <div class="sidebar-status">
            <span class="status-pulse"></span>
            <span>Workspace ready</span>
          </div>
          <span class="sidebar-tagline">Make every row count.</span>
        </div>
      </aside>

      <main class="page-content">
        <router-outlet />
      </main>
      <footer class="site-footer"><span>Sheetwise <span class="brand-dot">·</span> Employee analytics</span><span>Make every row count.</span></footer>
    </div>
  `
})
export class AppComponent {
  isSidebarOpen = false;
  showSplash = true;
  isSplashFading = false;

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.isSidebarOpen = false;
    });

    // Automatically transition splash screen on initial app load
    setTimeout(() => {
      this.isSplashFading = true;
      setTimeout(() => {
        this.showSplash = false;
      }, 500);
    }, 1800);
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth > 768 && this.isSidebarOpen) {
      this.isSidebarOpen = false;
    }
  }
}
