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

interface Props {
  returnUrl: string;
  externalLogins: { name: string; displayName: string }[];
  errors?: string[];
}

export default function Register({ returnUrl, externalLogins, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(`/Identity/Account/Register?returnUrl=${encodeURIComponent(returnUrl)}`, formData);
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
              Create an account
            </h1>
            <p className="text-text-muted text-sm mt-1.5">Get started with SimpleModule</p>
          </div>

          <Card>
            <CardContent className="p-8">
              {errors && errors.length > 0 && (
                <div className="alert-danger mb-4 text-sm" role="alert">
                  <ul className="list-none p-0 m-0 space-y-1">
                    {errors.map((err) => (
                      <li key={err}>{err}</li>
                    ))}
                  </ul>
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
                      autoComplete="new-password"
                      placeholder="&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;"
                    />
                  </Field>
                  <Field>
                    <Label htmlFor="confirmPassword">Confirm Password</Label>
                    <Input
                      id="confirmPassword"
                      name="confirmPassword"
                      type="password"
                      required
                      autoComplete="new-password"
                      placeholder="&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;&#8226;"
                    />
                  </Field>
                  <Button type="submit" className="w-full">
                    Create Account
                  </Button>
                </FieldGroup>
              </form>

              {externalLogins.length > 0 && (
                <>
                  <div className="relative my-6">
                    <hr />
                    <span className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-surface px-3 text-xs text-text-muted">
                      or continue with
                    </span>
                  </div>
                  <form action="/Identity/Account/ExternalLogin" method="post">
                    <input type="hidden" name="returnUrl" value={returnUrl} />
                    <div className="flex gap-2 flex-wrap">
                      {externalLogins.map((provider) => (
                        <Button
                          key={provider.name}
                          type="submit"
                          variant="secondary"
                          className="flex-1"
                          name="provider"
                          value={provider.name}
                        >
                          {provider.displayName}
                        </Button>
                      ))}
                    </div>
                  </form>
                </>
              )}
            </CardContent>
          </Card>

          <div className="mt-6 text-center text-sm text-text-muted">
            Already have an account?{' '}
            <a
              href="/Identity/Account/Login"
              className="text-primary font-medium no-underline hover:underline"
            >
              Sign in
            </a>
          </div>
        </div>
      </div>
    </Container>
  );
}
