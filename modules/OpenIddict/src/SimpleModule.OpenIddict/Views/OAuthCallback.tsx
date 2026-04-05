import { useEffect } from 'react';

export default function OAuthCallback() {
  useEffect(() => {
    window.location.href = `/${window.location.search}`;
  }, []);

  return <p className="text-center text-muted">Completing authentication...</p>;
}
