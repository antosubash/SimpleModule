import { router } from '@inertiajs/react';
import {
  Button,
  DataGridPage,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@simplemodule/ui';
import { useState } from 'react';
import type { EmailTemplate } from '../types';

export default function Templates({ templates }: { templates: EmailTemplate[] }) {
  const [deleteId, setDeleteId] = useState<number | null>(null);

  return (
    <DataGridPage
      title="Email Templates"
      description="Manage email templates for your application."
      actions={
        <Button onClick={() => router.visit('/email/templates/create')}>New Template</Button>
      }
    >
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Slug</TableHead>
            <TableHead>Subject</TableHead>
            <TableHead>HTML</TableHead>
            <TableHead className="w-[100px]">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {templates.map((t) => (
            <TableRow key={t.id}>
              <TableCell className="font-medium">{t.name}</TableCell>
              <TableCell className="text-text-muted">{t.slug}</TableCell>
              <TableCell>{t.subject}</TableCell>
              <TableCell>{t.isHtml ? 'Yes' : 'No'}</TableCell>
              <TableCell>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => router.visit(`/email/templates/${t.id}/edit`)}
                  >
                    Edit
                  </Button>
                  <Button variant="destructive" size="sm" onClick={() => setDeleteId(t.id)}>
                    Delete
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      <Dialog open={deleteId !== null} onOpenChange={() => setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Template</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this template? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteId(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (deleteId !== null) {
                  router.delete(`/email/templates/${deleteId}`, {
                    onSuccess: () => setDeleteId(null),
                  });
                }
              }}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </DataGridPage>
  );
}
