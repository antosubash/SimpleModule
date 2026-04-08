import { router } from '@inertiajs/react';
import { Button, Field, FieldGroup, Input, Label } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

interface Props {
  requirePassword: boolean;
  errors?: string[];
}

export default function DeletePersonalData({ requirePassword, errors }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/DeletePersonalData', formData);
  }

  return (
    <ManageLayout activePage="PersonalData">
      <h3 className="text-xl font-bold mb-4">Delete Personal Data</h3>
      <div className="alert-warning mb-4" role="alert">
        <p>
          <strong>
            Deleting this data will permanently remove your account, and this cannot be recovered.
          </strong>
        </p>
      </div>
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
          {requirePassword && (
            <Field>
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                name="password"
                type="password"
                required
                autoComplete="current-password"
              />
            </Field>
          )}
          <Button type="submit" variant="danger" className="w-full">
            Delete data and close my account
          </Button>
        </FieldGroup>
      </form>
    </ManageLayout>
  );
}
