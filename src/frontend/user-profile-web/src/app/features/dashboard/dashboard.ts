import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ProfileService } from '../profile/profile.service';

@Component({
  selector: 'app-dashboard',
  imports: [MatButtonModule, MatCardModule, RouterLink],
  providers: [ProfileService],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly profiles = inject(ProfileService);

  ngOnInit(): void {
    void this.profiles.load();
  }

  protected logout(): void {
    this.auth.clearSession();
    void this.router.navigate(['/login']);
  }

  protected reload(): void {
    void this.profiles.load();
  }
}
