# Frontend lint classification — final hardening

Generated 2026-09-02 with `npm run lint`.

| Result | Count |
| --- | ---: |
| Errors | 0 |
| Warnings | 159 |
| `@typescript-eslint/no-unused-vars` | 143 |
| `@typescript-eslint/no-explicit-any` | 16 |

The warning set is non-blocking: unused icons/props occur in composition-heavy screen templates and `any` is limited to test/fixture boundaries. No blanket autofix was applied because it could remove intentionally reserved UI props or alter runtime behavior. The lint gate is therefore green (zero errors), with the 159 warnings retained as tracked quality debt for a dedicated cleanup pass.
