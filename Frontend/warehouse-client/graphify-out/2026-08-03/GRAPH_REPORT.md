# Graph Report - warehouse-client  (2026-08-03)

## Corpus Check
- 18 files · ~7,720 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 130 nodes · 149 edges · 11 communities (10 shown, 1 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f2135609`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- compilerOptions
- compilerOptions
- PickTasks.tsx
- devDependencies
- package.json
- dependencies
- plugins
- React + TypeScript + Vite
- tsconfig.json
- InventoryAdmin.tsx

## God Nodes (most connected - your core abstractions)
1. `compilerOptions` - 17 edges
2. `compilerOptions` - 15 edges
3. `react` - 7 edges
4. `PickTask` - 6 edges
5. `scripts` - 5 edges
6. `axiosClient` - 5 edges
7. `plugins` - 4 edges
8. `rules` - 3 edges
9. `InventoryAdmin()` - 3 edges
10. `lib` - 3 edges

## Surprising Connections (you probably didn't know these)
- `Props` --references--> `PickTask`  [EXTRACTED]
  src/pages/PickTasks/ActiveTaskScreen.tsx → src/types/task.ts
- `Props` --references--> `PickTask`  [EXTRACTED]
  src/pages/PickTasks/NewTaskScreen.tsx → src/types/task.ts

## Import Cycles
- None detected.

## Communities (11 total, 1 thin omitted)

### Community 0 - "compilerOptions"
Cohesion: 0.09
Nodes (22): DOM, src, vite/client, compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib (+14 more)

### Community 1 - "compilerOptions"
Cohesion: 0.10
Nodes (19): node, vite.config.ts, compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection (+11 more)

### Community 2 - "PickTasks.tsx"
Cohesion: 0.16
Nodes (9): axiosClient, logout(), Props, Props, Props, Screen, Props, PickTask (+1 more)

### Community 3 - "devDependencies"
Cohesion: 0.13
Nodes (15): oxlint, devDependencies, oxlint, @types/node, @types/react, @types/react-dom, typescript, vite (+7 more)

### Community 4 - "package.json"
Cohesion: 0.20
Nodes (9): name, private, scripts, build, dev, lint, preview, type (+1 more)

### Community 5 - "dependencies"
Cohesion: 0.22
Nodes (9): axios, dependencies, axios, react, react-dom, react-router-dom, react, react-dom (+1 more)

### Community 6 - "plugins"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 7 - "React + TypeScript + Vite"
Cohesion: 0.50
Nodes (3): Expanding the Oxlint configuration, React Compiler, React + TypeScript + Vite

### Community 10 - "InventoryAdmin.tsx"
Cohesion: 0.19
Nodes (10): react, App(), extractErrorMessage(), getAvailableQuantity(), inputStyle, InventoryAdmin(), labelStyle, Product (+2 more)

## Knowledge Gaps
- **69 isolated node(s):** `$schema`, `typescript`, `oxc`, `react/rules-of-hooks`, `warn` (+64 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `react` connect `InventoryAdmin.tsx` to `PickTasks.tsx`, `plugins`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._
- **Why does `devDependencies` connect `devDependencies` to `package.json`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._
- **Why does `plugins` connect `plugins` to `InventoryAdmin.tsx`?**
  _High betweenness centrality (0.036) - this node is a cross-community bridge._
- **What connects `$schema`, `typescript`, `oxc` to the rest of the system?**
  _69 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `compilerOptions` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._
- **Should `compilerOptions` be split into smaller, more focused modules?**
  _Cohesion score 0.1 - nodes in this community are weakly interconnected._
- **Should `devDependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.13333333333333333 - nodes in this community are weakly interconnected._