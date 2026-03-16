import * as React from 'react';
import { cn } from '../lib/utils';

interface SectionProps extends React.HTMLAttributes<HTMLElement> {
  title?: string;
}

const Section = React.forwardRef<HTMLElement, SectionProps>(({ className, title, children, ...props }, ref) => (
  <section ref={ref} className={cn('mb-8', className)} {...props}>
    {title && (
      <h2
        className="text-base font-bold mb-4 pb-3 flex items-center gap-2 before:content-[''] before:w-1 before:h-5 before:rounded-full before:bg-gradient-to-b before:from-primary before:to-accent"
        style={{ fontFamily: "'Sora', system-ui, sans-serif" }}
      >
        {title}
      </h2>
    )}
    {children}
  </section>
));
Section.displayName = 'Section';

export { Section };
export type { SectionProps };
