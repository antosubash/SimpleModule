import { router } from '@inertiajs/react';
import { Button, Field, FieldGroup, Input, Label } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

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
    <ManageLayout activePage="Index">
      <h3 className="text-xl font-bold mb-4">Profile</h3>
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
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
    </ManageLayout>
  );
}
