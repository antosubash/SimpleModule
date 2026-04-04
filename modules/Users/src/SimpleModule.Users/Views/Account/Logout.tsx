import { router } from '@inertiajs/react';
import { Button, Card, CardContent, Container } from '@simplemodule/ui';

interface Props {
  isAuthenticated: boolean;
}

export default function Logout({ isAuthenticated }: Props) {
  function handleLogout(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    router.post('/Identity/Account/Logout');
  }

  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-sm text-center">
          {isAuthenticated ? (
            <Card>
              <CardContent className="p-8">
                <h1 className="text-xl font-bold mb-2" style={{ fontFamily: "'Sora',sans-serif" }}>
                  Log out?
                </h1>
                <p className="text-sm text-text-muted mb-6">
                  You'll need to sign in again to access your account.
                </p>
                <form onSubmit={handleLogout}>
                  <Button type="submit" className="w-full mb-3">
                    Log out
                  </Button>
                </form>
                <a
                  href="/"
                  className="text-sm text-text-muted no-underline hover:text-text transition-colors"
                >
                  Cancel
                </a>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="p-8">
                <h1 className="text-xl font-bold mb-2" style={{ fontFamily: "'Sora',sans-serif" }}>
                  Logged out
                </h1>
                <p className="text-sm text-text-muted mb-6">
                  You have been successfully logged out.
                </p>
                <a
                  href="/Identity/Account/Login"
                  className="btn-primary no-underline inline-block w-full"
                >
                  Sign in again
                </a>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </Container>
  );
}
