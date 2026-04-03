import { router } from '@inertiajs/react';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbSeparator,
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

export default function CreateTemplate() {
  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    router.post('/email/templates', data);
  };

  return (
    <PageShell title="Create Template" description="Create a new email template.">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/email/templates">Templates</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>Create</BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <Card>
        <CardHeader>
          <CardTitle>New Email Template</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="Name" required />
              </Field>
              <Field>
                <Label htmlFor="slug">Slug</Label>
                <Input id="slug" name="Slug" required placeholder="e.g. welcome-email" />
              </Field>
              <Field>
                <Label htmlFor="subject">Subject</Label>
                <Input
                  id="subject"
                  name="Subject"
                  required
                  placeholder="Use {{variable}} for placeholders"
                />
              </Field>
              <Field>
                <Label htmlFor="body">Body</Label>
                <Textarea
                  id="body"
                  name="Body"
                  required
                  rows={10}
                  placeholder="Email body content. Use {{variable}} for placeholders."
                />
              </Field>
              <Field className="flex items-center gap-2">
                <Checkbox id="isHtml" name="IsHtml" defaultChecked />
                <Label htmlFor="isHtml">HTML Email</Label>
              </Field>
            </FieldGroup>
            <div className="mt-4 flex gap-2">
              <Button type="submit">Create Template</Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => router.visit('/email/templates')}
              >
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
