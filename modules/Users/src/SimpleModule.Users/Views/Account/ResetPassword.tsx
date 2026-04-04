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
  invalidCode: boolean;
  code?: string;
  errors?: string[];
}

export default function ResetPassword({ invalidCode, code, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/ResetPassword', formData);
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-2">Reset password</h1>
              <hr className="mb-6" />
              {invalidCode ? (
                <p className="alert-danger mb-4 text-sm">
                  A code must be supplied for password reset.
                </p>
              ) : (
                <>
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
                    <input type="hidden" name="code" value={code ?? ''} />
                    <FieldGroup>
                      <Field>
                        <Label htmlFor="email">Email</Label>
                        <Input
                          id="email"
                          name="email"
                          type="email"
                          required
                          autoComplete="username"
                          placeholder="name@example.com"
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
                        />
                      </Field>
                      <Button type="submit" className="w-full">
                        Reset
                      </Button>
                    </FieldGroup>
                  </form>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
