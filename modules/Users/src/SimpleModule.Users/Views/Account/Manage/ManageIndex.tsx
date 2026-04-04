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
  username?: string;
  phoneNumber?: string;
  statusMessage?: string;
}

export default function ManageIndex({ username, phoneNumber, statusMessage }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage', formData);
  }

  return (
    <Container size="sm">
      <h3 className="text-xl font-bold mb-4">Profile</h3>
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label>Username</Label>
                <Input value={username ?? ''} disabled placeholder="Username" />
              </Field>
              <Field>
                <Label htmlFor="phoneNumber">Phone number</Label>
                <Input
                  id="phoneNumber"
                  name="phoneNumber"
                  defaultValue={phoneNumber ?? ''}
                  placeholder="Please enter your phone number."
                />
              </Field>
              <Button type="submit" className="w-full">
                Save
              </Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Container>
  );
}
