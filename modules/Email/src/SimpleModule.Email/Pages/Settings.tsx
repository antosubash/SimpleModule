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
  PageShell,
  Textarea,
} from '@simplemodule/ui';
import { type FormEvent, useState } from 'react';
import { EmailKeys } from '../Locales/keys';

interface Props {
  provider: string;
  defaultFromAddress: string;
}

type SubmitResult =
  | { kind: 'success'; messageId: string }
  | { kind: 'error'; message: string }
  | null;

export default function Settings({ provider, defaultFromAddress }: Props) {
  const { t } = useTranslation('Email');

  const [to, setTo] = useState('');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<SubmitResult>(null);

  const configFields = [
    { label: t(EmailKeys.Settings.Provider), value: provider },
    { label: t(EmailKeys.Settings.DefaultFromAddress), value: defaultFromAddress },
  ];

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLoading(true);
    setResult(null);

    try {
      const response = await fetch('/api/email/messages/test-send', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ to, subject: subject || undefined, body: body || undefined }),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = (await response.json()) as { messageId: string; status: string };
      setResult({ kind: 'success', messageId: data.messageId });
      setTo('');
      setSubject('');
      setBody('');
    } catch (err) {
      console.error('Failed to send test email', err);
      setResult({ kind: 'error', message: t(EmailKeys.Settings.ErrorMessage) });
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageShell
      className="space-y-4 sm:space-y-6"
      title={t(EmailKeys.Settings.Title)}
      description={t(EmailKeys.Settings.Description)}
    >
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium">
            {t(EmailKeys.Settings.CurrentConfig)}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 p-4 sm:p-6">
          {configFields.map((field) => (
            <div key={field.label}>
              <p className="text-xs font-medium tracking-wide text-text-muted uppercase">
                {field.label}
              </p>
              <p className="mt-1 text-sm font-medium">{field.value}</p>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium">
            {t(EmailKeys.Settings.SendTestTitle)}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-4 sm:p-6">
          <form onSubmit={handleSubmit}>
            <FieldGroup>
              <Field>
                <Label htmlFor="to">{t(EmailKeys.Settings.ToLabel)}</Label>
                <Input
                  id="to"
                  type="email"
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                  required
                  disabled={loading}
                />
              </Field>
              <Field>
                <Label htmlFor="subject">{t(EmailKeys.Settings.SubjectLabel)}</Label>
                <Input
                  id="subject"
                  type="text"
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  disabled={loading}
                />
              </Field>
              <Field>
                <Label htmlFor="body">{t(EmailKeys.Settings.BodyLabel)}</Label>
                <Textarea
                  id="body"
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  rows={4}
                  disabled={loading}
                />
              </Field>
            </FieldGroup>

            {result?.kind === 'success' && (
              <p className="mt-4 text-sm text-success">
                {t(EmailKeys.Settings.SuccessMessage, { messageId: result.messageId })}
              </p>
            )}

            {result?.kind === 'error' && (
              <p className="mt-4 text-sm text-danger">{result.message}</p>
            )}

            <Button type="submit" disabled={loading} className="mt-4">
              {loading ? t(EmailKeys.Settings.SendingLabel) : t(EmailKeys.Settings.SendButton)}
            </Button>
          </form>
        </CardContent>
      </Card>
    </PageShell>
  );
}
