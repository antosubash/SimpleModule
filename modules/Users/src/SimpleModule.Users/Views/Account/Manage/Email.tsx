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
  email?: string;
  isEmailConfirmed: boolean;
  newEmail?: string;
  statusMessage?: string;
}

export default function Email({ email, isEmailConfirmed, newEmail, statusMessage }: Props) {
  function handleChangeEmail(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/Email', formData);
  }

  return (
    <Container size="sm">
      <h3 className="text-xl font-bold mb-4">Manage Email</h3>
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
      <Card>
        <CardContent className="p-6">
          <div className="mb-4">
            <Label>Email</Label>
            <div className="flex items-center gap-2">
              <Input value={email ?? ''} disabled />
              {isEmailConfirmed && <span className="text-success font-bold">&#10003;</span>}
            </div>
          </div>
          <form onSubmit={handleChangeEmail}>
            <FieldGroup>
              <Field>
                <Label htmlFor="newEmail">New email</Label>
                <Input
                  id="newEmail"
                  name="newEmail"
                  type="email"
                  required
                  autoComplete="email"
                  defaultValue={newEmail ?? ''}
                  placeholder="Please enter new email."
                />
              </Field>
              <Button type="submit" className="w-full">
                Change email
              </Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
