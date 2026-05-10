import { Card, CardContent, Container } from '@simplemodule/ui';

export default function SendUnlockEmailConfirmation() {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-4">Check your email</h1>
              <p className="text-sm">
                If an account with that email exists and is currently locked, we've sent an unlock
                link. Please check your email.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
