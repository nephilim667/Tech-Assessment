import { Component, effect, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthenticationStore } from './core/authentication/store/authentication-store';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);

  readonly auth = inject(AuthenticationStore);

  constructor() {
    let hadSession = false;

    effect(() => {
      const hasSession = this.auth.hasSession();

      if (hadSession && !hasSession) {
        void this.router.navigateByUrl('/login');
      }

      hadSession = hasSession;
    });
  }

  async logout(): Promise<void> {
    await this.auth.logout();
  }
}