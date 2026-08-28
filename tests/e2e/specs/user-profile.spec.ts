import { randomUUID } from 'node:crypto';
import { expect, test, type Locator, type Page } from '@playwright/test';

interface Account {
  readonly email: string;
  readonly name: string;
}

type SecretKey = 'primary' | 'replacement' | 'invalid';

interface SecretField {
  readonly locator: Locator;
  readonly redactionSelector: string;
  readonly secretKey: SecretKey;
}

const secretVaultStorageKey = '__user_profile_e2e_secrets__';

function createAccount(prefix: string): Account {
  const suffix = randomUUID();

  return {
    email: `${prefix}-${suffix}@example.test`,
    name: `${prefix} User`,
  };
}

async function register(page: Page, account: Account): Promise<void> {
  await page.goto('/register');
  await expectNoHorizontalOverflow(page);
  await page.getByLabel('Nome').fill(account.name);
  await page.getByLabel('Email').fill(account.email);
  await submitWithSecrets(
    page,
    [
      {
        locator: page.getByLabel('Senha', { exact: true }),
        redactionSelector: '[formControlName="password"]',
        secretKey: 'primary',
      },
      {
        locator: page.getByLabel('Confirmação de senha', { exact: true }),
        redactionSelector: '[formControlName="passwordConfirmation"]',
        secretKey: 'primary',
      },
    ],
    () => page.getByRole('button', { name: 'Criar conta' }).click(),
  );
  await expectPath(page, '/login');
  await expectNoHorizontalOverflow(page);
  await expect(page.getByText('Cadastro realizado com sucesso. Faça login para continuar.')).toBeVisible();
}

async function login(page: Page, account: Account): Promise<void> {
  await expectNoHorizontalOverflow(page);
  await page.getByLabel('Email').fill(account.email);
  await submitWithSecrets(
    page,
    [
      {
        locator: page.getByLabel('Senha', { exact: true }),
        redactionSelector: '[formControlName="password"]',
        secretKey: 'primary',
      },
    ],
    () => page.getByRole('button', { name: 'Entrar' }).click(),
  );
  await expectPath(page, '/dashboard');
  await expect(page.getByRole('heading', { name: `Boas-vindas, ${account.name}!` })).toBeVisible();
}

async function submitWithSecrets(
  page: Page,
  fields: readonly SecretField[],
  submit: () => Promise<void>,
): Promise<void> {
  try {
    for (const field of fields) {
      await fillSecret(field.locator, field.secretKey);
    }

    await submit();
  } finally {
    await redactSecrets(
      page,
      fields.map(({ redactionSelector }) => redactionSelector),
    );
  }
}

async function fillSecret(field: Locator, secretKey: SecretKey): Promise<void> {
  await field.evaluate(
    (element, { storageKey, key }) => {
      let serializedVault = sessionStorage.getItem(storageKey);

      if (serializedVault === null) {
        const randomHex = (): string => Array.from(
          crypto.getRandomValues(new Uint8Array(16)),
          (byte) => byte.toString(16).padStart(2, '0'),
        ).join('');

        serializedVault = JSON.stringify({
          primary: `E2E-${randomHex()}-Aa1!`,
          replacement: `E2E-${randomHex()}-Aa2!`,
          invalid: `E2E-${randomHex()}-Aa3!`,
        });
        sessionStorage.setItem(storageKey, serializedVault);
      }

      const vault = JSON.parse(serializedVault) as Record<string, unknown>;
      const secret = vault?.[key];

      if (typeof secret !== 'string') {
        throw new Error(`Missing browser-local E2E secret for key: ${key}`);
      }

      const input = element as HTMLInputElement;
      const valueSetter = Object.getOwnPropertyDescriptor(
        HTMLInputElement.prototype,
        'value',
      )?.set;

      valueSetter?.call(input, secret);
      input.dispatchEvent(new Event('input', { bubbles: true }));
    },
    { storageKey: secretVaultStorageKey, key: secretKey },
  );
}

async function redactSecrets(page: Page, selectors: readonly string[]): Promise<void> {
  for (const selector of selectors) {
    await page.locator(selector).evaluateAll((elements) => {
      const valueSetter = Object.getOwnPropertyDescriptor(
        HTMLInputElement.prototype,
        'value',
      )?.set;

      elements.forEach((element) => valueSetter?.call(element, ''));
    });
  }
}

async function expectPath(page: Page, path: string): Promise<void> {
  await expect.poll(() => new URL(page.url()).pathname).toBe(path);
}

async function expectNoHorizontalOverflow(page: Page): Promise<void> {
  await expect
    .poll(() =>
      page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      ),
    )
    .toBe(true);
}

async function expectAuthFormInShortLandscape(
  page: Page,
  path: '/login' | '/register',
  heading: 'Entrar' | 'Criar conta',
  firstField: 'Email' | 'Nome',
  lastField: 'Senha' | 'Confirmação de senha',
  primaryAction: 'Entrar' | 'Criar conta',
): Promise<void> {
  await page.setViewportSize({ width: 667, height: 375 });
  await page.goto(path);
  await expectNoHorizontalOverflow(page);
  await expect(page.getByRole('heading', { name: heading, level: 1 })).toBeInViewport({ ratio: 1 });
  await expect(page.getByLabel(firstField, { exact: true })).toBeInViewport({ ratio: 0.9 });

  const finalInput = page.getByLabel(lastField, { exact: true });
  const submit = page.getByRole('button', { name: primaryAction, exact: true });
  await finalInput.scrollIntoViewIfNeeded();
  await expect(finalInput).toBeVisible();
  await expect(finalInput).toBeEnabled();
  await expect(finalInput).toBeInViewport({ ratio: 0.99 });
  await submit.scrollIntoViewIfNeeded();
  await expect(submit).toBeVisible();
  await expect(submit).toBeEnabled();
  await expect(submit).toBeInViewport({ ratio: 0.99 });
}

async function expectStackedActionsFollowFocusOrder(
  page: Page,
  lastField: Locator,
  secondaryAction: Locator,
  primaryAction: Locator,
): Promise<void> {
  await lastField.focus();
  await page.keyboard.press('Tab');
  await expect(secondaryAction).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(primaryAction).toBeFocused();

  const [secondaryBox, primaryBox] = await Promise.all([
    secondaryAction.boundingBox(),
    primaryAction.boundingBox(),
  ]);

  expect(secondaryBox).not.toBeNull();
  expect(primaryBox).not.toBeNull();
  expect(secondaryBox!.y).toBeLessThan(primaryBox!.y);
}

async function expectTextVisuallyLimitedToLines(locator: Locator, maximumLines: number): Promise<void> {
  await expect
    .poll(() =>
      locator.evaluate((element, lines) => {
        const style = getComputedStyle(element);
        const fontSize = Number.parseFloat(style.fontSize);
        const parsedLineHeight = Number.parseFloat(style.lineHeight);
        const minimumLineHeight = Number.isFinite(parsedLineHeight) ? parsedLineHeight : fontSize;
        const maximumLineHeight = Number.isFinite(parsedLineHeight) ? parsedLineHeight : fontSize * 1.6;
        const box = element.getBoundingClientRect();
        const viewportWidth = document.documentElement.clientWidth;
        let fitsClippingAncestors = true;
        let ancestor = element.parentElement;

        while (ancestor && fitsClippingAncestors) {
          const ancestorStyle = getComputedStyle(ancestor);

          if (ancestorStyle.overflowX === 'hidden' || ancestorStyle.overflowX === 'clip') {
            const ancestorBox = ancestor.getBoundingClientRect();
            fitsClippingAncestors = box.left >= ancestorBox.left - 1
              && box.right <= ancestorBox.right + 1;
          }

          ancestor = ancestor.parentElement;
        }

        return style.overflow === 'hidden'
          && style.webkitLineClamp === String(lines)
          && style.visibility !== 'hidden'
          && Number.parseFloat(style.opacity) > 0
          && box.width > 0
          && element.scrollWidth <= element.clientWidth + 1
          && box.left >= -1
          && box.right <= viewportWidth + 1
          && fitsClippingAncestors
          && box.height >= minimumLineHeight - 1
          && box.height <= maximumLineHeight * lines + 1;
      }, maximumLines),
    )
    .toBe(true);
}

test('E2E-001 registration, profile update, fresh dashboard and logout', async ({ page }) => {
  const account = createAccount('e2e-profile');
  const updatedName = `Responsive ${'N'.repeat(189)}`;
  const updatedEmail = `updated-${randomUUID()}@example.test`;

  await expectAuthFormInShortLandscape(
    page,
    '/register',
    'Criar conta',
    'Nome',
    'Confirmação de senha',
    'Criar conta',
  );
  await expectAuthFormInShortLandscape(page, '/login', 'Entrar', 'Email', 'Senha', 'Entrar');

  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto('/register');
  await expectStackedActionsFollowFocusOrder(
    page,
    page.getByLabel('Confirmação de senha', { exact: true }),
    page.getByRole('link', { name: 'Voltar para o login' }),
    page.getByRole('button', { name: 'Criar conta' }),
  );
  await register(page, account);
  await expectStackedActionsFollowFocusOrder(
    page,
    page.getByLabel('Senha', { exact: true }),
    page.getByRole('link', { name: 'Criar conta' }),
    page.getByRole('button', { name: 'Entrar' }),
  );
  await login(page, account);
  await expectNoHorizontalOverflow(page);

  await page.getByRole('link', { name: 'Ir para o perfil' }).click();
  await expectPath(page, '/profile');
  await expectNoHorizontalOverflow(page);
  await expect(page.getByLabel('Nome')).toBeVisible();
  await expect(page.getByText('Identificador da conta')).toHaveCount(0);
  await page.getByLabel('Nome').fill(updatedName);
  await page.getByLabel('Email').fill(updatedEmail);
  await page.getByRole('button', { name: 'Salvar dados' }).click();
  await expect(page.getByText('Dados pessoais atualizados com sucesso.')).toBeVisible();

  await page.getByRole('link', { name: 'Voltar ao dashboard' }).click();
  await expectPath(page, '/dashboard');
  const welcome = page.getByRole('heading', { name: `Boas-vindas, ${updatedName}!` });
  const profilePreviewName = page.locator('.profile-preview strong');
  await expect(welcome).toBeVisible();
  await expect(profilePreviewName).toBeVisible();
  await expect(profilePreviewName).toHaveText(updatedName);
  await expectTextVisuallyLimitedToLines(welcome, 3);
  await expectTextVisuallyLimitedToLines(profilePreviewName, 2);
  await page.evaluate(() => window.scrollTo(0, 0));
  await expect(page.getByRole('link', { name: 'Ir para o perfil' })).toBeInViewport({ ratio: 1 });
  await expect(page.getByRole('button', { name: 'Sair' })).toBeInViewport({ ratio: 1 });
  await expectNoHorizontalOverflow(page);

  await page.getByRole('link', { name: 'Ir para o perfil' }).click();
  await expectPath(page, '/profile');
  await expect(page.getByLabel('Email')).toHaveValue(updatedEmail);
  await page.getByRole('link', { name: 'Voltar ao dashboard' }).click();
  await expectPath(page, '/dashboard');

  await page.getByRole('button', { name: 'Sair' }).click();
  await expectPath(page, '/login');
  await page.goto('/dashboard');
  await expectPath(page, '/login');
  await expectNoHorizontalOverflow(page);
});

test('E2E-002 anonymous protected route and generic invalid login', async ({ page }) => {
  await page.goto('/dashboard');
  await expectPath(page, '/login');

  const skipLink = page.getByRole('link', { name: 'Ir para o conteúdo' });
  await page.keyboard.press('Tab');
  await expect(skipLink).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#main-content')).toBeFocused();

  await page.getByLabel('Email').fill(`unknown-${randomUUID()}@example.test`);
  const password = page.getByLabel('Senha', { exact: true });
  await submitWithSecrets(
    page,
    [
      {
        locator: password,
        redactionSelector: '[formControlName="password"]',
        secretKey: 'invalid',
      },
    ],
    () => password.press('Enter'),
  );

  await expectPath(page, '/login');
  await expect(page.getByRole('alert')).toHaveText('Email ou senha inválidos.');
});

test('E2E-003 password change ends the session and accepts only the new password', async ({ page }) => {
  const account = createAccount('e2e-password');

  await register(page, account);
  await login(page, account);

  await page.getByRole('link', { name: 'Ir para o perfil' }).click();
  await expectPath(page, '/profile');
  await submitWithSecrets(
    page,
    [
      {
        locator: page.getByLabel('Senha atual', { exact: true }),
        redactionSelector: '[formControlName="currentPassword"]',
        secretKey: 'primary',
      },
      {
        locator: page.getByLabel('Nova senha', { exact: true }),
        redactionSelector: '[formControlName="newPassword"]',
        secretKey: 'replacement',
      },
      {
        locator: page.getByLabel('Confirmação da nova senha', { exact: true }),
        redactionSelector: '[formControlName="newPasswordConfirmation"]',
        secretKey: 'replacement',
      },
    ],
    () => page.getByRole('button', { name: 'Alterar senha' }).click(),
  );

  await expectPath(page, '/login');
  await expect(page.getByText('Senha alterada com sucesso. Faça login novamente.')).toBeVisible();
  await page.goto('/dashboard');
  await expectPath(page, '/login');

  await page.getByLabel('Email').fill(account.email);
  const loginPassword = page.getByLabel('Senha', { exact: true });
  await submitWithSecrets(
    page,
    [
      {
        locator: loginPassword,
        redactionSelector: '[formControlName="password"]',
        secretKey: 'primary',
      },
    ],
    () => page.getByRole('button', { name: 'Entrar' }).click(),
  );
  await expect(page.getByRole('alert')).toHaveText('Email ou senha inválidos.');

  await submitWithSecrets(
    page,
    [
      {
        locator: loginPassword,
        redactionSelector: '[formControlName="password"]',
        secretKey: 'replacement',
      },
    ],
    () => page.getByRole('button', { name: 'Entrar' }).click(),
  );
  await expectPath(page, '/dashboard');
  await expect(page.getByRole('heading', { name: `Boas-vindas, ${account.name}!` })).toBeVisible();
});
