import { router } from '@inertiajs/react';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import ManageLayout from '@/components/ManageLayout';

interface Props {
  username?: string;
  phoneNumber?: string;
  statusMessage?: string;
}

export default function ManageIndex({ username, phoneNumber, statusMessage }: Props) {
  const [confirmOpen, setConfirmOpen] = useState(false);

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage', formData);
  }

  function confirmSignOutEverywhere() {
    router.post('/Identity/Account/Manage/SignOutEverywhere');
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

      <hr className="my-8 border-border" />

      <section>
        <h3 className="text-xl font-bold mb-2">Security</h3>
        <p className="text-sm text-text-secondary mb-4">
          Sign out of every device you've ever signed in on, including this one. Useful if you've
          lost a device or suspect your account has been compromised.
        </p>
        <Button type="button" variant="danger" onClick={() => setConfirmOpen(true)}>
          Sign out everywhere
        </Button>
      </section>

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Sign out everywhere?</DialogTitle>
            <DialogDescription>
              This will sign you out of every device, including this one. You'll need to sign in
              again on this device. Continue?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => setConfirmOpen(false)}>
              Cancel
            </Button>
            <Button type="button" variant="danger" onClick={confirmSignOutEverywhere}>
              Sign out everywhere
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </ManageLayout>
  );
}
