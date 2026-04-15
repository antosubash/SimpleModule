import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { useState } from 'react';
import { AdminKeys } from '@/Locales/keys';

interface UserDetail {
  id: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  isLockedOut: boolean;
  accessFailedCount: number;
  createdAt: string;
  lastLoginAt: string | null;
}

interface Props {
  user: UserDetail;
  isSelf: boolean;
  onReverify: () => void;
  onDisable2fa: () => void;
}

export function UserSecurityTab({ user, isSelf, onReverify, onDisable2fa }: Props) {
  const { t } = useTranslation('Admin');
  const [passwordError, setPasswordError] = useState<string | null>(null);

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.ResetPasswordTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              const formData = new FormData(e.currentTarget);
              if (formData.get('newPassword') !== formData.get('confirmPassword')) {
                setPasswordError(t(AdminKeys.UsersEdit.ErrorPasswordMismatch));
                return;
              }
              setPasswordError(null);
              router.post(`/admin/users/${user.id}/reset-password`, formData);
            }}
          >
            <FieldGroup>
              {passwordError && (
                <div className="rounded-lg border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger">
                  {passwordError}
                </div>
              )}
              <Field>
                <Label htmlFor="newPassword">{t(AdminKeys.UsersEdit.FieldNewPassword)}</Label>
                <Input id="newPassword" name="newPassword" type="password" required />
              </Field>
              <Field>
                <Label htmlFor="confirmPassword">
                  {t(AdminKeys.UsersEdit.FieldConfirmPassword)}
                </Label>
                <Input id="confirmPassword" name="confirmPassword" type="password" required />
              </Field>
              <Button type="submit">{t(AdminKeys.UsersEdit.ResetPasswordButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.AccountLockTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          {user.isLockedOut ? (
            <div>
              <p className="text-sm text-danger mb-3">
                {t(AdminKeys.UsersEdit.AccountLockedMessage)}
              </p>
              <Button
                variant="outline"
                onClick={() => router.post(`/admin/users/${user.id}/unlock`)}
              >
                {t(AdminKeys.UsersEdit.UnlockButton)}
              </Button>
            </div>
          ) : isSelf ? (
            <p className="text-sm text-text-muted">{t(AdminKeys.UsersEdit.CannotLockSelf)}</p>
          ) : (
            <div>
              <p className="text-sm text-success mb-3">
                {t(AdminKeys.UsersEdit.AccountActiveMessage)}
              </p>
              <Button variant="danger" onClick={() => router.post(`/admin/users/${user.id}/lock`)}>
                {t(AdminKeys.UsersEdit.LockButton)}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.EmailVerificationTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-3">
            {t(AdminKeys.UsersEdit.EmailVerificationStatus, {
              status: user.emailConfirmed
                ? t(AdminKeys.UsersEdit.EmailVerified)
                : t(AdminKeys.UsersEdit.EmailNotVerified),
            })}
          </p>
          {user.emailConfirmed && (
            <Button variant="outline" onClick={onReverify}>
              {t(AdminKeys.UsersEdit.ForceReverifyButton)}
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.TwoFactorTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-text-muted mb-3">
            {t(AdminKeys.UsersEdit.TwoFactorStatus, {
              status: user.twoFactorEnabled
                ? t(AdminKeys.UsersEdit.TwoFactorEnabled)
                : t(AdminKeys.UsersEdit.TwoFactorNotEnabled),
            })}
          </p>
          {user.twoFactorEnabled && (
            <Button variant="danger" onClick={onDisable2fa}>
              {t(AdminKeys.UsersEdit.Disable2faButton)}
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.LoginInfoTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-text-muted">{t(AdminKeys.UsersEdit.FailedLoginAttempts)}</span>
              <span className="ml-2 font-medium">{user.accessFailedCount}</span>
            </div>
            <div>
              <span className="text-text-muted">{t(AdminKeys.UsersEdit.LastLogin)}</span>
              <span className="ml-2 font-medium">
                {user.lastLoginAt
                  ? new Date(user.lastLoginAt).toLocaleString()
                  : t(AdminKeys.UsersEdit.LastLoginNever)}
              </span>
            </div>
            <div>
              <span className="text-text-muted">{t(AdminKeys.UsersEdit.CreatedAt)}</span>
              <span className="ml-2 font-medium">{new Date(user.createdAt).toLocaleString()}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </>
  );
}
