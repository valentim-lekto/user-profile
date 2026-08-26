import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login-placeholder',
  imports: [MatButtonModule, MatCardModule, RouterLink],
  templateUrl: './login-placeholder.html',
  styleUrl: './login-placeholder.scss',
})
export class LoginPlaceholder {
  private readonly router = inject(Router);

  protected readonly registrationCompleted =
    this.router.currentNavigation()?.extras.state?.['registrationCompleted'] === true;
}
