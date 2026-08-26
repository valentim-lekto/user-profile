import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RegisterRequest, RegistrationService } from './registration.service';

const REGISTER_REQUEST: RegisterRequest = {
  name: 'Ana Example',
  email: 'ana@example.test',
  password: 'synthetic-password',
  passwordConfirmation: 'synthetic-password',
};

describe('RegistrationService', () => {
  let service: RegistrationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(RegistrationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('posts the OpenAPI registration contract and exposes loading and data signals', async () => {
    const resultPromise = service.register(REGISTER_REQUEST);

    expect(service.loading()).toBe(true);
    expect(service.data()).toBeNull();
    expect(service.error()).toBeNull();

    const request = http.expectOne('/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(REGISTER_REQUEST);

    request.flush(
      { message: 'Registration completed successfully.' },
      { status: 201, statusText: 'Created' },
    );

    await expect(resultPromise).resolves.toEqual({
      message: 'Registration completed successfully.',
    });
    expect(service.loading()).toBe(false);
    expect(service.data()).toEqual({ message: 'Registration completed successfully.' });
  });

  it('does not issue a second request while registration is pending', async () => {
    const firstResult = service.register(REGISTER_REQUEST);
    const secondResult = service.register(REGISTER_REQUEST);
    const requests = http.match('/api/auth/register');

    expect(requests).toHaveLength(1);
    await expect(secondResult).resolves.toBeNull();

    requests[0].flush(
      { message: 'Registration completed successfully.' },
      { status: 201, statusText: 'Created' },
    );

    await expect(firstResult).resolves.not.toBeNull();
    expect(service.loading()).toBe(false);
  });

  it('preserves safe validation details returned by the API', async () => {
    const resultPromise = service.register(REGISTER_REQUEST);

    http.expectOne('/api/auth/register').flush(
      {
        title: 'Bad Request',
        status: 400,
        errors: { email: ['The email is invalid.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await expect(resultPromise).resolves.toBeNull();
    expect(service.error()).toEqual({
      title: 'Bad Request',
      status: 400,
      detail: undefined,
      errors: { email: ['The email is invalid.'] },
    });
    expect(service.loading()).toBe(false);
  });

  it('does not expose a non-ProblemDetails response body as an error message', async () => {
    const resultPromise = service.register(REGISTER_REQUEST);

    http
      .expectOne('/api/auth/register')
      .flush('<html>upstream failure</html>', { status: 503, statusText: 'Unavailable' });

    await expect(resultPromise).resolves.toBeNull();
    expect(service.error()).toEqual({ status: 503 });
  });
});
