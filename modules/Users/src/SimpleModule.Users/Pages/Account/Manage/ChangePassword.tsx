import { router } from '@inertiajs/react';
import { Button, Field, FieldGroup, Input, Label } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

interface Props {
  statusMessage?: string;
  errors?: string[];
}

export default function ChangePassword({ statusMessage, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/ChangePassword', formData);
  }

  return (
    <ManageLayout activePage="ChangePassword">
      <h3 className="text-xl font-bold mb-4">Change password</h3>
      {statusMessage && (
        <div className="alert-success mb-4 text-sm" role="alert">
          {statusMessage}
        </div>
      )}
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
            <Label htmlFor="oldPassword">Current password</Label>
            <Input
              id="oldPassword"
              name="oldPassword"
              type="password"
              required
              autoComplete="current-password"
            />
          </Field>
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
            Update password
          </Button>
        </FieldGroup>
      </form>
    </ManageLayout>
  );
}
