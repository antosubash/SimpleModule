import { router } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import { useState } from 'react';
import ManageLayout from '@/components/ManageLayout';
import { startPasskeyRegistration } from '../../passkey';

interface Passkey {
  credentialId: string;
  name: string;
  createdAt: string;
}

interface Props {
  passkeys: Passkey[];
}

export default function ManagePasskeys({ passkeys }: Props) {
  const [registering, setRegistering] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleAddPasskey() {
    if (!window.PublicKeyCredential) {
      setError('Your browser does not support passkeys.');
      return;
    }
    setRegistering(true);
    setError(null);
    try {
      const credential = await startPasskeyRegistration();
      const res = await fetch('/api/passkeys/register/complete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credential),
      });
      if (!res.ok) {
        setError('Passkey registration failed. Please try again.');
        return;
      }
      router.reload();
    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setError('Registration was cancelled.');
      } else {
        setError('An unexpected error occurred. Please try again.');
      }
    } finally {
      setRegistering(false);
    }
  }

  async function handleDeletePasskey(credentialId: string) {
    if (!confirm('Remove this passkey?')) return;
    const res = await fetch(`/api/passkeys/${encodeURIComponent(credentialId)}`, {
      method: 'DELETE',
    });
    if (res.ok) {
      router.reload();
    } else {
      setError('Failed to remove passkey. Please try again.');
    }
  }

  return (
    <ManageLayout activePage="Passkeys">
      <h3 className="text-lg font-semibold mb-3 sm:mb-4">Passkeys</h3>

      <p className="text-sm text-text-muted mb-4">
        Passkeys let you sign in with your fingerprint, face, or device PIN — no password needed.
      </p>

      {error && (
        <div className="alert-danger mb-4 text-sm" role="alert">
          {error}
        </div>
      )}

      {passkeys.length === 0 ? (
        <p className="text-sm text-text-muted mb-4">No passkeys registered yet.</p>
      ) : (
        <ul className="space-y-2 mb-4">
          {passkeys.map((passkey) => (
            <li
              key={passkey.credentialId}
              className="flex items-center justify-between p-3 border border-border rounded-lg"
            >
              <div>
                <p className="text-sm font-medium">{passkey.name || 'Passkey'}</p>
                <p className="text-xs text-text-muted">
                  Added {new Date(passkey.createdAt).toLocaleDateString()}
                </p>
              </div>
              <Button
                type="button"
                variant="secondary"
                className="text-sm"
                onClick={() => handleDeletePasskey(passkey.credentialId)}
              >
                Remove
              </Button>
            </li>
          ))}
        </ul>
      )}

      <Button type="button" onClick={handleAddPasskey} disabled={registering}>
        {registering ? 'Registering…' : 'Add passkey'}
      </Button>
    </ManageLayout>
  );
}
