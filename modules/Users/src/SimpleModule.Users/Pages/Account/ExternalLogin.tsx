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
  providerDisplayName: string;
  email?: string;
  errors?: string[];
}

export default function ExternalLogin({ returnUrl, providerDisplayName, email, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/ExternalLogin?action=Confirmation', formData);
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-2">Register</h1>
              <p className="text-sm text-text-muted mb-2">
                Associate your {providerDisplayName} account.
              </p>
              <hr className="my-4" />
              <p className="text-sm text-info mb-4">
                You've successfully authenticated with <strong>{providerDisplayName}</strong>.
                Please enter an email address for this site below and click the Register button to
                finish logging in.
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
                <input type="hidden" name="returnUrl" value={returnUrl} />
                <FieldGroup>
                  <Field>
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      name="email"
                      type="email"
                      required
                      autoComplete="email"
                      defaultValue={email}
                      placeholder="Please enter your email."
                    />
                  </Field>
                  <Button type="submit" className="w-full">
                    Register
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
