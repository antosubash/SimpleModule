import { Link } from '@inertiajs/react';
import { Button, Card, CardContent, Container } from '@simplemodule/ui';

interface Props {
  success: boolean;
  message: string;
}

export default function UnlockAccount({ success, message }: Props) {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1
                className={`text-xl font-bold mb-4 ${success ? 'text-success' : 'text-danger'}`}
              >
                {success ? 'Account unlocked' : 'Unlock failed'}
              </h1>
              <p className="text-sm mb-6">{message}</p>
              {success && (
                <Link href="/Identity/Account/Login">
                  <Button className="w-full">Sign in</Button>
                </Link>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
