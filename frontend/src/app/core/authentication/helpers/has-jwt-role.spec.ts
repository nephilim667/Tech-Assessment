import { hasJwtRole } from './has-jwt-role';

describe('hasJwtRole', () => {
  function tokenWith(payload: object): string {
    return `header.${btoa(JSON.stringify(payload))}.signature`;
  }

  it('recognizes a single role', () => {
    expect(
      hasJwtRole(tokenWith({ role: 'SuperAdmin' }), 'SuperAdmin'),
    ).toBe(true);
  });

  it('recognizes a role in an array', () => {
    expect(
      hasJwtRole(
        tokenWith({ role: ['Admin', 'SuperAdmin'] }),
        'SuperAdmin',
      ),
    ).toBe(true);
  });

  it('does not grant the superadministrator role to an administrator', () => {
    expect(
      hasJwtRole(tokenWith({ role: 'Admin' }), 'SuperAdmin'),
    ).toBe(false);
  });

  it.each([null, 'invalid', 'header.invalid.signature'])(
    'rejects a missing or malformed token: %s',
    (token) => {
      expect(hasJwtRole(token, 'SuperAdmin')).toBe(false);
    },
  );
});