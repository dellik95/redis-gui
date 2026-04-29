import {
  Component,
  ElementRef,
  input,
  output,
  viewChild,
  effect,
  ChangeDetectionStrategy,
  signal,
  inject,
  DestroyRef
} from '@angular/core';

import loader from '@monaco-editor/loader';
import type { editor } from 'monaco-editor';

/**
 * JSON Editor Component
 * Monaco Editor-based JSON editor with syntax highlighting, validation, and formatting
 * 
 * Features:
 * - Syntax highlighting for JSON
 * - Real-time validation
 * - Read-only mode support
 * - Customizable theme and height
 * - Emits value changes and validation status
 * 
 * @example
 * ```html
 * <app-json-editor
 *   [value]="jsonString"
 *   [readOnly]="false"
 *   [theme]="'vs-dark'"
 *   [height]="'500px'"
 *   (valueChange)="onValueChange($event)"
 *   (valid)="onValidationChange($event)"
 * />
 * ```
 */
@Component({
  selector: 'app-json-editor',
  standalone: true,
  imports: [],
  templateUrl: './json-editor.component.html',
  styleUrl: './json-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JsonEditorComponent {
  // 1. Injected dependencies
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  /** JSON string value to display/edit */
  readonly value = input<string>('');
  
  /** Whether the editor is in read-only mode */
  readonly readOnly = input<boolean>(false);
  
  /** Editor language (default: 'json') */
  readonly language = input<string>('json');
  
  /** Editor theme: 'vs' (light), 'vs-dark' (dark), or 'hc-black' (high contrast) */
  readonly theme = input<'vs' | 'vs-dark' | 'hc-black'>('vs');
  
  /** Editor height (CSS value, default: '400px') */
  readonly height = input<string>('400px');

  // 3. Outputs
  /** Emitted when the editor value changes */
  readonly valueChange = output<string>();
  
  /** Emitted when validation status changes (true if valid JSON) */
  readonly valid = output<boolean>();

  // 4. View queries
  /** Reference to the editor container element */
  readonly editorContainer = viewChild.required<ElementRef>('editorContainer');

  // 5. State signals
  /** Current error message (if JSON is invalid) */
  readonly errorMessage = signal<string>('');
  
  /** Whether Monaco Editor has been initialized */
  readonly isInitialized = signal(false);

  // 6. Private state
  private editor: editor.IStandaloneCodeEditor | null = null;
  private monaco: typeof import('monaco-editor') | null = null;

  // 7. Constructor with initialization
  constructor() {
    // Set Monaco paths
    loader.config({
      paths: {
        vs: 'assets/monaco-editor/min/vs'
      }
    });

    // Initialize Monaco editor when container is available
    effect(() => {
      const container = this.editorContainer();
      if (container && !this.isInitialized()) {
        this.initializeMonaco();
      }
    });

    // React to value changes
    effect(() => {
      const newValue = this.value();
      if (this.editor && this.editor.getValue() !== newValue) {
        this.editor.setValue(newValue);
        this.validateJson(newValue);
      }
    });

    // React to theme changes
    effect(() => {
      const newTheme = this.theme();
      if (this.monaco) {
        this.monaco.editor.setTheme(newTheme);
      }
    });

    // React to readOnly changes
    effect(() => {
      const readOnly = this.readOnly();
      if (this.editor) {
        this.editor.updateOptions({ readOnly });
      }
    });

    // Cleanup on destroy
    this.destroyRef.onDestroy(() => {
      this.editor?.dispose();
    });
  }

  /**
   * Initialize Monaco editor
   */
  private async initializeMonaco(): Promise<void> {
    try {
      this.monaco = await loader.init();
      this.initializeEditor();
      this.isInitialized.set(true);
    } catch (error) {
      console.error('Error initializing Monaco Editor:', error);
      this.errorMessage.set('Failed to initialize editor');
    }
  }

  private initializeEditor(): void {
    if (!this.monaco || !this.editorContainer()) return;

    const container = this.editorContainer().nativeElement;
    
    this.editor = this.monaco.editor.create(container, {
      value: this.value(),
      language: this.language(),
      theme: this.theme(),
      readOnly: this.readOnly(),
      automaticLayout: true,
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      fontSize: 13,
      lineNumbers: 'on',
      renderWhitespace: 'selection',
      tabSize: 2,
      formatOnPaste: true,
      formatOnType: true,
      suggest: {
        showKeywords: true,
        showSnippets: true
      },
      quickSuggestions: {
        other: true,
        comments: false,
        strings: true
      }
    });

    // Listen for content changes
    this.editor.onDidChangeModelContent(() => {
      const value = this.editor?.getValue() || '';
      this.validateJson(value);
      this.valueChange.emit(value);
    });

    // Initial validation
    this.validateJson(this.value());
  }

  /**
   * Validate JSON content
   */
  private validateJson(value: string): void {
    if (!value.trim()) {
      this.errorMessage.set('');
      this.valid.emit(true);
      return;
    }

    try {
      JSON.parse(value);
      this.errorMessage.set('');
      this.valid.emit(true);
    } catch (error: unknown) {
      const message = error instanceof Error 
        ? error.message 
        : 'Invalid JSON';
      this.errorMessage.set(message);
      this.valid.emit(false);
    }
  }

  /**
   * Format the current JSON content
   */
  /**
   * Format the current JSON content
   */
  formatJson(): void {
    if (!this.editor) return;

    try {
      const value = this.editor.getValue();
      const parsed = JSON.parse(value);
      const formatted = JSON.stringify(parsed, null, 2);
      this.editor.setValue(formatted);
      this.errorMessage.set('');
    } catch (error: unknown) {
      this.errorMessage.set('Cannot format invalid JSON');
    }
  }

  /**
   * Get the current value
   */
  getValue(): string {
    return this.editor?.getValue() || '';
  }

  /**
   * Set the editor value
   */
  setValue(value: string): void {
    this.editor?.setValue(value);
  }

  /**
   * Check if the current content is valid JSON
   */
  isValid(): boolean {
    try {
      JSON.parse(this.getValue());
      return true;
    } catch {
      return false;
    }
  }
}
