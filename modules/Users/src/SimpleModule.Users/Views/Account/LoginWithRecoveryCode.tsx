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
  errors?: string[];
}

export default function LoginWithRecoveryCode({ returnUrl, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post(
      `/Identity/Account/LoginWithRecoveryCode?returnUrl=${encodeURIComponent(returnUrl)}`,
      formData,
    );
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-2">Recovery code verification</h1>
              <hr className="my-4" />
              <p className="text-sm text-text-muted mb-4">
                You have requested to log in with a recovery code. This login will not be remembered
                until you provide an authenticator app code at log in or disable 2FA and log in
                again.
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
                    <Label htmlFor="recoveryCode">Recovery Code</Label>
                    <Input
                      id="recoveryCode"
                      name="recoveryCode"
                      required
                      autoComplete="off"
                      placeholder="RecoveryCode"
                    />
                  </Field>
                  <Button type="submit" className="w-full">
                    Log in
                  </Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
