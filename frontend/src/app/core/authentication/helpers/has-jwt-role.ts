export function hasJwtRole(token: string | null, expectedRole: string): boolean {
  if (!token) {
    return false;
  }

  try {
    const parts = token.split('.');

    if (parts.length !== 3) {
      return false;
    }

    const base64 = parts[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    const bytes = Uint8Array.from(atob(padded), (character) => character.charCodeAt(0));

    const payload: unknown = JSON.parse(
      new TextDecoder().decode(bytes),
    );

    if (typeof payload !== 'object' || payload === null || !('role' in payload)) {
      return false;
    }

    const roles = payload.role;
    return (roles === expectedRole || (Array.isArray(roles) && roles.includes(expectedRole)));
  } 
  catch {
    return false;
  }
}