import { HttpErrorResponse } from '@angular/common/http';
import { toProblemDetails } from './problem-details';

describe('toProblemDetails', () => {
  it('preserves only the supported ProblemDetails fields', () => {
    const result = toProblemDetails(
      new HttpErrorResponse({
        status: 503,
        error: {
          status: 400,
          title: 'Bad Request',
          detail: 'Review the submitted values.',
          errors: {
            email: ['The email is invalid.'],
            ignored: ['valid message', 123],
          },
          traceId: 'not-exposed',
        },
      }),
    );

    expect(result).toEqual({
      status: 400,
      title: 'Bad Request',
      detail: 'Review the submitted values.',
      errors: { email: ['The email is invalid.'] },
    });
  });

  it('uses the HTTP status and ignores an unsupported response body', () => {
    const result = toProblemDetails(
      new HttpErrorResponse({ status: 503, error: '<html>upstream failure</html>' }),
    );

    expect(result).toEqual({ status: 503 });
  });

  it('maps a non-HTTP failure to the safe client fallback', () => {
    expect(toProblemDetails(new Error('synthetic failure'))).toEqual({ status: 0 });
  });
});
