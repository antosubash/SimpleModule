import { router } from '@inertiajs/react';
import {
  Button,
  Card,
  CardContent,
  Container,
  Field,
  FieldGroup,
  Input,
  Label,
} from '@simplemodule/ui';

interface Props {
  message?: string;
}

export default function ResendEmailConfirmation({ message }: Props) {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/Identity/Account/ResendEmailConfirmation', formData);
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-2">Resend email confirmation</h1>
              <p className="text-sm text-text-muted mb-6">Enter your email.</p>
              <hr className="mb-6" />
              {message && (
                <div className="alert-success mb-4 text-sm" role="alert">
                  {message}
                </div>
              )}
              <form onSubmit={handleSubmit}>
                <FieldGroup>
                  <Field>
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      name="email"
                      type="email"
                      required
                      placeholder="name@example.com"
                    />
                  </Field>
                  <Button type="submit" className="w-full">
                    Resend
                  </Button>
                </FieldGroup>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
