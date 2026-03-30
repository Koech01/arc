# Arc Frontend Architecture

This document describes the architecture, design patterns, and technical decisions of Arc Frontend.

## Table of Contents

- [Overview](#overview)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Component Architecture](#component-architecture)
- [State Management](#state-management)
- [Authentication](#authentication)
- [API Integration](#api-integration)
- [Routing](#routing)
- [Styling](#styling)
- [Accessibility](#accessibility)
- [Build and Deployment](#build-and-deployment)

## Overview

Arc Frontend is a single-page application (SPA) built with React 19 and TypeScript. It provides a comprehensive interface for managing AI agent workflows, with a focus on type safety, accessibility, and security.

### Design Principles

1. **Type Safety**: Strict TypeScript throughout the codebase
2. **Accessibility First**: WCAG 2.1 AA compliance on all interactive components
3. **Component Composition**: Reusable, composable components built on shadcn/ui
4. **Security**: HTTP-only cookies, no client-side credential storage
5. **Developer Experience**: Consistent patterns and naming conventions

## Technology Stack

### Core

- **React 19.2.0**: Functional components with hooks
- **TypeScript 5.9.3**: Strict mode enabled
- **Vite (rolldown-vite 7.2.5)**: Build tool using the rolldown bundler via `npm:rolldown-vite` override

### UI Framework

- **shadcn/ui**: Component library built on Radix UI primitives
- **Radix UI**: Unstyled, accessible component primitives
- **Tailwind CSS 3.x**: Utility-first CSS framework
- **next-themes 0.4.6**: Light/dark theme management
- **Lucide React**: Icon library

### Data and Interaction

- **TanStack Table 8.21.3**: Advanced table functionality
- **Recharts 2.15.4**: Chart and data visualization
- **Zod 4.3.6**: Runtime schema validation
- **dnd-kit**: Drag-and-drop for workflow task ordering
- **sonner**: Toast notification system

### Routing

- **React Router DOM 7.13.0**: Client-side routing with `BrowserRouter`

### Utilities

- **class-variance-authority**: Component variant management
- **clsx / tailwind-merge**: Conditional class name composition

## Project Structure

```
arc-frontend/
├── src/
│   ├── components/
│   │   ├── Admin/               # Admin panel: dashboard, user management, audit log,
│   │   │                        #   system config, maintenance mode, cache, webhooks, LLM configs
│   │   ├── BatchExecution/      # Batch workflow execution with variable sets
│   │   ├── CacheManagement/     # User-level cache management
│   │   ├── Dashboard/           # Main dashboard with metrics and charts
│   │   ├── DataPersistence/     # Execution export and import
│   │   ├── Executions/          # Execution list, detail, replay, comparison
│   │   ├── ForgotPassword/      # Password reset request
│   │   ├── Home/                # Authenticated shell layout (sidebar, nav)
│   │   ├── LLMManagement/       # LLM provider configuration
│   │   ├── Login/               # Login form
│   │   ├── NotFound/            # 404 page
│   │   ├── Notifications/       # In-app notification center
│   │   ├── Onboarding/          # First-run onboarding flow
│   │   ├── PerformanceProfile/  # Per-execution performance profiling
│   │   ├── Profile/             # User profile management
│   │   ├── QuickExecute/        # Ad-hoc workflow execution
│   │   ├── RegressionGates/     # Regression gate management and testing
│   │   ├── ResetPassword/       # Password reset confirmation
│   │   ├── Settings/            # User preferences
│   │   ├── SignUp/              # Registration form
│   │   ├── Templates/           # Execution template CRUD and instantiation
│   │   ├── Webhooks/            # Webhook registration and management
│   │   ├── Workflows/           # Workflow builder, list, and detail
│   │   ├── types/               # Shared TypeScript type definitions
│   │   ├── ui/                  # shadcn/ui component overrides and additions
│   │   ├── ProtectedRoute.tsx   # Authentication guard
│   │   └── ThemeProvider.tsx    # next-themes provider wrapper
│   ├── hooks/
│   │   └── use-mobile.tsx       # Responsive breakpoint hook
│   ├── lib/
│   │   ├── api.ts               # All API clients (auth, executions, workflows, etc.)
│   │   ├── date.ts              # Date parsing utilities
│   │   ├── dateFormat.ts        # Date display formatting
│   │   ├── notification-utils.ts # Notification helper functions
│   │   ├── templateUtils.ts     # Template variable extraction utilities
│   │   └── utils.ts             # General utilities (cn, etc.)
│   ├── App.tsx                  # Root component with BrowserRouter and all routes
│   ├── main.tsx                 # Application entry point
│   ├── App.css                  # App-level styles
│   └── index.css                # Global styles and CSS variable definitions
├── public/
│   └── fonts/                   # Self-hosted Geist font
├── vercel.json                  # SPA rewrite rules and security headers
└── [configuration files]        # vite.config.ts, tsconfig.*, tailwind.config.ts, etc.
```

### Directory Organization

- Each feature has its own directory under `src/components/`
- Shared TypeScript types live in `src/components/types/`
- All API calls are centralized in `src/lib/api.ts`
- Feature directories use `index.tsx` as the primary export

## Component Architecture

### Component Pattern

All components follow this structure:

```typescript
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';

interface ComponentNameProps {
  // Props definition
}

export function ComponentName({ prop }: ComponentNameProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleAction = async () => {
    setError('');
    setLoading(true);
    try {
      await api.action(data);
      navigate('/success');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed');
    } finally {
      setLoading(false);
    }
  };

  return (/* JSX */);
}
```

### Skeleton Loaders

Loading states use skeleton components co-located with their feature:

```typescript
export function FeaturePage() {
  const [loading, setLoading] = useState(true);
  if (loading) return <FeaturePageSkeleton />;
  return (/* Content */);
}

export function FeaturePageSkeleton() {
  return (
    <div>
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-32 w-full" />
    </div>
  );
}
```

### Component Categories

- **Page Components**: Full-page feature views (e.g., `WorkflowListPage`, `ExecutionListPage`)
- **UI Components**: shadcn/ui primitives in `src/components/ui/`
- **Guard Components**: `ProtectedRoute` and `RequireAdmin` for route access control
- **Layout Components**: `Home/page.tsx` provides the authenticated shell with sidebar and navigation

## State Management

Arc Frontend uses only React built-in state:

- `useState` for local component state
- `useReducer` for complex state transitions
- `useContext` for theme state (via next-themes)
- No external state management libraries

State is lifted to the nearest common ancestor. Context is used only for theme management and is not used for application data.

## Authentication

### Security Model

- Authentication uses HTTP-only secure cookies set by the backend
- No credentials are stored in `localStorage` or `sessionStorage`
- All API requests include `credentials: 'include'`
- Protected routes verify authentication status on mount via `GET /api/auth/me`

### Route Guards

**ProtectedRoute** (`src/components/ProtectedRoute.tsx`):

```typescript
export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    auth.checkAuth()
      .then(() => setIsAuthenticated(true))
      .catch(() => setIsAuthenticated(false));
  }, []);

  if (isAuthenticated === null) return null; // Renders nothing while checking
  if (!isAuthenticated) return <Navigate to="/login/" replace />;
  return <>{children}</>;
}
```

**RequireAdmin** (`src/components/Admin/RequireAdmin.tsx`):

```typescript
export function RequireAdmin({ children }: { children: React.ReactNode }) {
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);

  useEffect(() => {
    auth.checkAuth()
      .then(user => setIsAdmin(user.role === 'Admin'))
      .catch(() => setIsAdmin(false));
  }, []);

  if (isAdmin === null) return <SkeletonLoader />;
  if (!isAdmin) return <Navigate to="/dashboard" replace />;
  return <>{children}</>;
}
```

### Authentication Flow

1. User submits credentials via `/login/`
2. `api.login()` posts to `POST /api/auth/login` with `credentials: 'include'`
3. Backend sets an HTTP-only cookie on success
4. `ProtectedRoute` calls `GET /api/auth/me` on every protected page load to verify the session
5. Logout calls `POST /api/auth/logout`, which clears the cookie server-side

## API Integration

### API Client Structure

All API calls are in `src/lib/api.ts`. The file exports a class instance for auth and plain objects for each domain:

```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';
```

**Exported API objects:**

| Export | Domain | Base Path |
|--------|--------|-----------|
| `api` | Authentication | `/api/auth/*` |
| `auth` | Auth helpers (logout, checkAuth, updateProfile) | `/api/auth/*` |
| `executionApi` | Execution CRUD, archive, replay | `/api/executions/*` |
| `workflowApi` | Workflow CRUD, execute, duplicate | `/api/workflows/*` |
| `webhookApi` | Webhook CRUD, toggle, test | `/api/webhooks/*` |
| `exportImportApi` | Bulk export and import | `/api/executions/export-bulk`, `/api/executions/import` |
| `cacheApi` | User cache clear | `/api/cache` |
| `batchApi` | Batch execution | `/api/batch` |
| `performanceApi` | Execution performance profile | `/api/executions/:id/profile` |
| `templateApi` | Execution template CRUD, instantiate | `/api/execution-templates/*` |
| `notificationApi` | Notification CRUD, mark read | `/api/notifications/*` |
| `settingsApi` | User preferences | `/api/settings/preferences` |
| `llmApi` | LLM config CRUD, test | `/api/llm-configs/*` |
| `adminApi` | Admin stats, users, executions, cache, maintenance, system config, audit log | `/api/admin/*` |
| `regressionGatesApi` | Regression gate CRUD, toggle, test | `/api/regression-gates/*` |
| `goldenExecutionsApi` | Golden execution mark/unmark | `/api/executions/:id/mark-golden` |

### Error Handling Pattern

All API functions follow this pattern:

```typescript
const response = await fetch(`${API_BASE_URL}/endpoint`, {
  credentials: 'include',
  // ...
});
if (!response.ok) {
  const error = await response.json().catch(() => ({
    message: getErrorMessage(response.status, 'Default message'),
  }));
  throw new Error(error.message);
}
return response.json();
```

The `getErrorMessage` helper maps HTTP status codes (401, 403, 404, 500, 503) to user-facing messages.

## Routing

### Route Structure

All routes are defined in `src/App.tsx` using `BrowserRouter`:

```
/login/                          → Login (public)
/signup/                         → SignUp (public)
/forgot/                         → ForgotPassword (public)
/reset/:token/                   → ResetPassword (public)
/onboarding                      → OnboardingPage (public)
/ (ProtectedRoute → Home layout)
  /dashboard                     → Dashboard
  /executions                    → ExecutionListPage
  /executions/compare            → CompareExecutions
  /executions/:id                → Executions (detail)
  /executions/:id/replay         → ReplayExecution
  /executions/:id/profile        → PerformanceProfilePage
  /workflows                     → WorkflowListPage
  /workflows/create              → CreateWorkflow
  /workflows/:id                 → WorkflowDetailPage
  /webhooks                      → WebhooksListPage
  /data-persistence              → DataPersistencePage
  /cache                         → CacheManagementPage
  /batch                         → BatchExecutionPage
  /profile                       → ProfilePage
  /execute                       → QuickExecutePage
  /templates                     → TemplateListPage
  /templates/new                 → CreateTemplatePage
  /templates/:name/edit          → EditTemplatePage
  /templates/:name/use           → InstantiateTemplatePage
  /notifications                 → NotificationsPage
  /settings                      → SettingsPage
  /llms                          → LLMManagementPage
  /regression-gates              → RegressionGatesPage
  /admin (RequireAdmin)          → AdminDashboard
  /admin/users                   → UserManagement
  /admin/executions              → SystemExecutions
  /admin/llm-configs             → LLMConfigsAdmin
  /admin/webhooks                → WebhooksAdmin
  /admin/cache                   → CacheManagement (admin)
  /admin/maintenance             → MaintenanceMode
  /admin/system                  → SystemConfiguration
  /admin/audit-log               → AdminAuditLog
  *                              → NotFoundPage
```

The root `/` redirects to `/dashboard`. A catch-all `*` route is registered both inside and outside the protected layout to handle 404s for authenticated and unauthenticated users.

## Styling

### Tailwind CSS

Utility-first CSS. All component styling uses Tailwind classes directly in JSX.

### CSS Variables

Theme tokens are defined as CSS custom properties in `src/index.css`:

```css
:root {
  --background: 0 0% 100%;
  --foreground: 222.2 84% 4.9%;
  --primary: 222.2 47.4% 11.2%;
  /* ... */
}
.dark {
  --background: 222.2 84% 4.9%;
  --foreground: 210 40% 98%;
  /* ... */
}
```

### Component Variants

shadcn/ui components use `class-variance-authority` for variant management. Custom variants follow the same pattern.

### Font

Geist Regular is self-hosted in `public/fonts/` and referenced in `index.css`.

## Accessibility

All components meet WCAG 2.1 AA standards:

- Semantic HTML with correct heading hierarchy and landmark regions
- ARIA labels on all interactive elements
- `aria-required`, `aria-invalid`, and `aria-describedby` on form fields
- `role="alert"` and `aria-live="polite"` on error messages
- `aria-busy` on loading buttons
- Keyboard navigation for all interactive elements
- Visible focus indicators
- Color contrast minimum 4.5:1 for normal text

```tsx
<Input
  id="field"
  aria-label="Field label"
  aria-required="true"
  aria-invalid={!!error}
  aria-describedby={error ? "field-error" : undefined}
/>
{error && (
  <div id="field-error" role="alert" aria-live="polite">
    {error}
  </div>
)}
<Button type="submit" disabled={loading} aria-busy={loading}>
  {loading ? 'Loading...' : 'Submit'}
</Button>
```

## Build and Deployment

### Build Command

```bash
npm run build
# Executes: tsc -b && vite build
```

Output is written to `dist/`.

### Environment Variable

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_BASE_URL` | Backend API base URL including `/api` path | `http://localhost:5000/api` |

### Deployment

Production deployments target Vercel. The deployment branch is `deployment-config`, not `main`. The `package.json` deploy script enforces this:

```bash
npm run deploy  # Must be on deployment-config branch
```

The `vercel.json` file configures:
- SPA rewrite: all routes serve `index.html`
- Security headers: `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`
- Git deployment: only `deployment-config` branch triggers production builds

See `DEPLOYMENT.md` for the full deployment procedure.
