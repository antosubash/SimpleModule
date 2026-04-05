import { Card, CardContent, Container } from '@simplemodule/ui';

interface Props {
  email: string;
  displayConfirmAccountLink: boolean;
  emailConfirmationUrl?: string;
}

export default function RegisterConfirmation({
  displayConfirmAccountLink,
  emailConfirmationUrl,
}: Props) {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-4">Register confirmation</h1>
              {displayConfirmAccountLink ? (
                <p className="text-sm">
                  This app does not currently have a real email sender registered.{' '}
                  <a id="confirm-link" href={emailConfirmationUrl}>
                    Click here to confirm your account
                  </a>
                </p>
              ) : (
                <p className="text-sm">Please check your email to confirm your account.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
