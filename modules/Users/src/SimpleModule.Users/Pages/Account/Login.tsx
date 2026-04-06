import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import { startPasskeyAssertion } from '../passkey';

interface Props {
  returnUrl: string;
  showTestAccounts: boolean;
  passkeyEnabled: boolean;
  errors?: { email?: string };
}

export default function Login({ returnUrl, showTestAccounts, passkeyEnabled, errors }: Props) {
  const [passkeyError, setPasskeyError] = useState<string | null>(null);
  const [passkeyLoading, setPasskeyLoading] = useState(false);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/Identity/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`, formData);
  }

  function quickLogin(email: string, password: string) {
    const form = document.querySelector('form') as HTMLFormElement;
    (form.querySelector('[name="email"]') as HTMLInputElement).value = email;
    (form.querySelector('[name="password"]') as HTMLInputElement).value = password;
  }

  async function handlePasskeySignIn() {
    if (!window.PublicKeyCredential) {
      setPasskeyError('Your browser does not support passkeys.');
      return;
    }
    setPasskeyLoading(true);
    setPasskeyError(null);
    try {
      const credential = await startPasskeyAssertion();
      const res = await fetch(
        `/api/passkeys/login/complete?returnUrl=${encodeURIComponent(returnUrl)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(credential),
        },
      );
      if (res.ok) {
        const data = (await res.json()) as { redirectUrl: string };
        window.location.href = data.redirectUrl;
      } else if (res.status === 423) {
        setPasskeyError('Your account is locked. Please try again later.');
      } else {
        setPasskeyError('Passkey sign-in failed. Use your password instead.');
      }
    } catch (err) {
      if (err instanceof Error && err.name === 'NotAllowedError') {
        setPasskeyError('Passkey sign-in was cancelled.');
      } else {
        setPasskeyError('An unexpected error occurred.');
      }
    } finally {
      setPasskeyLoading(false);
    }
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <div
              className="w-14 h-14 rounded-2xl mx-auto mb-5 flex items-center justify-center text-white text-xl font-bold shadow-lg"
              style={{
                background: 'linear-gradient(135deg,var(--color-primary),var(--color-accent))',
              }}
            >
              S
            </div>
            <h1
              className="text-2xl font-extrabold tracking-tight"
              style={{ fontFamily: "'Sora',sans-serif" }}
            >
              Welcome back
            </h1>
            <p className="text-text-muted text-sm mt-1.5">Sign in to your account</p>
          </div>

          <Card>
            <CardContent className="p-8">
              {errors?.email && (
                <div className="alert-danger mb-4 text-sm" role="alert">
                  {errors.email}
                </div>
              )}
              <form onSubmit={handleSubmit}>
                <FieldGroup>
                  <Field>
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      name="email"
                      type="email"
                      required
                      autoComplete="username"
                      placeholder="you@example.com"
                    />
                  </Field>
                  <Field>
                    <Label htmlFor="password">Password</Label>
                    <Input
                      id="password"
                      name="password"
                      type="password"
                      required
                      autoComplete="current-password"
                      placeholder="&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;"
                    />
                  </Field>
                  <div className="flex items-center justify-between mb-2">
                    <label className="flex items-center gap-2 text-sm text-text-secondary cursor-pointer mb-0">
                      <input
                        type="checkbox"
                        name="rememberMe"
                        value="true"
                        className="w-4 h-4 rounded border-border text-primary accent-primary"
                      />
                      Remember me
                    </label>
                    <a
                      href="/Identity/Account/ForgotPassword"
                      className="text-xs text-text-muted hover:text-primary no-underline transition-colors"
                    >
                      Forgot password?
                    </a>
                  </div>
                  <Button type="submit" className="w-full">
                    Log in
                  </Button>
                </FieldGroup>
              </form>

              {passkeyEnabled && (
                <>
                  <div className="relative my-6">
                    <hr />
                    <span className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-surface px-3 text-xs text-text-muted">
                      or
                    </span>
                  </div>
                  {passkeyError && (
                    <div className="alert-danger mb-3 text-sm" role="alert">
                      {passkeyError}
                    </div>
                  )}
                  <Button
                    type="button"
                    variant="secondary"
                    className="w-full"
                    onClick={handlePasskeySignIn}
                    disabled={passkeyLoading}
                  >
                    <svg
                      className="w-4 h-4 mr-2"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                    </svg>
                    {passkeyLoading ? 'Signing in…' : 'Sign in with passkey'}
                  </Button>
                </>
              )}

              {showTestAccounts && (
                <>
                  <div className="relative my-6">
                    <hr />
                    <span className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-surface px-3 text-xs text-text-muted">
                      quick login
                    </span>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="secondary"
                      className="flex-1 text-sm"
                      onClick={() => quickLogin('admin@simplemodule.dev', 'Admin123!')}
                    >
                      Admin
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      className="flex-1 text-sm"
                      onClick={() => quickLogin('user@simplemodule.dev', 'User123!')}
                    >
                      User
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          <div className="mt-6 text-center text-sm text-text-muted">
            Don't have an account?{' '}
            <a
              href={`/Identity/Account/Register?returnUrl=${encodeURIComponent(returnUrl)}`}
              className="text-primary font-medium no-underline hover:underline"
            >
              Sign up
            </a>
          </div>
        </div>
      </div>
    </Container>
  );
}
