import * as React from 'react';
import { Input, type InputProps } from './input';
import { Kbd } from './kbd';

interface SearchInputProps extends Omit<InputProps, 'type' | 'prefix'> {
  /** Optional keyboard shortcut hint shown as a Kbd on the right edge. e.g. `'⌘K'`. */
  shortcut?: string;
}

const SearchIcon = () => (
  <svg
    aria-hidden="true"
    width="14"
    height="14"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <circle cx="11" cy="11" r="8" />
    <path d="m21 21-4.3-4.3" />
  </svg>
);

const SearchInput = React.forwardRef<HTMLInputElement, SearchInputProps>(
  ({ shortcut, placeholder = 'Search…', suffix, ...props }, ref) => (
    <Input
      ref={ref}
      type="search"
      placeholder={placeholder}
      prefix={<SearchIcon />}
      suffix={suffix ?? (shortcut ? <Kbd>{shortcut}</Kbd> : undefined)}
      {...props}
    />
  ),
);
SearchInput.displayName = 'SearchInput';

export type { SearchInputProps };
export { SearchInput };
