import { router } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import { useTranslation } from '@simplemodule/client/use-translation';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
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
  PageShell,
  Textarea,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import { EmailKeys } from '../Locales/keys';
import type { EmailTemplate } from '../types';

export default function EditTemplate({ template }: { template: EmailTemplate }) {
  const { t } = useTranslation('Email');
  const [showDelete, setShowDelete] = useState(false);

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    router.post(`/email/templates/${template.id}`, data);
  };

  return (
    <PageShell
      title={t(EmailKeys.EditTemplate.Title)}
      description={t(EmailKeys.EditTemplate.Description, { name: template.name })}
      breadcrumbs={[
        {
          label: t(EmailKeys.EditTemplate.BreadcrumbTemplates),
          href: routes.email.views.templates(),
        },
        { label: t(EmailKeys.EditTemplate.Breadcrumb) },
      ]}
    >
      <Card>
        <CardHeader>
          <CardTitle>{t(EmailKeys.EditTemplate.CardTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">{t(EmailKeys.EditTemplate.NameLabel)}</Label>
                <Input id="name" name="Name" required defaultValue={template.name} />
              </Field>
              <Field>
                <Label htmlFor="subject">{t(EmailKeys.EditTemplate.SubjectLabel)}</Label>
                <Input id="subject" name="Subject" required defaultValue={template.subject} />
              </Field>
              <Field>
                <Label htmlFor="body">{t(EmailKeys.EditTemplate.BodyLabel)}</Label>
                <Textarea id="body" name="Body" required rows={10} defaultValue={template.body} />
              </Field>
              <Field className="flex items-center gap-2">
                <Checkbox id="isHtml" name="IsHtml" defaultChecked={template.isHtml} />
                <Label htmlFor="isHtml">{t(EmailKeys.EditTemplate.IsHtmlLabel)}</Label>
              </Field>
            </FieldGroup>
            <div className="mt-4 flex gap-2">
              <Button type="submit">{t(EmailKeys.EditTemplate.SaveChanges)}</Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => router.visit(routes.email.views.templates())}
              >
                {t(EmailKeys.EditTemplate.CancelButton)}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
      <Card className="mt-6 border-danger">
        <CardHeader>
          <CardTitle>{t(EmailKeys.EditTemplate.DangerZone)}</CardTitle>
        </CardHeader>
        <CardContent>
          <Button variant="danger" onClick={() => setShowDelete(true)}>
            {t(EmailKeys.EditTemplate.DeleteTemplate)}
          </Button>
        </CardContent>
      </Card>
      <Dialog open={showDelete} onOpenChange={setShowDelete}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t(EmailKeys.EditTemplate.DeleteDialog.Title)}</DialogTitle>
            <DialogDescription>
              {t(EmailKeys.EditTemplate.DeleteDialog.Confirm, { name: template.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="secondary" onClick={() => setShowDelete(false)}>
              {t(EmailKeys.EditTemplate.DeleteDialog.CancelButton)}
            </Button>
            <Button
              variant="danger"
              onClick={() =>
                router.delete(`/email/templates/${template.id}`, {
                  onSuccess: () => setShowDelete(false),
                })
              }
            >
              {t(EmailKeys.EditTemplate.DeleteDialog.DeleteButton)}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
}
