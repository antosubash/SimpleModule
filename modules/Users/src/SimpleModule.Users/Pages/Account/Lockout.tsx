import { Link } from '@inertiajs/react';
import { routes } from '@simplemodule/client/routes';
import { Card, CardContent, Container } from '@simplemodule/ui';

export default function Lockout() {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold text-danger mb-2">Locked out</h1>
              <p className="text-sm text-danger mb-4">
                This account has been locked out, please try again later.
              </p>
              <hr className="mb-4" />
              <p className="text-sm text-text-muted">
                You can also{' '}
                <Link
                  href={routes.users.views.sendUnlockEmail()}
                  className="text-primary underline hover:no-underline"
                >
                  receive an unlock link by email
                </Link>
                .
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
