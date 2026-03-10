# TailwindCSS Integration Design

## Approach: Standalone CLI + MSBuild Integration

### Setup
- Download Tailwind standalone CLI into `tools/` directory (gitignored)
- Create `tailwind.config.js` scanning all `.razor` files
- Create `src/SimpleModule.Api/Styles/app.css` as Tailwind input file
- Output compiled CSS to `src/SimpleModule.Api/wwwroot/css/app.css`
- Add MSBuild target in API `.csproj` to run Tailwind CLI before build
- Add helper script to download the CLI

### Component Migration
- Replace all inline `<style>` blocks and Bootstrap classes with Tailwind utility classes
- Files: `App.razor`, `MainLayout.razor`, `Home.razor`, `OAuthCallback.razor`, Users module components

### Constraints
- No Node.js dependency
- AOT compatibility preserved
- No changes to module architecture
