import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-profile-placeholder',
  imports: [MatButtonModule, MatCardModule, RouterLink],
  template: `
    <section class="profile-page" aria-labelledby="profile-title">
      <mat-card appearance="outlined">
        <mat-card-header>
          <mat-card-title id="profile-title">Perfil</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>A edição do perfil será disponibilizada no próximo milestone.</p>
        </mat-card-content>
        <mat-card-actions>
          <a mat-button routerLink="/dashboard">Voltar ao dashboard</a>
        </mat-card-actions>
      </mat-card>
    </section>
  `,
  styles: `
    .profile-page {
      display: grid;
      justify-items: center;
    }

    mat-card {
      max-width: 42rem;
      width: 100%;
    }

    mat-card-content {
      padding-top: 1rem;
    }
  `,
})
export class ProfilePlaceholder {}
