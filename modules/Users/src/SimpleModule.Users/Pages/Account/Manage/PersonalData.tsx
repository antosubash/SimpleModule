import { usePage } from '@inertiajs/react';
import { Button } from '@simplemodule/ui';
import ManageLayout from '@/components/ManageLayout';

export default function PersonalData() {
  const { csrfToken } = usePage<{ csrfToken?: string }>().props;

  return (
    <ManageLayout activePage="PersonalData">
      <h3 className="text-xl font-bold mb-4">Personal Data</h3>
      <p className="text-sm mb-4">
        Your account contains personal data that you have given us. This page allows you to download
        or delete that data.
      </p>
      <p className="text-sm font-bold mb-4">
        Deleting this data will permanently remove your account, and this cannot be recovered.
      </p>
      <form action="/api/users/download-personal-data" method="post" className="mb-4">
        <input type="hidden" name="__RequestVerificationToken" value={csrfToken ?? ''} />
        <Button type="submit">Download</Button>
      </form>
      <a
        href="/Identity/Account/Manage/DeletePersonalData"
        className="btn-danger inline-block no-underline"
      >
        Delete
      </a>
    </ManageLayout>
  );
}
