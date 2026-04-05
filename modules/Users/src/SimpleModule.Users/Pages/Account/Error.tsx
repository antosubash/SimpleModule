import { Card, CardContent, Container } from '@simplemodule/ui';

interface Props {
  requestId?: string;
}

export default function ErrorPage({ requestId }: Props) {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold text-danger mb-2">Error.</h1>
              <p className="text-sm text-danger mb-4">
                An error occurred while processing your request.
              </p>
              {requestId && (
                <p className="text-sm">
                  <strong>Request ID:</strong> <code>{requestId}</code>
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
