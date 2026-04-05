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
  rememberMe: boolean;
  returnUrl: string;
  errors?: string[];
}

export default function LoginWith2fa({ rememberMe, returnUrl, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(
      `/Identity/Account/LoginWith2fa?rememberMe=${rememberMe}&returnUrl=${encodeURIComponent(returnUrl)}`,
      formData,
    );
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-2">Two-factor authentication</h1>
              <hr className="my-4" />
              <p className="text-sm text-text-muted mb-4">
                Your login is protected with an authenticator app. Enter your authenticator code
                below.
              </p>
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
                    <Label htmlFor="twoFactorCode">Authenticator code</Label>
                    <Input id="twoFactorCode" name="twoFactorCode" required autoComplete="off" />
                  </Field>
                  <label className="flex items-center gap-2 text-sm text-text-secondary cursor-pointer mb-0">
                    <input
                      type="checkbox"
                      name="rememberMachine"
                      value="true"
                      className="w-4 h-4 rounded border-border accent-primary"
                    />
                    Remember this machine
                  </label>
                  <Button type="submit" className="w-full">
                    Log in
                  </Button>
                </FieldGroup>
              </form>
              <p className="text-sm text-text-muted mt-4">
                Don't have access to your authenticator device? You can{' '}
                <a
                  href={`/Identity/Account/LoginWithRecoveryCode?returnUrl=${encodeURIComponent(returnUrl)}`}
                >
                  log in with a recovery code
                </a>
                .
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
