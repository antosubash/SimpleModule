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
  statusMessage?: string;
  errors?: string[];
}

export default function SetPassword({ statusMessage, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/SetPassword', formData);
  }

  return (
    <Container size="sm">
      <h3 className="text-xl font-bold mb-4">Set your password</h3>
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
      <p className="text-info text-sm mb-4">
        You do not have a local username/password for this site. Add a local account so you can log
        in without an external login.
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
      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="newPassword">New password</Label>
                <Input
                  id="newPassword"
                  name="newPassword"
                  type="password"
                  required
                  autoComplete="new-password"
                />
              </Field>
              <Field>
                <Label htmlFor="confirmPassword">Confirm new password</Label>
                <Input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  required
                  autoComplete="new-password"
                />
              </Field>
              <Button type="submit" className="w-full">
                Set password
              </Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
