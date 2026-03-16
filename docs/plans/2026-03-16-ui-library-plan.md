# @simplemodule/ui Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a shadcn-style shared React component library with ~18 components, a CLI tool, and Radix UI primitives — all themed via CSS variables.

**Architecture:** New npm workspace `@simplemodule/ui` at `src/SimpleModule.UI/`. Components are source TSX files styled with cva + Tailwind classes referencing `@simplemodule/theme-default` CSS variables. A CLI tool (`tools/add-component.mjs`) copies component templates into the active components directory.

**Tech Stack:** Radix UI, class-variance-authority (cva), clsx, tailwind-merge, React 19

**Design doc:** `docs/plans/2026-03-16-ui-library-design.md`

---

### Task 1: Scaffold `@simplemodule/ui` package and install dependencies

**Files:**
- Create: `src/SimpleModule.UI/package.json`
- Create: `src/SimpleModule.UI/lib/utils.ts`
- Create: `src/SimpleModule.UI/components/index.ts`
- Create: `src/SimpleModule.UI/registry/registry.json`
- Modify: `package.json` (root — add workspace)

**Step 1: Create package.json**

```json
{
  "private": true,
  "name": "@simplemodule/ui",
  "type": "module",
  "main": "components/index.ts",
  "exports": {
    ".": "./components/index.ts",
    "./lib/utils": "./lib/utils.ts"
  },
  "peerDependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0"
  },
  "dependencies": {
    "class-variance-authority": "^0.7.1",
    "clsx": "^2.1.1",
    "tailwind-merge": "^3.0.0",
    "@radix-ui/react-slot": "^1.2.0"
  }
}
```

**Step 2: Add workspace to root package.json**

Add `"src/SimpleModule.UI"` to the `workspaces` array in `package.json`:

```json
"workspaces": [
  "src/modules/*/src/*",
  "src/SimpleModule.Client",
  "src/SimpleModule.Theme.Default",
  "src/SimpleModule.UI",
  "src/SimpleModule.Host/ClientApp"
]
```

**Step 3: Create `cn()` utility**

Create `src/SimpleModule.UI/lib/utils.ts`:

```ts
import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

**Step 4: Create empty barrel export**

Create `src/SimpleModule.UI/components/index.ts`:

```ts
// Components are added here by the CLI tool (npm run ui:add)
```

**Step 5: Create empty registry**

Create `src/SimpleModule.UI/registry/registry.json`:

```json
{}
```

**Step 6: Install dependencies**

Run: `npm install`
Expected: Clean install with new workspace resolved

**Step 7: Verify TypeScript resolves the package**

Run: `npx tsc --noEmit --project tsconfig.json 2>&1 | head -5`
Expected: No errors related to `@simplemodule/ui`

**Step 8: Commit**

```bash
git add src/SimpleModule.UI/ package.json package-lock.json
git commit -m "feat: scaffold @simplemodule/ui workspace with cn() utility"
```

---

### Task 2: Add shadow tokens to theme

**Files:**
- Modify: `src/SimpleModule.Theme.Default/theme.css:6-64` (inside `@theme` block)

**Step 1: Add shadow tokens**

Add these lines at the end of the `@theme` block (before the closing `}`), after the `--color-muted` line:

```css
  /* --- Shadows (themeable) --- */
  --shadow-primary: 0 4px 14px rgba(13, 148, 136, 0.35);
  --shadow-primary-hover: 0 6px 20px rgba(13, 148, 136, 0.5);
  --shadow-danger: 0 4px 14px rgba(225, 29, 72, 0.25);
  --shadow-danger-hover: 0 6px 20px rgba(225, 29, 72, 0.4);
```

**Step 2: Verify the host app builds CSS**

Run: `cd src/SimpleModule.Host && dotnet build 2>&1 | tail -3`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/SimpleModule.Theme.Default/theme.css
git commit -m "feat: add shadow tokens to theme for themeable button/danger shadows"
```

---

### Task 3: Create Button component template

**Files:**
- Create: `src/SimpleModule.UI/registry/templates/button.tsx`
- Modify: `src/SimpleModule.UI/registry/registry.json`

**Step 1: Create button template**

Create `src/SimpleModule.UI/registry/templates/button.tsx`:

```tsx
import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl text-sm font-semibold transition-all duration-200 active:scale-[0.97] cursor-pointer disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        primary:
          'text-white bg-gradient-to-br from-primary to-accent shadow-(--shadow-primary) hover:shadow-(--shadow-primary-hover) hover:-translate-y-px',
        secondary:
          'bg-surface text-text border border-border hover:bg-surface-raised hover:border-border-strong',
        ghost: 'bg-transparent text-text-secondary hover:bg-primary-subtle hover:text-primary',
        danger:
          'text-white bg-danger shadow-(--shadow-danger) hover:bg-danger-hover hover:shadow-(--shadow-danger-hover) hover:-translate-y-px',
        outline:
          'bg-transparent text-primary border-2 border-primary/30 hover:bg-primary-subtle hover:border-primary',
      },
      size: {
        sm: 'px-3.5 py-1.5 text-xs rounded-lg',
        default: 'px-5 py-2.5',
        lg: 'px-8 py-3.5 text-base',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'default',
    },
  },
);

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';
    return <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />;
  },
);
Button.displayName = 'Button';

export { Button, buttonVariants };
export type { ButtonProps };
```

**Step 2: Add button to registry**

Update `src/SimpleModule.UI/registry/registry.json`:

```json
{
  "button": {
    "name": "Button",
    "file": "button.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-slot"],
    "exports": ["Button", "buttonVariants"]
  }
}
```

**Step 3: Commit**

```bash
git add src/SimpleModule.UI/registry/
git commit -m "feat: add Button component template to registry"
```

---

### Task 4: Create Input, Textarea, Label component templates

**Files:**
- Create: `src/SimpleModule.UI/registry/templates/input.tsx`
- Create: `src/SimpleModule.UI/registry/templates/textarea.tsx`
- Create: `src/SimpleModule.UI/registry/templates/label.tsx`
- Modify: `src/SimpleModule.UI/registry/registry.json`

**Step 1: Create input template**

Create `src/SimpleModule.UI/registry/templates/input.tsx`:

```tsx
import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const inputVariants = cva(
  'w-full px-4 py-3 bg-surface border rounded-xl text-sm text-text transition-all duration-200 placeholder:text-text-muted outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring',
  {
    variants: {
      variant: {
        default: 'border-border',
        error: 'border-danger focus:border-danger focus:ring-danger-bg',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
);

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement>, VariantProps<typeof inputVariants> {}

const Input = React.forwardRef<HTMLInputElement, InputProps>(({ className, variant, type, ...props }, ref) => {
  return <input type={type} className={cn(inputVariants({ variant, className }))} ref={ref} {...props} />;
});
Input.displayName = 'Input';

export { Input, inputVariants };
export type { InputProps };
```

**Step 2: Create textarea template**

Create `src/SimpleModule.UI/registry/templates/textarea.tsx`:

```tsx
import * as React from 'react';
import { cn } from '../lib/utils';

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(({ className, ...props }, ref) => {
  return (
    <textarea
      className={cn(
        'w-full px-4 py-3 bg-surface border border-border rounded-xl text-sm text-text transition-all duration-200 placeholder:text-text-muted outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring min-h-[80px] resize-y',
        className,
      )}
      ref={ref}
      {...props}
    />
  );
});
Textarea.displayName = 'Textarea';

export { Textarea };
export type { TextareaProps };
```

**Step 3: Create label template**

Create `src/SimpleModule.UI/registry/templates/label.tsx`:

```tsx
import * as React from 'react';
import * as LabelPrimitive from '@radix-ui/react-label';
import { cn } from '../lib/utils';

const Label = React.forwardRef<
  React.ComponentRef<typeof LabelPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof LabelPrimitive.Root>
>(({ className, ...props }, ref) => (
  <LabelPrimitive.Root
    ref={ref}
    className={cn('block mb-1.5 font-medium text-sm text-text-secondary', className)}
    {...props}
  />
));
Label.displayName = 'Label';

export { Label };
```

**Step 4: Update registry.json**

Add to the registry object:

```json
{
  "button": { ... },
  "input": {
    "name": "Input",
    "file": "input.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Input", "inputVariants"]
  },
  "textarea": {
    "name": "Textarea",
    "file": "textarea.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Textarea"]
  },
  "label": {
    "name": "Label",
    "file": "label.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-label"],
    "exports": ["Label"]
  }
}
```

**Step 5: Commit**

```bash
git add src/SimpleModule.UI/registry/
git commit -m "feat: add Input, Textarea, Label component templates"
```

---

### Task 5: Create Select, Checkbox, Radio Group, Switch component templates

**Files:**
- Create: `src/SimpleModule.UI/registry/templates/select.tsx`
- Create: `src/SimpleModule.UI/registry/templates/checkbox.tsx`
- Create: `src/SimpleModule.UI/registry/templates/radio-group.tsx`
- Create: `src/SimpleModule.UI/registry/templates/switch.tsx`
- Modify: `src/SimpleModule.UI/registry/registry.json`

**Step 1: Create select template**

Create `src/SimpleModule.UI/registry/templates/select.tsx`:

```tsx
import * as React from 'react';
import * as SelectPrimitive from '@radix-ui/react-select';
import { cn } from '../lib/utils';

const Select = SelectPrimitive.Root;
const SelectGroup = SelectPrimitive.Group;
const SelectValue = SelectPrimitive.Value;

const SelectTrigger = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Trigger>
>(({ className, children, ...props }, ref) => (
  <SelectPrimitive.Trigger
    ref={ref}
    className={cn(
      'flex h-11 w-full items-center justify-between rounded-xl border border-border bg-surface px-4 py-3 text-sm text-text transition-all duration-200 outline-none focus:border-primary focus:ring-4 focus:ring-primary-ring disabled:cursor-not-allowed disabled:opacity-50 [&>span]:line-clamp-1',
      className,
    )}
    {...props}
  >
    {children}
    <SelectPrimitive.Icon asChild>
      <svg className="h-4 w-4 opacity-50" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
        <path d="m6 9 6 6 6-6" />
      </svg>
    </SelectPrimitive.Icon>
  </SelectPrimitive.Trigger>
));
SelectTrigger.displayName = SelectPrimitive.Trigger.displayName;

const SelectContent = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Content>
>(({ className, children, position = 'popper', ...props }, ref) => (
  <SelectPrimitive.Portal>
    <SelectPrimitive.Content
      ref={ref}
      className={cn(
        'relative z-50 max-h-96 min-w-[8rem] overflow-hidden rounded-xl border border-border bg-surface shadow-lg data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
        position === 'popper' && 'data-[side=bottom]:translate-y-1 data-[side=top]:-translate-y-1',
        className,
      )}
      position={position}
      {...props}
    >
      <SelectPrimitive.Viewport
        className={cn('p-1', position === 'popper' && 'h-[var(--radix-select-trigger-height)] w-full min-w-[var(--radix-select-trigger-width)]')}
      >
        {children}
      </SelectPrimitive.Viewport>
    </SelectPrimitive.Content>
  </SelectPrimitive.Portal>
));
SelectContent.displayName = SelectPrimitive.Content.displayName;

const SelectItem = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Item>
>(({ className, children, ...props }, ref) => (
  <SelectPrimitive.Item
    ref={ref}
    className={cn(
      'relative flex w-full cursor-default select-none items-center rounded-lg py-2 pl-8 pr-2 text-sm text-text outline-none focus:bg-primary-subtle focus:text-primary data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
      className,
    )}
    {...props}
  >
    <span className="absolute left-2 flex h-3.5 w-3.5 items-center justify-center">
      <SelectPrimitive.ItemIndicator>
        <svg className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path d="M20 6 9 17l-5-5" />
        </svg>
      </SelectPrimitive.ItemIndicator>
    </span>
    <SelectPrimitive.ItemText>{children}</SelectPrimitive.ItemText>
  </SelectPrimitive.Item>
));
SelectItem.displayName = SelectPrimitive.Item.displayName;

const SelectSeparator = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Separator>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Separator>
>(({ className, ...props }, ref) => (
  <SelectPrimitive.Separator ref={ref} className={cn('-mx-1 my-1 h-px bg-border', className)} {...props} />
));
SelectSeparator.displayName = SelectPrimitive.Separator.displayName;

export { Select, SelectGroup, SelectValue, SelectTrigger, SelectContent, SelectItem, SelectSeparator };
```

**Step 2: Create checkbox template**

Create `src/SimpleModule.UI/registry/templates/checkbox.tsx`:

```tsx
import * as React from 'react';
import * as CheckboxPrimitive from '@radix-ui/react-checkbox';
import { cn } from '../lib/utils';

const Checkbox = React.forwardRef<
  React.ComponentRef<typeof CheckboxPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof CheckboxPrimitive.Root>
>(({ className, ...props }, ref) => (
  <CheckboxPrimitive.Root
    ref={ref}
    className={cn(
      'peer h-5 w-5 shrink-0 rounded-md border border-border bg-surface transition-all duration-200 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring disabled:cursor-not-allowed disabled:opacity-50 data-[state=checked]:bg-primary data-[state=checked]:border-primary data-[state=checked]:text-text-inverse',
      className,
    )}
    {...props}
  >
    <CheckboxPrimitive.Indicator className="flex items-center justify-center text-current">
      <svg className="h-3.5 w-3.5" fill="none" stroke="currentColor" strokeWidth="3" viewBox="0 0 24 24">
        <path d="M20 6 9 17l-5-5" />
      </svg>
    </CheckboxPrimitive.Indicator>
  </CheckboxPrimitive.Root>
));
Checkbox.displayName = CheckboxPrimitive.Root.displayName;

export { Checkbox };
```

**Step 3: Create radio-group template**

Create `src/SimpleModule.UI/registry/templates/radio-group.tsx`:

```tsx
import * as React from 'react';
import * as RadioGroupPrimitive from '@radix-ui/react-radio-group';
import { cn } from '../lib/utils';

const RadioGroup = React.forwardRef<
  React.ComponentRef<typeof RadioGroupPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof RadioGroupPrimitive.Root>
>(({ className, ...props }, ref) => (
  <RadioGroupPrimitive.Root className={cn('grid gap-2', className)} {...props} ref={ref} />
));
RadioGroup.displayName = RadioGroupPrimitive.Root.displayName;

const RadioGroupItem = React.forwardRef<
  React.ComponentRef<typeof RadioGroupPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof RadioGroupPrimitive.Item>
>(({ className, ...props }, ref) => (
  <RadioGroupPrimitive.Item
    ref={ref}
    className={cn(
      'aspect-square h-5 w-5 rounded-full border border-border bg-surface text-primary transition-all duration-200 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring disabled:cursor-not-allowed disabled:opacity-50',
      className,
    )}
    {...props}
  >
    <RadioGroupPrimitive.Indicator className="flex items-center justify-center">
      <svg className="h-2.5 w-2.5 fill-current" viewBox="0 0 24 24">
        <circle cx="12" cy="12" r="12" />
      </svg>
    </RadioGroupPrimitive.Indicator>
  </RadioGroupPrimitive.Item>
));
RadioGroupItem.displayName = RadioGroupPrimitive.Item.displayName;

export { RadioGroup, RadioGroupItem };
```

**Step 4: Create switch template**

Create `src/SimpleModule.UI/registry/templates/switch.tsx`:

```tsx
import * as React from 'react';
import * as SwitchPrimitive from '@radix-ui/react-switch';
import { cn } from '../lib/utils';

const Switch = React.forwardRef<
  React.ComponentRef<typeof SwitchPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof SwitchPrimitive.Root>
>(({ className, ...props }, ref) => (
  <SwitchPrimitive.Root
    className={cn(
      'peer inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors duration-200 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring disabled:cursor-not-allowed disabled:opacity-50 data-[state=checked]:bg-primary data-[state=unchecked]:bg-border-strong',
      className,
    )}
    {...props}
    ref={ref}
  >
    <SwitchPrimitive.Thumb
      className={cn(
        'pointer-events-none block h-5 w-5 rounded-full bg-white shadow-lg ring-0 transition-transform duration-200 data-[state=checked]:translate-x-5 data-[state=unchecked]:translate-x-0',
      )}
    />
  </SwitchPrimitive.Root>
));
Switch.displayName = SwitchPrimitive.Root.displayName;

export { Switch };
```

**Step 5: Update registry.json**

Add entries for select, checkbox, radio-group, switch:

```json
{
  "select": {
    "name": "Select",
    "file": "select.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-select"],
    "exports": ["Select", "SelectGroup", "SelectValue", "SelectTrigger", "SelectContent", "SelectItem", "SelectSeparator"]
  },
  "checkbox": {
    "name": "Checkbox",
    "file": "checkbox.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-checkbox"],
    "exports": ["Checkbox"]
  },
  "radio-group": {
    "name": "RadioGroup",
    "file": "radio-group.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-radio-group"],
    "exports": ["RadioGroup", "RadioGroupItem"]
  },
  "switch": {
    "name": "Switch",
    "file": "switch.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-switch"],
    "exports": ["Switch"]
  }
}
```

**Step 6: Commit**

```bash
git add src/SimpleModule.UI/registry/
git commit -m "feat: add Select, Checkbox, RadioGroup, Switch templates"
```

---

### Task 6: Create Dialog, Dropdown Menu, Popover, Tabs component templates

**Files:**
- Create: `src/SimpleModule.UI/registry/templates/dialog.tsx`
- Create: `src/SimpleModule.UI/registry/templates/dropdown-menu.tsx`
- Create: `src/SimpleModule.UI/registry/templates/popover.tsx`
- Create: `src/SimpleModule.UI/registry/templates/tabs.tsx`
- Modify: `src/SimpleModule.UI/registry/registry.json`

**Step 1: Create dialog template**

Create `src/SimpleModule.UI/registry/templates/dialog.tsx`:

```tsx
import * as React from 'react';
import * as DialogPrimitive from '@radix-ui/react-dialog';
import { cn } from '../lib/utils';

const Dialog = DialogPrimitive.Root;
const DialogTrigger = DialogPrimitive.Trigger;
const DialogClose = DialogPrimitive.Close;
const DialogPortal = DialogPrimitive.Portal;

const DialogOverlay = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Overlay>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Overlay
    ref={ref}
    className={cn(
      'fixed inset-0 z-50 bg-dark/60 backdrop-blur-sm data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
      className,
    )}
    {...props}
  />
));
DialogOverlay.displayName = DialogPrimitive.Overlay.displayName;

const DialogContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content>
>(({ className, children, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content
      ref={ref}
      className={cn(
        'fixed left-[50%] top-[50%] z-50 grid w-full max-w-lg translate-x-[-50%] translate-y-[-50%] gap-4 border border-border bg-surface p-6 shadow-lg rounded-2xl duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
        className,
      )}
      {...props}
    >
      {children}
      <DialogPrimitive.Close className="absolute right-4 top-4 rounded-lg p-1 text-text-muted transition-colors hover:text-text outline-none focus-visible:ring-4 focus-visible:ring-primary-ring">
        <svg className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path d="M18 6 6 18M6 6l12 12" />
        </svg>
        <span className="sr-only">Close</span>
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPortal>
));
DialogContent.displayName = DialogPrimitive.Content.displayName;

function DialogHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('flex flex-col gap-1.5', className)} {...props} />;
}

function DialogFooter({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('flex flex-col-reverse sm:flex-row sm:justify-end sm:gap-2', className)} {...props} />;
}

const DialogTitle = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Title>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Title>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Title ref={ref} className={cn('text-lg font-semibold text-text', className)} {...props} />
));
DialogTitle.displayName = DialogPrimitive.Title.displayName;

const DialogDescription = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Description>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Description>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Description ref={ref} className={cn('text-sm text-text-secondary', className)} {...props} />
));
DialogDescription.displayName = DialogPrimitive.Description.displayName;

export {
  Dialog,
  DialogPortal,
  DialogOverlay,
  DialogClose,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
};
```

**Step 2: Create dropdown-menu template**

Create `src/SimpleModule.UI/registry/templates/dropdown-menu.tsx`:

```tsx
import * as React from 'react';
import * as DropdownMenuPrimitive from '@radix-ui/react-dropdown-menu';
import { cn } from '../lib/utils';

const DropdownMenu = DropdownMenuPrimitive.Root;
const DropdownMenuTrigger = DropdownMenuPrimitive.Trigger;
const DropdownMenuGroup = DropdownMenuPrimitive.Group;
const DropdownMenuSub = DropdownMenuPrimitive.Sub;

const DropdownMenuContent = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Content>
>(({ className, sideOffset = 4, ...props }, ref) => (
  <DropdownMenuPrimitive.Portal>
    <DropdownMenuPrimitive.Content
      ref={ref}
      sideOffset={sideOffset}
      className={cn(
        'z-50 min-w-[8rem] overflow-hidden rounded-xl border border-border bg-surface p-1.5 shadow-lg data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
        className,
      )}
      {...props}
    />
  </DropdownMenuPrimitive.Portal>
));
DropdownMenuContent.displayName = DropdownMenuPrimitive.Content.displayName;

const DropdownMenuItem = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Item> & { inset?: boolean }
>(({ className, inset, ...props }, ref) => (
  <DropdownMenuPrimitive.Item
    ref={ref}
    className={cn(
      'relative flex cursor-default select-none items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-text-secondary outline-none transition-colors focus:bg-surface-raised focus:text-text data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
      inset && 'pl-8',
      className,
    )}
    {...props}
  />
));
DropdownMenuItem.displayName = DropdownMenuPrimitive.Item.displayName;

const DropdownMenuSeparator = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Separator>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Separator>
>(({ className, ...props }, ref) => (
  <DropdownMenuPrimitive.Separator ref={ref} className={cn('-mx-1 my-1.5 h-px bg-border', className)} {...props} />
));
DropdownMenuSeparator.displayName = DropdownMenuPrimitive.Separator.displayName;

const DropdownMenuLabel = React.forwardRef<
  React.ComponentRef<typeof DropdownMenuPrimitive.Label>,
  React.ComponentPropsWithoutRef<typeof DropdownMenuPrimitive.Label> & { inset?: boolean }
>(({ className, inset, ...props }, ref) => (
  <DropdownMenuPrimitive.Label
    ref={ref}
    className={cn('px-3 py-1.5 text-xs font-semibold text-text-muted uppercase tracking-wider', inset && 'pl-8', className)}
    {...props}
  />
));
DropdownMenuLabel.displayName = DropdownMenuPrimitive.Label.displayName;

export {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuLabel,
  DropdownMenuGroup,
  DropdownMenuSub,
};
```

**Step 3: Create popover template**

Create `src/SimpleModule.UI/registry/templates/popover.tsx`:

```tsx
import * as React from 'react';
import * as PopoverPrimitive from '@radix-ui/react-popover';
import { cn } from '../lib/utils';

const Popover = PopoverPrimitive.Root;
const PopoverTrigger = PopoverPrimitive.Trigger;

const PopoverContent = React.forwardRef<
  React.ComponentRef<typeof PopoverPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof PopoverPrimitive.Content>
>(({ className, align = 'center', sideOffset = 4, ...props }, ref) => (
  <PopoverPrimitive.Portal>
    <PopoverPrimitive.Content
      ref={ref}
      align={align}
      sideOffset={sideOffset}
      className={cn(
        'z-50 w-72 rounded-xl border border-border bg-surface p-4 shadow-lg outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
        className,
      )}
      {...props}
    />
  </PopoverPrimitive.Portal>
));
PopoverContent.displayName = PopoverPrimitive.Content.displayName;

export { Popover, PopoverTrigger, PopoverContent };
```

**Step 4: Create tabs template**

Create `src/SimpleModule.UI/registry/templates/tabs.tsx`:

```tsx
import * as React from 'react';
import * as TabsPrimitive from '@radix-ui/react-tabs';
import { cn } from '../lib/utils';

const Tabs = TabsPrimitive.Root;

const TabsList = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.List>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.List
    ref={ref}
    className={cn('inline-flex items-center gap-1 rounded-xl bg-surface-sunken p-1', className)}
    {...props}
  />
));
TabsList.displayName = TabsPrimitive.List.displayName;

const TabsTrigger = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Trigger
    ref={ref}
    className={cn(
      'inline-flex items-center justify-center whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium text-text-muted transition-all duration-200 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-surface data-[state=active]:text-text data-[state=active]:shadow-sm',
      className,
    )}
    {...props}
  />
));
TabsTrigger.displayName = TabsPrimitive.Trigger.displayName;

const TabsContent = React.forwardRef<
  React.ComponentRef<typeof TabsPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>
>(({ className, ...props }, ref) => (
  <TabsPrimitive.Content
    ref={ref}
    className={cn('mt-3 outline-none focus-visible:ring-4 focus-visible:ring-primary-ring', className)}
    {...props}
  />
));
TabsContent.displayName = TabsPrimitive.Content.displayName;

export { Tabs, TabsList, TabsTrigger, TabsContent };
```

**Step 5: Update registry.json**

Add entries for dialog, dropdown-menu, popover, tabs.

```json
{
  "dialog": {
    "name": "Dialog",
    "file": "dialog.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-dialog"],
    "exports": ["Dialog", "DialogPortal", "DialogOverlay", "DialogClose", "DialogTrigger", "DialogContent", "DialogHeader", "DialogFooter", "DialogTitle", "DialogDescription"]
  },
  "dropdown-menu": {
    "name": "DropdownMenu",
    "file": "dropdown-menu.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-dropdown-menu"],
    "exports": ["DropdownMenu", "DropdownMenuTrigger", "DropdownMenuContent", "DropdownMenuItem", "DropdownMenuSeparator", "DropdownMenuLabel", "DropdownMenuGroup", "DropdownMenuSub"]
  },
  "popover": {
    "name": "Popover",
    "file": "popover.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-popover"],
    "exports": ["Popover", "PopoverTrigger", "PopoverContent"]
  },
  "tabs": {
    "name": "Tabs",
    "file": "tabs.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-tabs"],
    "exports": ["Tabs", "TabsList", "TabsTrigger", "TabsContent"]
  }
}
```

**Step 6: Commit**

```bash
git add src/SimpleModule.UI/registry/
git commit -m "feat: add Dialog, DropdownMenu, Popover, Tabs templates"
```

---

### Task 7: Create Table, Card, Badge, Alert, Separator, Spinner component templates

**Files:**
- Create: `src/SimpleModule.UI/registry/templates/table.tsx`
- Create: `src/SimpleModule.UI/registry/templates/card.tsx`
- Create: `src/SimpleModule.UI/registry/templates/badge.tsx`
- Create: `src/SimpleModule.UI/registry/templates/alert.tsx`
- Create: `src/SimpleModule.UI/registry/templates/separator.tsx`
- Create: `src/SimpleModule.UI/registry/templates/spinner.tsx`
- Modify: `src/SimpleModule.UI/registry/registry.json`

**Step 1: Create table template**

Create `src/SimpleModule.UI/registry/templates/table.tsx`:

```tsx
import * as React from 'react';
import { cn } from '../lib/utils';

const Table = React.forwardRef<HTMLTableElement, React.HTMLAttributes<HTMLTableElement>>(
  ({ className, ...props }, ref) => (
    <div className="relative w-full overflow-auto">
      <table ref={ref} className={cn('w-full caption-bottom text-sm', className)} {...props} />
    </div>
  ),
);
Table.displayName = 'Table';

const TableHeader = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
  ({ className, ...props }, ref) => <thead ref={ref} className={cn('[&_tr]:border-b', className)} {...props} />,
);
TableHeader.displayName = 'TableHeader';

const TableBody = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
  ({ className, ...props }, ref) => (
    <tbody ref={ref} className={cn('[&_tr:last-child]:border-0', className)} {...props} />
  ),
);
TableBody.displayName = 'TableBody';

const TableRow = React.forwardRef<HTMLTableRowElement, React.HTMLAttributes<HTMLTableRowElement>>(
  ({ className, ...props }, ref) => (
    <tr
      ref={ref}
      className={cn('border-b border-border transition-colors hover:bg-surface-raised', className)}
      {...props}
    />
  ),
);
TableRow.displayName = 'TableRow';

const TableHead = React.forwardRef<HTMLTableCellElement, React.ThHTMLAttributes<HTMLTableCellElement>>(
  ({ className, ...props }, ref) => (
    <th
      ref={ref}
      className={cn(
        'h-10 px-4 text-left align-middle text-xs font-semibold text-text-muted uppercase tracking-wider [&:has([role=checkbox])]:pr-0',
        className,
      )}
      {...props}
    />
  ),
);
TableHead.displayName = 'TableHead';

const TableCell = React.forwardRef<HTMLTableCellElement, React.TdHTMLAttributes<HTMLTableCellElement>>(
  ({ className, ...props }, ref) => (
    <td
      ref={ref}
      className={cn('px-4 py-2.5 align-middle text-text-secondary [&:has([role=checkbox])]:pr-0', className)}
      {...props}
    />
  ),
);
TableCell.displayName = 'TableCell';

export { Table, TableHeader, TableBody, TableRow, TableHead, TableCell };
```

**Step 2: Create card template**

Create `src/SimpleModule.UI/registry/templates/card.tsx`:

```tsx
import * as React from 'react';
import { cn } from '../lib/utils';

const Card = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn('bg-surface border border-border rounded-2xl p-5 transition-all duration-200 hover:border-border-strong', className)}
    {...props}
  />
));
Card.displayName = 'Card';

const CardHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => <div ref={ref} className={cn('flex flex-col gap-1.5 pb-4', className)} {...props} />,
);
CardHeader.displayName = 'CardHeader';

const CardTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => (
    <h3 ref={ref} className={cn('text-lg font-semibold leading-none tracking-tight', className)} {...props} />
  ),
);
CardTitle.displayName = 'CardTitle';

const CardDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => <p ref={ref} className={cn('text-sm text-text-secondary', className)} {...props} />,
);
CardDescription.displayName = 'CardDescription';

const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => <div ref={ref} className={cn('', className)} {...props} />,
);
CardContent.displayName = 'CardContent';

const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('flex items-center pt-4', className)} {...props} />
  ),
);
CardFooter.displayName = 'CardFooter';

export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
```

**Step 3: Create badge template**

Create `src/SimpleModule.UI/registry/templates/badge.tsx`:

```tsx
import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const badgeVariants = cva('inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold', {
  variants: {
    variant: {
      default: 'bg-surface-raised text-text-secondary',
      success: 'bg-success-bg text-success-text',
      danger: 'bg-danger-bg text-danger-text',
      warning: 'bg-warning-bg text-warning-text',
      info: 'bg-info-bg text-primary',
    },
  },
  defaultVariants: {
    variant: 'default',
  },
});

interface BadgeProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export { Badge, badgeVariants };
export type { BadgeProps };
```

**Step 4: Create alert template**

Create `src/SimpleModule.UI/registry/templates/alert.tsx`:

```tsx
import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const alertVariants = cva('p-4 rounded-xl text-sm border', {
  variants: {
    variant: {
      success: 'bg-success-bg border-success/20 text-success-text',
      danger: 'bg-danger-bg border-danger/20 text-danger-text',
      warning: 'bg-warning-bg border-warning-border text-warning-text',
      info: 'bg-info-bg border-info/20 text-primary',
    },
  },
  defaultVariants: {
    variant: 'info',
  },
});

interface AlertProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof alertVariants> {}

const Alert = React.forwardRef<HTMLDivElement, AlertProps>(({ className, variant, ...props }, ref) => (
  <div ref={ref} role="alert" className={cn(alertVariants({ variant }), className)} {...props} />
));
Alert.displayName = 'Alert';

const AlertTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => (
    <h5 ref={ref} className={cn('mb-1 font-semibold leading-none tracking-tight', className)} {...props} />
  ),
);
AlertTitle.displayName = 'AlertTitle';

const AlertDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('text-sm [&_p]:leading-relaxed', className)} {...props} />
  ),
);
AlertDescription.displayName = 'AlertDescription';

export { Alert, AlertTitle, AlertDescription, alertVariants };
export type { AlertProps };
```

**Step 5: Create separator template**

Create `src/SimpleModule.UI/registry/templates/separator.tsx`:

```tsx
import * as React from 'react';
import * as SeparatorPrimitive from '@radix-ui/react-separator';
import { cn } from '../lib/utils';

const Separator = React.forwardRef<
  React.ComponentRef<typeof SeparatorPrimitive.Root>,
  React.ComponentPropsWithoutRef<typeof SeparatorPrimitive.Root>
>(({ className, orientation = 'horizontal', decorative = true, ...props }, ref) => (
  <SeparatorPrimitive.Root
    ref={ref}
    decorative={decorative}
    orientation={orientation}
    className={cn('shrink-0 bg-border', orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px', className)}
    {...props}
  />
));
Separator.displayName = SeparatorPrimitive.Root.displayName;

export { Separator };
```

**Step 6: Create spinner template**

Create `src/SimpleModule.UI/registry/templates/spinner.tsx`:

```tsx
import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '../lib/utils';

const spinnerVariants = cva('inline-block border-2 border-border border-t-primary rounded-full animate-spin', {
  variants: {
    size: {
      sm: 'w-3 h-3',
      default: 'w-4 h-4',
      lg: 'w-6 h-6',
    },
  },
  defaultVariants: {
    size: 'default',
  },
});

interface SpinnerProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof spinnerVariants> {}

function Spinner({ className, size, ...props }: SpinnerProps) {
  return <div className={cn(spinnerVariants({ size }), className)} role="status" aria-label="Loading" {...props} />;
}

export { Spinner, spinnerVariants };
export type { SpinnerProps };
```

**Step 7: Update registry.json with all 6 components**

```json
{
  "table": {
    "name": "Table",
    "file": "table.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Table", "TableHeader", "TableBody", "TableRow", "TableHead", "TableCell"]
  },
  "card": {
    "name": "Card",
    "file": "card.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Card", "CardHeader", "CardTitle", "CardDescription", "CardContent", "CardFooter"]
  },
  "badge": {
    "name": "Badge",
    "file": "badge.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Badge", "badgeVariants"]
  },
  "alert": {
    "name": "Alert",
    "file": "alert.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Alert", "AlertTitle", "AlertDescription", "alertVariants"]
  },
  "separator": {
    "name": "Separator",
    "file": "separator.tsx",
    "dependencies": [],
    "radixPackages": ["@radix-ui/react-separator"],
    "exports": ["Separator"]
  },
  "spinner": {
    "name": "Spinner",
    "file": "spinner.tsx",
    "dependencies": [],
    "radixPackages": [],
    "exports": ["Spinner", "spinnerVariants"]
  }
}
```

**Step 8: Commit**

```bash
git add src/SimpleModule.UI/registry/
git commit -m "feat: add Table, Card, Badge, Alert, Separator, Spinner templates"
```

---

### Task 8: Build the CLI tool

**Files:**
- Create: `tools/add-component.mjs`
- Modify: `package.json` (root — add `ui:add` script)

**Step 1: Create the CLI script**

Create `tools/add-component.mjs`:

```js
#!/usr/bin/env node

import { readFileSync, writeFileSync, copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..');
const UI_DIR = resolve(ROOT, 'src/SimpleModule.UI');
const REGISTRY_PATH = resolve(UI_DIR, 'registry/registry.json');
const TEMPLATES_DIR = resolve(UI_DIR, 'registry/templates');
const COMPONENTS_DIR = resolve(UI_DIR, 'components');
const INDEX_PATH = resolve(COMPONENTS_DIR, 'index.ts');

function loadRegistry() {
  return JSON.parse(readFileSync(REGISTRY_PATH, 'utf-8'));
}

function listComponents(registry) {
  const names = Object.keys(registry).sort();
  if (names.length === 0) {
    console.log('No components in registry.');
    return;
  }

  const installed = new Set();
  for (const name of names) {
    const dest = resolve(COMPONENTS_DIR, registry[name].file);
    if (existsSync(dest)) installed.add(name);
  }

  console.log('\nAvailable components:\n');
  for (const name of names) {
    const status = installed.has(name) ? ' (installed)' : '';
    const radix = registry[name].radixPackages?.length
      ? ` [${registry[name].radixPackages.join(', ')}]`
      : '';
    console.log(`  ${name}${status}${radix}`);
  }
  console.log('');
}

function resolveAllDependencies(registry, names) {
  const resolved = new Set();
  const queue = [...names];

  while (queue.length > 0) {
    const name = queue.shift();
    if (resolved.has(name)) continue;
    if (!registry[name]) {
      console.error(`Unknown component: "${name}"`);
      console.error(`Run with --list to see available components.`);
      process.exit(1);
    }
    resolved.add(name);
    for (const dep of registry[name].dependencies || []) {
      if (!resolved.has(dep)) queue.push(dep);
    }
  }

  return resolved;
}

function installRadixPackages(registry, names) {
  const packages = new Set();
  for (const name of names) {
    for (const pkg of registry[name].radixPackages || []) {
      packages.add(pkg);
    }
  }

  if (packages.size === 0) return;

  // Check which are already installed
  const pkgJsonPath = resolve(UI_DIR, 'package.json');
  const pkgJson = JSON.parse(readFileSync(pkgJsonPath, 'utf-8'));
  const allDeps = { ...pkgJson.dependencies, ...pkgJson.peerDependencies, ...pkgJson.devDependencies };

  const toInstall = [...packages].filter((p) => !allDeps[p]);
  if (toInstall.length === 0) return;

  console.log(`Installing: ${toInstall.join(', ')}`);
  execFileSync('npm', ['install', ...toInstall, '-w', '@simplemodule/ui'], {
    cwd: ROOT,
    stdio: 'inherit',
  });
}

function copyComponents(registry, names) {
  mkdirSync(COMPONENTS_DIR, { recursive: true });

  const copied = [];
  for (const name of names) {
    const src = resolve(TEMPLATES_DIR, registry[name].file);
    const dest = resolve(COMPONENTS_DIR, registry[name].file);

    if (existsSync(dest)) {
      console.log(`  ${name} — already exists, skipping`);
      continue;
    }

    if (!existsSync(src)) {
      console.error(`  ${name} — template not found at ${src}`);
      process.exit(1);
    }

    copyFileSync(src, dest);
    copied.push(name);
    console.log(`  ${name} — added`);
  }

  return copied;
}

function updateBarrelExport(registry, names) {
  // Collect all installed components (check files on disk)
  const allInstalled = [];
  for (const [name, meta] of Object.entries(registry)) {
    const dest = resolve(COMPONENTS_DIR, meta.file);
    if (existsSync(dest)) {
      allInstalled.push({ name, meta });
    }
  }

  // Sort alphabetically
  allInstalled.sort((a, b) => a.name.localeCompare(b.name));

  // Generate index
  const lines = allInstalled.map(({ meta }) => {
    const exports = meta.exports.join(', ');
    const file = meta.file.replace('.tsx', '');
    return `export { ${exports} } from './${file}';`;
  });

  writeFileSync(INDEX_PATH, lines.join('\n') + '\n');
}

// --- Main ---

const args = process.argv.slice(2);

if (args.length === 0 || args.includes('--help') || args.includes('-h')) {
  console.log(`
Usage: npm run ui:add -- <component...>
       npm run ui:add -- --list

Examples:
  npm run ui:add -- button
  npm run ui:add -- dialog badge card
  npm run ui:add -- --list
`);
  process.exit(0);
}

const registry = loadRegistry();

if (args.includes('--list')) {
  listComponents(registry);
  process.exit(0);
}

const requested = args.filter((a) => !a.startsWith('-'));
const allNames = resolveAllDependencies(registry, requested);

console.log(`\nAdding ${allNames.size} component(s):\n`);

installRadixPackages(registry, allNames);
const copied = copyComponents(registry, allNames);
updateBarrelExport(registry, allNames);

if (copied.length > 0) {
  console.log(`\nDone! ${copied.length} component(s) added to src/SimpleModule.UI/components/`);
} else {
  console.log('\nAll requested components already installed.');
}
```

**Step 2: Add npm script to root package.json**

Add to `scripts` in `package.json`:

```json
"ui:add": "node tools/add-component.mjs"
```

**Step 3: Verify the CLI runs**

Run: `npm run ui:add -- --list`
Expected: Lists all 18 components from the registry

**Step 4: Test adding a component**

Run: `npm run ui:add -- button`
Expected:
- Prints "Adding 1 component(s)"
- `@radix-ui/react-slot` is already installed (from Task 1)
- Copies `button.tsx` to `components/`
- Updates `components/index.ts` with Button export

**Step 5: Verify the barrel export**

Run: `cat src/SimpleModule.UI/components/index.ts`
Expected: `export { Button, buttonVariants } from './button';`

**Step 6: Commit**

```bash
git add tools/add-component.mjs package.json src/SimpleModule.UI/components/
git commit -m "feat: add CLI tool for adding UI components (npm run ui:add)"
```

---

### Task 9: Add all 18 components and verify build

**Step 1: Add all components via CLI**

Run: `npm run ui:add -- button input textarea label select checkbox radio-group switch dialog dropdown-menu popover tabs table card badge alert separator spinner`

Expected: All 18 components added, Radix packages installed

**Step 2: Verify npm install is clean**

Run: `npm install`
Expected: No errors, lockfile updated

**Step 3: Verify TypeScript compiles**

Run: `npx tsc --noEmit`
Expected: No errors

**Step 4: Verify biome lint passes**

Run: `npm run check`
Expected: No errors (or only pre-existing ones)

**Step 5: Fix any lint issues**

Run: `npm run check:fix` if needed

**Step 6: Commit**

```bash
git add src/SimpleModule.UI/ package-lock.json
git commit -m "feat: add all 18 component templates via CLI"
```

---

### Task 10: Verify module consumption — update Products/Manage.tsx

This task proves the library works end-to-end in a real module.

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Manage.tsx`

**Step 1: Update Manage.tsx to use `@simplemodule/ui` components**

Replace the full file content with:

```tsx
import { router } from '@inertiajs/react';
import { Button, Table, TableHeader, TableBody, TableRow, TableHead, TableCell, Badge } from '@simplemodule/ui';

interface Product {
  id: number;
  name: string;
  price: number;
}

interface Props {
  products: Product[];
}

export default function Manage({ products }: Props) {
  function handleDelete(id: number, name: string) {
    if (!confirm(`Delete product "${name}"?`)) return;
    router.delete(`/products/${id}`);
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1
            className="text-2xl font-extrabold tracking-tight"
            style={{ fontFamily: "'Sora', sans-serif" }}
          >
            <span className="gradient-text">Manage Products</span>
          </h1>
          <p className="text-text-muted text-sm mt-1">{products.length} total products</p>
        </div>
        <Button onClick={() => router.get('/products/create')}>Create Product</Button>
      </div>

      <div className="glass-card overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Price</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {products.map((product) => (
              <TableRow key={product.id}>
                <TableCell className="text-text-muted">#{product.id}</TableCell>
                <TableCell className="font-medium text-text">{product.name}</TableCell>
                <TableCell>${product.price.toFixed(2)}</TableCell>
                <TableCell>
                  <div className="flex gap-3">
                    <Button variant="ghost" size="sm" onClick={() => router.get(`/products/${product.id}/edit`)}>
                      Edit
                    </Button>
                    <Button variant="danger" size="sm" onClick={() => handleDelete(product.id, product.name)}>
                      Delete
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {products.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="py-8 text-center text-text-muted">
                  No products yet. Create your first product!
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
```

**Step 2: Verify Vite build succeeds for Products module**

Run: `npm run build -w @simplemodule/products`
Expected: Build succeeds, outputs `Products.pages.js`

**Step 3: Verify TypeScript compiles**

Run: `npx tsc --noEmit`
Expected: No errors

**Step 4: Commit**

```bash
git add src/modules/Products/src/Products/Views/Manage.tsx
git commit -m "feat: migrate Products/Manage to @simplemodule/ui components"
```

---

### Task 11: Update Products/Create.tsx to use UI components

**Files:**
- Modify: `src/modules/Products/src/Products/Views/Create.tsx`

**Step 1: Update Create.tsx**

Replace with:

```tsx
import { router } from '@inertiajs/react';
import { Button, Input, Label, Card, CardContent } from '@simplemodule/ui';

export default function Create() {
  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    router.post('/products', formData);
  }

  return (
    <div className="max-w-xl">
      <div className="flex items-center gap-3 mb-1">
        <a
          href="/products/manage"
          className="text-text-muted hover:text-text transition-colors no-underline"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            viewBox="0 0 24 24"
          >
            <path d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <h1
          className="text-2xl font-extrabold tracking-tight"
          style={{ fontFamily: "'Sora', sans-serif" }}
        >
          <span className="gradient-text">Create Product</span>
        </h1>
      </div>
      <p className="text-text-muted text-sm ml-7 mb-6">Add a new product</p>

      <Card className="glass-card">
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input id="name" name="name" required placeholder="Product name" />
            </div>
            <div>
              <Label htmlFor="price">Price</Label>
              <Input id="price" name="price" type="number" required min={0.01} step={0.01} placeholder="0.00" />
            </div>
            <Button type="submit">Create</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

**Step 2: Verify build**

Run: `npm run build -w @simplemodule/products`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/modules/Products/src/Products/Views/Create.tsx
git commit -m "feat: migrate Products/Create to @simplemodule/ui components"
```
