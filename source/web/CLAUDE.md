# Project: Expense Tracker
Expense Tracker - Family Expense Intelligence Platform
This is web folder for front-end

## React Architecture

- `src/components/` — reusable UI components (Button, Modal, Table, etc.)
- `src/features/` — feature modules (auth, dashboard, settings), each with its own components, hooks, and API calls
- `src/hooks/` — shared custom hooks
- `src/api/` — API client and typed request/response definitions
- `src/types/` — shared TypeScript types and interfaces
- `src/utils/` — pure utility functions

## Component Conventions

- Functional components only — no class components
- Use named exports, not default exports
- Co-locate tests: `Button.tsx` → `Button.test.tsx` in the same directory
- Co-locate styles: `Button.tsx` → `Button.module.css` (CSS Modules)
- Props interface named `{Component}Props` — e.g., `ButtonProps`
- Destructure props in the function signature

```tsx
// Good
export function Button({ label, onClick, variant = 'primary' }: ButtonProps) {
  return <button className={styles[variant]} onClick={onClick}>{label}</button>;
}
```

## State Management

- Local state: useState/useReducer
- Server state: TanStack Query (React Query) — never store API data in local state
- Global app state: Zustand stores in `src/stores/`
- No Redux — do not introduce Redux or Redux Toolkit

## Testing

- Use Vitest + React Testing Library
- Test behavior, not implementation — query by role, text, or test ID
- Every component should have at least a smoke test (renders without crashing)
- Mock API calls with MSW (Mock Service Worker), not jest.mock
- Place test utilities in `src/test/helpers.ts`

## TypeScript

- Strict mode enabled — do not use `any` unless absolutely necessary with a comment explaining why
- Prefer `interface` over `type` for object shapes
- Use discriminated unions for state machines and complex state
- API response types live in `src/api/types.ts`

## Do NOT

- Do not use `any` without a justifying comment
- Do not add new dependencies without discussing first
- Do not use inline styles — use CSS Modules
- Do not use default exports