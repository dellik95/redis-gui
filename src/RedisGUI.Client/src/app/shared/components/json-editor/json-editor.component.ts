import {
  Component,
  ElementRef,
  OnInit,
  OnDestroy,
  input,
  output,
  viewChild,
  effect,
  ChangeDetectionStrategy,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import loader from '@monaco-editor/loader';
import type { editor } from 'monaco-editor';

@Component({
  selector: 'app-json-editor',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="json-editor-container">
      <div #editorContainer class="editor-container"></div>
      @if (errorMessage()) {
        <div class="error-message">{{ errorMessage() }}</div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      height: 100%;
    }

    .json-editor-container {
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .editor-container {
      flex: 1;
      min-height: 300px;
      border: 1px solid var(--mat-divider-color, rgba(0, 0, 0, 0.12));
      border-radius: 4px;
      overflow: hidden;
    }

    .error-message {
      padding: 8px 12px;
      margin-top: 8px;
      background-color: var(--mat-error-container-background-color, #ffebee);
      color: var(--mat-error-color, #c62828);
      border-radius: 4px;
      font-size: 12px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JsonEditorComponent implements OnInit, OnDestroy {
  // Inputs
  value = input<string>('');
  readOnly = input<boolean>(false);
  language = input<string>('json');
  theme = input<'vs' | 'vs-dark' | 'hc-black'>('vs');
  height = input<string>('400px');

  // Outputs
  valueChange = output<string>();
  valid = output<boolean>();

  // ViewChild
  editorContainer = viewChild.required<ElementRef>('editorContainer');

  // State
  errorMessage = signal<string>('');
  private editor: editor.IStandaloneCodeEditor | null = null;
  private monaco: typeof import('monaco-editor') | null = null;

  constructor() {
    // Set Monaco paths
    loader.config({
      paths: {
        vs: 'assets/monaco-editor/min/vs'
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
  }

  async ngOnInit(): Promise<void> {
    try {
      this.monaco = await loader.init();
      this.initializeEditor();
    } catch (error) {
      console.error('Error initializing Monaco Editor:', error);
      this.errorMessage.set('Failed to initialize editor');
    }
  }

  ngOnDestroy(): void {
    this.editor?.dispose();
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
    } catch (error: any) {
      const message = error.message || 'Invalid JSON';
      this.errorMessage.set(message);
      this.valid.emit(false);
    }
  }

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
    } catch (error: any) {
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
