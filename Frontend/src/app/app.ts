import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast';
import { AuthService } from './core/services/auth';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('Frontend');
  private authService = inject(AuthService);

  ngOnInit(): void {
    this.authService.refreshToken()
      .pipe(catchError(() => of(null)))
      .subscribe();
  }
}
