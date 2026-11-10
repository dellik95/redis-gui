import tsParser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import ngPlugin from '@angular-eslint/eslint-plugin';
import ngTemplatePlugin from '@angular-eslint/eslint-plugin-template';
import ngTemplateParser from '@angular-eslint/template-parser';

export default [
  {
    ignores: ['dist/**', 'node_modules/**', '.angular/**', '**/*.js', '**/*.mjs']
  },
  {
    files: ['src/**/*.ts'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 2022,
        sourceType: 'module'
      }
    },
    plugins: {
      '@typescript-eslint': tsPlugin,
      '@angular-eslint': ngPlugin
    },
    rules: {}
  },
  {
    files: ['src/**/*.html'],
    languageOptions: {
      parser: ngTemplateParser
    },
    plugins: {
      '@angular-eslint/template': ngTemplatePlugin
    },
    rules: {}
  }
];
