# Audit Logs Dashboard Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a rich dashboard page for the Audit Logs module with KPI cards, charts (bar, pie, area, radial), and activity breakdowns powered by recharts via shadcn chart components.

**Architecture:** Expand the C# backend with a new `DashboardStats` DTO containing timeline series, source/action/module/status breakdowns, top users, top paths, hourly distribution, and summary KPIs. Serve it via a new `DashboardEndpoint` IViewEndpoint. On the frontend, add recharts + shadcn chart component to the UI library, then build a `Dashboard.tsx` page with responsive grid layout using existing Card components.

**Tech Stack:** C# / ASP.NET Minimal API, React 19, Recharts, shadcn/ui chart component, Tailwind CSS, Vite library mode.

---

## Task 1: Install recharts in the UI library

**Files:**
- Modify: `packages/SimpleModule.UI/package.json`

**Step 1: Install recharts**

Run: `npm install recharts -w @simplemodule/ui`

**Step 2: Verify installation**

Run: `npm ls recharts`
Expected: `recharts@2.x.x` listed under `@simplemodule/ui`

---

## Task 2: Create chart component in UI library

**Files:**
- Create: `packages/SimpleModule.UI/components/chart.tsx`
- Modify: `packages/SimpleModule.UI/components/index.ts`
- Modify: `packages/SimpleModule.UI/registry/registry.json`

**Step 1: Create chart.tsx**

Create `packages/SimpleModule.UI/components/chart.tsx` — a shadcn-style chart wrapper providing:
- `ChartContainer` — wraps recharts with responsive container, CSS variable theming, and tooltip context
- `ChartTooltip` + `ChartTooltipContent` — styled tooltip using Radix-compatible design
- `ChartLegend` + `ChartLegendContent` — styled legend
- `ChartConfig` type — maps data keys to label/color/icon config

The component should use CSS custom properties from the theme (--color-primary, --color-accent, etc.) and support both named theme colors and arbitrary hex/hsl values via `chartConfig`. It wraps recharts' `ResponsiveContainer` internally.

Pattern reference (shadcn chart):
```tsx
type ChartConfig = Record<string, {
  label?: React.ReactNode;
  icon?: React.ComponentType;
  color?: string;
}>;

// ChartContainer renders a ResponsiveContainer and injects CSS variables
// from chartConfig as --color-{key} on the wrapper div
```

**Step 2: Add exports to index.ts**

Add to `packages/SimpleModule.UI/components/index.ts`:
```typescript
export {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  ChartLegend,
  ChartLegendContent,
  type ChartConfig,
} from './chart';
```

**Step 3: Update registry.json**

Add chart entry:
```json
"chart": {
  "name": "Chart",
  "file": "chart.tsx",
  "dependencies": [],
  "radixPackages": [],
  "extraPackages": ["recharts"],
  "exports": ["ChartContainer", "ChartTooltip", "ChartTooltipContent", "ChartLegend", "ChartLegendContent"]
}
```

---

## Task 3: Create DashboardStats DTO in Contracts

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs.Contracts/DashboardStats.cs`

**Step 1: Create DashboardStats.cs**

```csharp
namespace SimpleModule.AuditLogs.Contracts;

[Dto]
public class DashboardStats
{
    // Summary KPIs
    public int TotalEntries { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageDurationMs { get; set; }
    public double ErrorRate { get; set; }

    // Breakdowns (for pie/bar charts)
    public Dictionary<string, int> BySource { get; set; } = new();
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByModule { get; set; } = new();
    public Dictionary<string, int> ByStatusCategory { get; set; } = new();
    public Dictionary<string, int> ByEntityType { get; set; } = new();

    // Top-N lists
    public List<NamedCount> TopUsers { get; set; } = [];
    public List<NamedCount> TopPaths { get; set; } = [];

    // Time series
    public List<TimelinePoint> Timeline { get; set; } = [];
    public List<NamedCount> HourlyDistribution { get; set; } = [];
}

[Dto]
public class NamedCount
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

[Dto]
public class TimelinePoint
{
    public string Date { get; set; } = "";
    public int Http { get; set; }
    public int Domain { get; set; }
    public int Changes { get; set; }
}
```

**Step 2: Build to verify**

Run: `dotnet build modules/AuditLogs/src/AuditLogs.Contracts/`

---

## Task 4: Add GetDashboardStatsAsync to contract and service

**Files:**
- Modify: `modules/AuditLogs/src/AuditLogs.Contracts/IAuditLogContracts.cs`
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogService.cs`

**Step 1: Add method to interface**

Add to `IAuditLogContracts`:
```csharp
Task<DashboardStats> GetDashboardStatsAsync(DateTimeOffset from, DateTimeOffset to);
```

**Step 2: Implement in AuditLogService**

Query the database within the date range and compute:
- `TotalEntries`: count all
- `UniqueUsers`: count distinct non-null UserIds
- `AverageDurationMs`: average of non-null DurationMs values (0 if none)
- `ErrorRate`: percentage of entries with StatusCode >= 400 out of entries with non-null StatusCode
- `BySource`: group by Source enum, use source name as key ("Http", "Domain", "ChangeTracker")
- `ByAction`: group by Action enum, use action name as key
- `ByModule`: group by Module (non-null)
- `ByStatusCategory`: group StatusCode into "2xx", "3xx", "4xx", "5xx" categories
- `ByEntityType`: group by EntityType (non-null), top 10
- `TopUsers`: top 10 users by entry count (use UserName ?? UserId)
- `TopPaths`: top 10 paths by entry count
- `Timeline`: group by date (Timestamp.Date), one point per day, split by source (Http/Domain/Changes counts)
- `HourlyDistribution`: group by hour of day (0-23), count per hour

Use multiple queries rather than materializing all entries. For SQLite compatibility, use Id-based ordering where Timestamp ordering is problematic.

**Step 3: Build**

Run: `dotnet build modules/AuditLogs/src/AuditLogs/`

---

## Task 5: Create Dashboard view endpoint

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Views/DashboardEndpoint.cs`

**Step 1: Create the endpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.AuditLogs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;

namespace SimpleModule.AuditLogs.Views;

public class DashboardEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/dashboard",
                async (DateTimeOffset? from, DateTimeOffset? to, IAuditLogContracts auditLogs) =>
                {
                    var now = DateTimeOffset.UtcNow;
                    var effectiveFrom = from ?? now.AddDays(-30);
                    var effectiveTo = to ?? now;
                    var stats = await auditLogs.GetDashboardStatsAsync(effectiveFrom, effectiveTo);
                    return Inertia.Render(
                        "AuditLogs/Dashboard",
                        new { stats, from = effectiveFrom, to = effectiveTo }
                    );
                }
            )
            .RequirePermission(AuditLogsPermissions.View);
}
```

**Step 2: Build**

Run: `dotnet build modules/AuditLogs/src/AuditLogs/`

---

## Task 6: Create Dashboard.tsx React page

**Files:**
- Create: `modules/AuditLogs/src/AuditLogs/Views/Dashboard.tsx`
- Modify: `modules/AuditLogs/src/AuditLogs/Pages/index.ts`

**Step 1: Create Dashboard.tsx**

Build a full dashboard page with the following layout:

**Row 1 — KPI Cards (4 columns):**
- Total Entries (with icon)
- Unique Users (with icon)
- Avg Response Time (formatted as ms)
- Error Rate (formatted as percentage, colored red if > 5%)

**Row 2 — Timeline Area Chart (full width):**
- Stacked area chart showing Http/Domain/Changes over time
- Uses ChartContainer + recharts AreaChart
- Tooltip showing date + breakdown

**Row 3 — Two charts side by side:**
- Left: Activity by Source (pie/donut chart — Http vs Domain vs ChangeTracker)
- Right: Actions Breakdown (horizontal bar chart — Created, Updated, Deleted, etc.)

**Row 4 — Two charts side by side:**
- Left: Status Code Distribution (pie chart — 2xx, 3xx, 4xx, 5xx with semantic colors)
- Right: Activity by Module (bar chart)

**Row 5 — Two charts side by side:**
- Left: Top 10 Users (horizontal bar chart)
- Right: Top 10 Paths (horizontal bar chart)

**Row 6 — Hourly Activity (full width):**
- Bar chart showing activity distribution by hour of day (0-23)

**Date range selector** at top (from/to inputs + Apply button) that navigates via Inertia router.

Use imports from `@simplemodule/ui` for Card, CardContent, CardHeader, CardTitle, PageHeader, Button, Input.
Use imports from `recharts` for AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid.
Use `ChartContainer`, `ChartTooltip`, `ChartTooltipContent` from `@simplemodule/ui` for chart wrappers.

Color palette: use CSS custom properties from theme (--color-primary, --color-accent, --color-success, --color-danger, --color-warning, --color-info).

**Step 2: Register in Pages/index.ts**

Add entry:
```typescript
'AuditLogs/Dashboard': () => import('../Views/Dashboard'),
```

---

## Task 7: Update sidebar menu to link to dashboard

**Files:**
- Modify: `modules/AuditLogs/src/AuditLogs/AuditLogsModule.cs`

**Step 1: Add dashboard menu item**

Add a new MenuItem for the dashboard at order 94 (before the existing Browse at 95):
```csharp
new MenuItem
{
    Label = "Dashboard",
    Url = "/audit-logs/dashboard",
    Icon = """<svg ...chart icon...</svg>""",
    Order = 94,
    Section = MenuSection.AdminSidebar,
}
```

Update the existing Browse item label to "Browse Logs" for clarity.

---

## Task 8: Build, lint, and verify

**Step 1:** Run `dotnet build` to verify C# compiles
**Step 2:** Run `npm run build` (or at minimum `npm run build -w @simplemodule/auditlogs`) to verify Vite build
**Step 3:** Run `npm run check` to verify lint/format
**Step 4:** Run `dotnet test` to verify existing tests still pass

---

## Task 9: Extract TypeScript types and validate pages

**Step 1:** Run `node tools/extract-ts-types.mjs` to regenerate types.ts with new DashboardStats/NamedCount/TimelinePoint interfaces
**Step 2:** Run `npm run validate-pages` to confirm the new Dashboard page is registered
