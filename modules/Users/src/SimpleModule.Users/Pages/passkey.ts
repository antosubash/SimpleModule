// WebAuthn uses base64url encoding for all binary data.
// These helpers convert between ArrayBuffer (required by browser API) and base64url strings.

function base64urlToArrayBuffer(base64url: string): ArrayBuffer {
  const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const binary = atob(padded);
  const buffer = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    buffer[i] = binary.charCodeAt(i);
  }
  return buffer.buffer;
}

function arrayBufferToBase64url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

function prepareCreationOptions(json: Record<string, unknown>): PublicKeyCredentialCreationOptions {
  const opts = json as Record<string, unknown>;
  return {
    ...opts,
    challenge: base64urlToArrayBuffer(opts.challenge as string),
    user: {
      ...(opts.user as Record<string, unknown>),
      id: base64urlToArrayBuffer((opts.user as Record<string, unknown>).id as string),
    },
    excludeCredentials: ((opts.excludeCredentials as unknown[]) ?? []).map((c: unknown) => {
      const cred = c as Record<string, unknown>;
      return { ...cred, id: base64urlToArrayBuffer(cred.id as string) };
    }),
  } as unknown as PublicKeyCredentialCreationOptions;
}

function prepareRequestOptions(json: Record<string, unknown>): PublicKeyCredentialRequestOptions {
  const opts = json as Record<string, unknown>;
  return {
    ...opts,
    challenge: base64urlToArrayBuffer(opts.challenge as string),
    allowCredentials: ((opts.allowCredentials as unknown[]) ?? []).map((c: unknown) => {
      const cred = c as Record<string, unknown>;
      return { ...cred, id: base64urlToArrayBuffer(cred.id as string) };
    }),
  } as unknown as PublicKeyCredentialRequestOptions;
}

function serializeAttestation(credential: PublicKeyCredential): Record<string, unknown> {
  const r = credential.response as AuthenticatorAttestationResponse;
  return {
    id: credential.id,
    rawId: arrayBufferToBase64url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: arrayBufferToBase64url(r.clientDataJSON),
      attestationObject: arrayBufferToBase64url(r.attestationObject),
      transports: r.getTransports?.() ?? [],
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

function serializeAssertion(credential: PublicKeyCredential): Record<string, unknown> {
  const r = credential.response as AuthenticatorAssertionResponse;
  return {
    id: credential.id,
    rawId: arrayBufferToBase64url(credential.rawId),
    type: credential.type,
    response: {
      clientDataJSON: arrayBufferToBase64url(r.clientDataJSON),
      authenticatorData: arrayBufferToBase64url(r.authenticatorData),
      signature: arrayBufferToBase64url(r.signature),
      userHandle: r.userHandle ? arrayBufferToBase64url(r.userHandle) : null,
    },
    clientExtensionResults: credential.getClientExtensionResults(),
  };
}

/**
 * Full passkey registration flow:
 * 1. Fetches creation options from the server
 * 2. Prompts the user's device for biometric/PIN confirmation
 * 3. Returns the serialized credential to be posted to /api/passkeys/register/complete
 */
export async function startPasskeyRegistration(): Promise<Record<string, unknown>> {
  const beginRes = await fetch('/api/passkeys/register/begin', { method: 'POST' });
  if (!beginRes.ok) {
    throw new Error('Failed to start passkey registration');
  }
  const optionsJson = (await beginRes.json()) as Record<string, unknown>;
  const options = prepareCreationOptions(optionsJson);

  const credential = await navigator.credentials.create({ publicKey: options });
  if (!credential) {
    throw new Error('No credential returned from device');
  }
  return serializeAttestation(credential as PublicKeyCredential);
}

/**
 * Full passkey authentication flow:
 * 1. Fetches request options from the server
 * 2. Prompts the user's device for biometric/PIN confirmation
 * 3. Returns the serialized credential to be posted to /api/passkeys/login/complete
 */
export async function startPasskeyAssertion(): Promise<Record<string, unknown>> {
  const beginRes = await fetch('/api/passkeys/login/begin', { method: 'POST' });
  if (!beginRes.ok) {
    throw new Error('Failed to start passkey sign-in');
  }
  const optionsJson = (await beginRes.json()) as Record<string, unknown>;
  const options = prepareRequestOptions(optionsJson);

  const credential = await navigator.credentials.get({ publicKey: options });
  if (!credential) {
    throw new Error('No credential returned from device');
  }
  return serializeAssertion(credential as PublicKeyCredential);
}
