import { router } from '@inertiajs/react';
import {
  Alert,
  AlertDescription,
  Button,
  Card,
  CardContent,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { useRef } from 'react';

interface Props {
  returnUrl: string;
  showTestAccounts: boolean;
  errors?: {
    email?: string;
  };
}

export default function Login({ returnUrl, showTestAccounts, errors }: Props) {
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const qs = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    router.post(`/Identity/Account/Login${qs}`, formData);
  }

  function fillCredentials(email: string, password: string) {
    if (emailRef.current) emailRef.current.value = email;
    if (passwordRef.current) passwordRef.current.value = password;
  }

  return (
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
              <Alert variant="danger" className="mb-4">
                <AlertDescription>{errors.email}</AlertDescription>
              </Alert>
            )}

            <form onSubmit={handleSubmit}>
              <FieldGroup>
                <Field>
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="username"
                    required
                    placeholder="you@example.com"
                    ref={emailRef}
                  />
                </Field>
                <Field>
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    required
                    placeholder="••••••••"
                    ref={passwordRef}
                  />
                </Field>

                <div className="flex items-center justify-between">
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
                    variant="outline"
                    className="flex-1 text-sm"
                    onClick={() => fillCredentials('admin@simplemodule.dev', 'Admin123!')}
                  >
                    Admin
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="flex-1 text-sm"
                    onClick={() => fillCredentials('user@simplemodule.dev', 'User123!')}
                  >
                    User
                  </Button>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <div className="mt-6 text-center text-sm text-text-muted">
          {"Don't have an account? "}
          <a
            href={`/Identity/Account/Register?returnUrl=${encodeURIComponent(returnUrl ?? '/')}`}
            className="text-primary font-medium no-underline hover:underline"
          >
            Sign up
          </a>
        </div>
      </div>
    </div>
  );
}
