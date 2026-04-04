import { Card, CardContent, Container } from '@simplemodule/ui';

export default function ForgotPasswordConfirmation() {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-4">Forgot password confirmation</h1>
              <p className="text-sm">Please check your email to reset your password.</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
