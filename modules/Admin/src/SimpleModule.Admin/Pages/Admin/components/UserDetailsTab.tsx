import { router } from '@inertiajs/react';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';
import { AdminKeys } from '@/Locales/keys';

interface UserDetail {
  id: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  isDeactivated: boolean;
}

interface Props {
  user: UserDetail;
  isSelf: boolean;
  onDeactivate: () => void;
}

export function UserDetailsTab({ user, isSelf, onDeactivate }: Props) {
  const { t } = useTranslation('Admin');

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.DetailsTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              router.post(`/admin/users/${user.id}`, new FormData(e.currentTarget));
            }}
          >
            <FieldGroup>
              <Field>
                <Label htmlFor="displayName">{t(AdminKeys.UsersEdit.FieldDisplayName)}</Label>
                <Input id="displayName" name="displayName" defaultValue={user.displayName} />
              </Field>
              <Field>
                <Label htmlFor="email">{t(AdminKeys.UsersEdit.FieldEmail)}</Label>
                <Input id="email" name="email" type="email" defaultValue={user.email} />
              </Field>
              <Field orientation="horizontal">
                <Checkbox
                  id="emailConfirmed"
                  name="emailConfirmed"
                  value="true"
                  defaultChecked={user.emailConfirmed}
                />
                <Label htmlFor="emailConfirmed" className="mb-0">
                  {t(AdminKeys.UsersEdit.FieldEmailConfirmed)}
                </Label>
              </Field>
              <Button type="submit">{t(AdminKeys.UsersEdit.SaveDetailsButton)}</Button>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t(AdminKeys.UsersEdit.AccountStatusTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          {user.isDeactivated ? (
            <div>
              <p className="text-sm text-text-muted mb-3">
                {t(AdminKeys.UsersEdit.AccountDeactivatedMessage)}
              </p>
              <Button
                variant="outline"
                onClick={() => router.post(`/admin/users/${user.id}/reactivate`)}
              >
                {t(AdminKeys.UsersEdit.ReactivateButton)}
              </Button>
            </div>
          ) : isSelf ? (
            <p className="text-sm text-text-muted">{t(AdminKeys.UsersEdit.CannotDeactivateSelf)}</p>
          ) : (
            <div>
              <p className="text-sm text-text-muted mb-3">
                {t(AdminKeys.UsersEdit.DeactivateWarning)}
              </p>
              <Button variant="danger" onClick={onDeactivate}>
                {t(AdminKeys.UsersEdit.DeactivateButton)}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </>
  );
}
