import { Card, CardContent, Container } from '@simplemodule/ui';

interface Props {
  message: string;
}

export default function ConfirmEmailChange({ message }: Props) {
  return (
    <Container size="sm">
      <div className="flex items-center justify-center min-h-[calc(100vh-12rem)]">
        <div className="w-full max-w-md">
          <Card>
            <CardContent className="p-8">
              <h1 className="text-xl font-bold mb-4">Confirm email change</h1>
              <p className="text-sm">{message}</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </Container>
  );
}
