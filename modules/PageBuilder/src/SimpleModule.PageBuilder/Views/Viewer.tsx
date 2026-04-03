import { Render } from '@puckeditor/core/rsc';
import { useTranslation } from '@simplemodule/client/use-translation';
import { Alert, AlertDescription, Container } from '@simplemodule/ui';
import { useMemo } from 'react';
import { PageBuilderKeys } from '../Locales/keys';
import { puckConfig } from '../puck/config';
import type { Page } from '../types';

interface Props {
  page: Page;
  isDraft?: boolean;
}

export default function Viewer({ page, isDraft }: Props) {
  const { t } = useTranslation('PageBuilder');
  const data = useMemo(() => {
    try {
      const parsed = JSON.parse(page.content);
      return {
        content: parsed.content ?? [],
        root: parsed.root ?? { props: {} },
      };
    } catch {
      return { content: [], root: { props: {} } };
    }
  }, [page.content]);

  return (
    <Container className="py-4 sm:py-8" data-testid="page-content">
      {isDraft && (
        <Alert variant="warning" className="mb-4 sm:mb-6">
          <AlertDescription>{t(PageBuilderKeys.Viewer.DraftBanner)}</AlertDescription>
        </Alert>
      )}
      <Render config={puckConfig} data={data} />
    </Container>
  );
}
