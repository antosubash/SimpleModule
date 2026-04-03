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
  PageShell,
  Textarea,
} from '@simplemodule/ui';
import type { FormEvent } from 'react';
import { EmailKeys } from '../Locales/keys';

export default function CreateTemplate() {
  const { t } = useTranslation('Email');

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    router.post('/email/templates', data);
  };

  return (
    <PageShell
      title={t(EmailKeys.CreateTemplate.Title)}
      description={t(EmailKeys.CreateTemplate.Description)}
      breadcrumbs={[
        { label: t(EmailKeys.CreateTemplate.BreadcrumbTemplates), href: '/email/templates' },
        { label: t(EmailKeys.CreateTemplate.Breadcrumb) },
      ]}
    >
      <Card>
        <CardHeader>
          <CardTitle>{t(EmailKeys.CreateTemplate.CardTitle)}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">{t(EmailKeys.CreateTemplate.NameLabel)}</Label>
                <Input id="name" name="Name" required />
              </Field>
              <Field>
                <Label htmlFor="slug">{t(EmailKeys.CreateTemplate.SlugLabel)}</Label>
                <Input
                  id="slug"
                  name="Slug"
                  required
                  placeholder={t(EmailKeys.CreateTemplate.SlugPlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="subject">{t(EmailKeys.CreateTemplate.SubjectLabel)}</Label>
                <Input
                  id="subject"
                  name="Subject"
                  required
                  placeholder={t(EmailKeys.CreateTemplate.SubjectPlaceholder)}
                />
              </Field>
              <Field>
                <Label htmlFor="body">{t(EmailKeys.CreateTemplate.BodyLabel)}</Label>
                <Textarea
                  id="body"
                  name="Body"
                  required
                  rows={10}
                  placeholder={t(EmailKeys.CreateTemplate.BodyPlaceholder)}
                />
              </Field>
              <Field className="flex items-center gap-2">
                <Checkbox id="isHtml" name="IsHtml" defaultChecked />
                <Label htmlFor="isHtml">{t(EmailKeys.CreateTemplate.IsHtmlLabel)}</Label>
              </Field>
            </FieldGroup>
            <div className="mt-4 flex gap-2">
              <Button type="submit">{t(EmailKeys.CreateTemplate.SubmitButton)}</Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => router.visit('/email/templates')}
              >
                {t(EmailKeys.CreateTemplate.CancelButton)}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
