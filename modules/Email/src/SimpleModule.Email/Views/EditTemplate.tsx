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
import type { EmailTemplate } from '../types';

export default function EditTemplate({ template }: { template: EmailTemplate }) {
  const [showDelete, setShowDelete] = useState(false);

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const form = e.currentTarget;
    const data = new FormData(form);
    router.post(`/email/templates/${template.id}`, data);
  };

  return (
    <PageShell title="Edit Template" description={`Editing template: ${template.name}`}>
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/email/templates">Templates</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>Edit</BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <Card>
        <CardHeader>
          <CardTitle>Edit Email Template</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="Name" required defaultValue={template.name} />
              </Field>
              <Field>
                <Label htmlFor="subject">Subject</Label>
                <Input id="subject" name="Subject" required defaultValue={template.subject} />
              </Field>
              <Field>
                <Label htmlFor="body">Body</Label>
                <Textarea id="body" name="Body" required rows={10} defaultValue={template.body} />
              </Field>
              <Field className="flex items-center gap-2">
                <Checkbox id="isHtml" name="IsHtml" defaultChecked={template.isHtml} />
                <Label htmlFor="isHtml">HTML Email</Label>
              </Field>
            </FieldGroup>
            <div className="mt-4 flex gap-2">
              <Button type="submit">Save Changes</Button>
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
      <Card className="mt-6 border-destructive">
        <CardHeader>
          <CardTitle>Danger Zone</CardTitle>
        </CardHeader>
        <CardContent>
          <Button variant="destructive" onClick={() => setShowDelete(true)}>
            Delete Template
          </Button>
        </CardContent>
      </Card>
      <Dialog open={showDelete} onOpenChange={setShowDelete}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Template</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &quot;{template.name}&quot;? This action cannot be
              undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDelete(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() =>
                router.delete(`/email/templates/${template.id}`, {
                  onSuccess: () => setShowDelete(false),
                })
              }
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
}
