import { 
  Component, 
  viewChild, 
  ElementRef, 
  ChangeDetectionStrategy, 
  inject, 
  DestroyRef,
  effect 
} from '@angular/core';

interface Particle {
  x: number;
  y: number;
  size: number;
  speedX: number;
  speedY: number;
  hue: number;
}

/**
 * Fractal Animation Component
 * 
 * Displays an animated particle network effect for the auth page.
 * Features:
 * - Responsive particle count based on screen size
 * - High-DPI display support
 * - Optimized performance with throttled resize events
 * - Full height, 30% width container
 */
@Component({
  selector: 'app-fractal-animation',
  standalone: true,
  imports: [],
  templateUrl: './fractal-animation.component.html',
  styleUrl: './fractal-animation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FractalAnimationComponent {
  // 1. Injected dependencies
  private readonly destroyRef = inject(DestroyRef);

  // 2. View queries
  readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('fractalCanvas');

  // 3. Private state
  private animationId: number | null = null;
  private particles: Particle[] = [];
  private resizeHandler?: () => void;

  // 4. Constructor with effects
  constructor() {
    // Initialize animation when canvas is available
    effect(() => {
      const canvas = this.canvasRef();
      if (canvas) {
        // Use requestAnimationFrame to ensure the DOM is fully rendered
        requestAnimationFrame(() => {
          this.initFractalAnimation();
        });
      }
    });

    // Cleanup on destroy
    this.destroyRef.onDestroy(() => {
      if (this.animationId) {
        cancelAnimationFrame(this.animationId);
      }
      if (this.resizeHandler) {
        window.removeEventListener('resize', this.resizeHandler);
      }
    });
  }

  /**
   * Initialize the fractal animation
   */
  private initFractalAnimation(): void {
    // Use setTimeout to ensure the canvas element has dimensions
    setTimeout(() => {
      const canvas = this.canvasRef().nativeElement;
      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      // Disable shadows globally for cleaner particle rendering
      ctx.shadowBlur = 0;
      ctx.shadowColor = 'transparent';
      ctx.shadowOffsetX = 0;
      ctx.shadowOffsetY = 0;

      // Use device pixel ratio for crisp rendering on high-DPI displays
      const dpr = window.devicePixelRatio || 1;

      const resizeCanvas = () => {
        const rect = canvas.getBoundingClientRect();
        const displayWidth = rect.width;
        const displayHeight = rect.height;

        if (displayWidth === 0 || displayHeight === 0) {
          return; // Skip if canvas has no dimensions
        }

        // Set actual size in memory (scaled by device pixel ratio)
        canvas.width = displayWidth * dpr;
        canvas.height = displayHeight * dpr;

        // Reset transform matrix before scaling
        ctx.setTransform(1, 0, 0, 1, 0, 0);
        
        // Scale the drawing context to match device pixel ratio
        ctx.scale(dpr, dpr);

        // Set display size (CSS pixels)
        canvas.style.width = displayWidth + 'px';
        canvas.style.height = displayHeight + 'px';

        // Re-initialize particles with display dimensions (not scaled)
        this.initParticles(displayWidth, displayHeight);
      };

      resizeCanvas();
      
      // Throttle resize events for better performance
      let resizeTimeout: number | null = null;
      this.resizeHandler = () => {
        if (resizeTimeout) {
          cancelAnimationFrame(resizeTimeout);
        }
        resizeTimeout = requestAnimationFrame(() => {
          resizeCanvas();
        });
      };
      
      window.addEventListener('resize', this.resizeHandler);

      // Start animation loop
      this.animate(ctx, canvas);
    }, 0);
  }

  private initParticles(width: number, height: number): void {
    this.particles = [];
    // Responsive particle count based on screen size
    // For large screens (>1920px), use more particles but cap at reasonable limit
    const screenArea = width * height;
    const baseParticleCount = 100; // Increased base count
    const maxParticleCount = 250; // Increased max for very large screens
    const particleCount = Math.min(
      Math.max(baseParticleCount, Math.floor(screenArea / 10000)), // More particles per area
      maxParticleCount
    );
    
    for (let i = 0; i < particleCount; i++) {
      this.particles.push({
        x: Math.random() * width,
        y: Math.random() * height,
        size: Math.random() * 3 + 1.5, // Visible particle size
        speedX: (Math.random() - 0.5) * 0.5,
        speedY: (Math.random() - 0.5) * 0.5,
        hue: Math.random() * 60 + 240 // Purple to blue range
      });
    }
  }

  private animate(ctx: CanvasRenderingContext2D, canvas: HTMLCanvasElement): void {
    // Get display dimensions (CSS pixels, not scaled)
    const rect = canvas.getBoundingClientRect();
    const displayWidth = rect.width || canvas.width / (window.devicePixelRatio || 1);
    const displayHeight = rect.height || canvas.height / (window.devicePixelRatio || 1);

    // Skip if canvas has no dimensions
    if (displayWidth === 0 || displayHeight === 0 || this.particles.length === 0) {
      this.animationId = requestAnimationFrame(() => this.animate(ctx, canvas));
      return;
    }

    // Clear canvas completely (no trail effect)
    ctx.clearRect(0, 0, displayWidth, displayHeight);
    
    // Fill with dark background
    ctx.fillStyle = 'rgba(17, 24, 39, 1)';
    ctx.fillRect(0, 0, displayWidth, displayHeight);

    // Update and draw particles
    this.particles.forEach((particle, i) => {
      particle.x += particle.speedX;
      particle.y += particle.speedY;

      // Bounce off edges (using display dimensions)
      if (particle.x < 0 || particle.x > displayWidth) particle.speedX *= -1;
      if (particle.y < 0 || particle.y > displayHeight) particle.speedY *= -1;

      // Draw particle with localized shadow (only near the particle)
      ctx.shadowBlur = 8; // Small shadow blur for glow effect
      ctx.shadowColor = `hsla(${particle.hue}, 70%, 60%, 0.5)`; // Shadow matches particle color
      ctx.shadowOffsetX = 0;
      ctx.shadowOffsetY = 0;
      ctx.beginPath();
      ctx.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
      ctx.fillStyle = `hsla(${particle.hue}, 70%, 60%, 0.8)`;
      ctx.fill();
      
      // Reset shadow after drawing particle
      ctx.shadowBlur = 0;
      ctx.shadowColor = 'transparent';

      // Draw connections (optimized: only check nearby particles)
      // For large screens, limit connection distance checks to improve performance
      const maxConnectionDistance = Math.min(120, Math.max(displayWidth, displayHeight) * 0.1);
      
      this.particles.slice(i + 1).forEach(otherParticle => {
        const dx = particle.x - otherParticle.x;
        const dy = particle.y - otherParticle.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        if (distance < maxConnectionDistance) {
          // Disable shadows for connection lines
          ctx.shadowBlur = 0;
          ctx.shadowColor = 'transparent';
          ctx.beginPath();
          ctx.moveTo(particle.x, particle.y);
          ctx.lineTo(otherParticle.x, otherParticle.y);
          ctx.strokeStyle = `hsla(${particle.hue}, 70%, 60%, ${1 - distance / maxConnectionDistance})`;
          ctx.lineWidth = 0.5;
          ctx.stroke();
        }
      });
    });

    this.animationId = requestAnimationFrame(() => this.animate(ctx, canvas));
  }
}

