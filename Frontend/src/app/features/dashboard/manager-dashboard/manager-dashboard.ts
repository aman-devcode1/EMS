import { Component, OnInit } from '@angular/core';
import { TokenService } from '../../../core/services/token';

@Component({
  selector: 'app-manager-dashboard',
  imports: [],
  templateUrl: './manager-dashboard.html',
  styleUrl: './manager-dashboard.css',
})
export class ManagerDashboard implements OnInit {
  managerName = 'Manager';
  greeting = 'Welcome back';

  constructor(private tokenService: TokenService) { }

  ngOnInit(): void {
    const name = this.tokenService.getUserName();
    this.managerName = (name && name.trim()) ? name : 'Manager';

    // Same logic as Admin/Employee for First Login
    if (sessionStorage.getItem('isFirstLogin') === 'true') {
      this.greeting = 'Welcome';
      sessionStorage.removeItem('isFirstLogin');
    }
  }
}
