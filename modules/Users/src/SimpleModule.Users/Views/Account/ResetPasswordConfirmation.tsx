import { Card, CardContent, Container } from '@simplemodule/ui';

export default function ResetPasswordConfirmation() {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-4">Reset password confirmation</h1>
              <p className="text-sm">
                Your password has been reset. Please{' '}
                <a href="/Identity/Account/Login">click here to log in</a>.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
