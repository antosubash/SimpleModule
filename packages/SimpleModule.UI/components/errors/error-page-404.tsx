import { ErrorPageLayout, type ErrorPageProps } from './error-page-layout';

export default function ErrorPage404({ message }: ErrorPageProps) {
  return (
    <ErrorPageLayout
      statusCode={404}
      icon={
        <svg
          className="h-7 w-7 text-danger-text"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607ZM13.5 10.5h-6"
          />
        </svg>
      }
      title="Page not found"
      description={message ?? 'The page you are looking for does not exist or has been moved.'}
    />
  );
}
