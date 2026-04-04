import { Card, CardContent, Container } from '@simplemodule/ui';

export default function Lockout() {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold text-danger mb-2">Locked out</h1>
              <p className="text-sm text-danger">
                This account has been locked out, please try again later.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
