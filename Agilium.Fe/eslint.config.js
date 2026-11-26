import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
  },
  {
    // Route files export `Route` next to their page component and the auth context exports its hook:
    // that is how TanStack Router / React context are used, fast refresh just falls back to a full reload.
    files: ['src/routes/**/*.tsx', 'src/contexts/**/*.tsx'],
    rules: { 'react-refresh/only-export-components': 'off' },
  },
])
