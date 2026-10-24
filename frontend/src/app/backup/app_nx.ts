import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { NxWelcome } from './nx-welcome';

@Component({
  imports: [NxWelcome, RouterModule],
  selector: 'app-root',
  templateUrl: './app_nx.html',
  styleUrl: './app_nx.css',
})
export class App {
  protected title = 'N2F';
}
