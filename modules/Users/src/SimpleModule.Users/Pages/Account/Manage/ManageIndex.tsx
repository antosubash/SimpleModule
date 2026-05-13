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
  phoneNumber?: string | null;
  isPhoneNumberConfirmed?: boolean;
  pendingPhoneNumber?: string | null;
  statusMessage?: string;
}

export default function ManageIndex({
  username,
  phoneNumber,
  isPhoneNumberConfirmed,
  pendingPhoneNumber,
  statusMessage,
}: Props) {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [inputPhone, setInputPhone] = useState(pendingPhoneNumber ?? phoneNumber ?? '');

  function confirmSignOutEverywhere() {
    router.post('/Identity/Account/Manage/SignOutEverywhere');
  }

  function sendVerificationCode(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/SendPhoneVerificationCode', formData);
  }

  function verifyCode(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/Manage/ConfirmPhoneNumber', formData);
  }

  function removePhone() {
    router.post('/Identity/Account/Manage/RemovePhoneNumber');
  }

  const hasSavedPhone = !!phoneNumber;
  const showVerifiedBadge =
    isPhoneNumberConfirmed &&
    hasSavedPhone &&
    (!pendingPhoneNumber || pendingPhoneNumber === phoneNumber);

  return (
    <ManageLayout activePage="Index">
      <h3 className="text-xl font-bold mb-4">Profile</h3>
      {statusMessage && (
        <div
          className={`mb-4 text-sm ${statusMessage.startsWith('Error') ? 'alert-danger' : 'alert-success'}`}
          role="alert"
        >
          {statusMessage}
        </div>
      )}

      <FieldGroup>
        <Field>
          <Label>Username</Label>
          <Input value={username ?? ''} disabled placeholder="Username" />
        </Field>
      </FieldGroup>

      <hr className="my-8 border-border" />

      <section>
        <h3 className="text-xl font-bold mb-2">Phone number</h3>
        <p className="text-sm text-text-secondary mb-4">
          Verify your phone number to use it for account recovery and two-factor authentication.
        </p>

        <form onSubmit={sendVerificationCode}>
          <FieldGroup>
            <Field>
              <Label htmlFor="phoneNumber">Phone number</Label>
              <div className="flex items-center gap-2">
                <Input
                  id="phoneNumber"
                  name="phoneNumber"
                  value={inputPhone}
                  onChange={(e) => setInputPhone(e.target.value)}
                  placeholder="Please enter your phone number."
                />
                {showVerifiedBadge && (
                  <span className="text-success font-bold" title="Verified">
                    &#10003;
                  </span>
                )}
              </div>
            </Field>
            <div className="flex flex-wrap gap-2">
              <Button type="submit">
                {hasSavedPhone && inputPhone === phoneNumber && !isPhoneNumberConfirmed
                  ? 'Send verification code'
                  : inputPhone === phoneNumber && isPhoneNumberConfirmed
                    ? 'Resend verification code'
                    : 'Send verification code'}
              </Button>
              {hasSavedPhone && (
                <Button type="button" variant="outline" onClick={removePhone}>
                  Remove
                </Button>
              )}
            </div>
          </FieldGroup>
        </form>

        {pendingPhoneNumber && (
          <form onSubmit={verifyCode} className="mt-4">
            <FieldGroup>
              <input type="hidden" name="phoneNumber" value={pendingPhoneNumber} />
              <Field>
                <Label htmlFor="code">Enter the 6-digit code sent to {pendingPhoneNumber}</Label>
                <Input
                  id="code"
                  name="code"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  required
                  placeholder="123456"
                />
              </Field>
              <Button type="submit">Verify</Button>
            </FieldGroup>
          </form>
        )}
      </section>

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
