import { Component } from '@angular/core';
import { Sidebar } from '../sidebar/sidebar';
import { Navbar } from '../navbar/navbar';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Navbar, Sidebar],
  standalone: true,
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout {
  sidebarOpen = false;

  onMenuToggle(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  onSidebarClose(): void {
    this.sidebarOpen = false;
  }
}