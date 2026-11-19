# Sheetwise Design System - Color & Style Guide

## ?? Official Color Palette

### Primary Colors
```css
--navy: #0f1419       /* Deep navy for text and primary elements */
--purple: #7c3aed     /* Vibrant purple for actions and accents */
--mint: #10b981       /* Fresh green for success states */
```

### Secondary Colors
```css
--muted: #6b7280      /* Gray for secondary text */
--line: #e5e7eb       /* Light gray for borders */
```

### Gradient Applications

#### **Button Gradients**
```css
/* Primary Button */
background: linear-gradient(135deg, var(--navy) 0%, #1e2a3f 100%);
box-shadow: 0 4px 12px rgba(15, 20, 25, 0.15);

/* Hover State */
transform: translateY(-2px);
box-shadow: 0 8px 20px rgba(15, 20, 25, 0.2);
```

#### **Metric Card Icons**
```css
/* Purple Gradient */
background: linear-gradient(135deg, #f3e8ff 0%, #ede9fe 100%);
color: #7c3aed;

/* Green Gradient */
background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
color: #059669;

/* Blue Gradient */
background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
color: #0369a1;
```

#### **Tag & Badge Backgrounds**
```css
/* Department Tag */
background: linear-gradient(135deg, #ede9fe 0%, #f3e8ff 100%);
color: #6d28d9;

/* File Badge */
background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
color: #065f46;
```

#### **Loading & Success States**
```css
/* Loading Panel */
background: linear-gradient(135deg, #f3e8ff 0%, #ede9fe 100%);
border: 1px solid #e9d5ff;
color: #6d28d9;

/* Success Result */
background: #f0fdf4;
border: 1px solid #bbf7d0;
color: #065f46;
```

### Shadow System
```css
/* Card Shadow */
--shadow: 0 10px 25px rgba(15, 20, 25, 0.08), 
          0 4px 12px rgba(15, 20, 25, 0.05);

/* Button Hover Shadow */
box-shadow: 0 8px 20px rgba(15, 20, 25, 0.2);

/* Topbar Shadow */
box-shadow: 0 2px 8px rgba(15, 20, 25, 0.03);
```

## ?? Typography System

### Font Stack
```css
Font: 'DM Sans' and 'Manrope'
Fallback: system-ui, -apple-system, sans-serif
```

### Heading Sizes
```css
h1: clamp(32px, 5vw, 42px)  /* Responsive title */
h2: 15px, weight 700        /* Section heading */
h3: 16px, weight 700        /* Card title */
p: 15px, weight 400         /* Body text */
```

### Font Weights
```css
400: Regular text
500: Medium emphasis
600: Strong emphasis / Labels
700: Headings / Bold text
800: Extra bold (brand marks)
```

### Line Heights
```css
Headings: 1.1
Body: 1.5-1.6
Compact: 1.4
```

## ?? Spacing System

### Padding & Margin Scale
```css
4px   /* Minimal space (icon padding)  */
8px   /* Small gap (list items)        */
12px  /* Medium (card content)         */
16px  /* Standard (section padding)    */
20px  /* Large (card padding)          */
24px  /* Extra large (layout spacing)  */
28px  /* Page margins                  */
32px  /* Large margins                 */
48px  /* Max width padding             */
```

### Gap System
```css
Grid gaps: 10px, 12px, 15px, 18px, 20px, 28px
Flex gaps: 6px, 7px, 8px, 10px, 12px, 14px, 16px
```

## ?? Border & Radius System

### Border Radius
```css
6px   /* Subtle (pills, inputs)      */
8px   /* Standard (buttons, cards)   */
10px  /* Medium (icons, panels)      */
12px  /* Large (metric cards)        */
14px  /* Extra large (panels)        */
20px  /* Label pills (ultra-rounded) */
50%   /* Circular (avatars, pulses)  */
```

### Border Widths
```css
1px    /* Standard borders              */
1.5px  /* Focus states, emphasis        */
2px    /* Drop zone dashed border       */
4px    /* Notice accent left border     */
```

## ?? Hover & Interactive States

### Button States
```css
Default:   box-shadow + solid color
Hover:     transform: translateY(-2px) + enhanced shadow
Active:    transform: translateY(0)
Disabled:  opacity: 0.5, cursor: not-allowed
Focus:     border-color: purple + ring shadow
```

### Input States
```css
Default:   1.5px solid #e5e7eb
Focus:     1.5px solid #7c3aed + #f5f3ff background
Error:     1.5px solid #ef4444 + #fef2f2 background
Success:   1.5px solid #22c55e + #f0fdf4 background
```

### Link States
```css
Default:   color: #7c3aed
Hover:     text-decoration: underline + color darkens
Active:    opacity: 0.7
Visited:   color: #5b21b6
```

## ?? Animation Library

### Transition Timings
```css
Fast:   0.2s (hover effects)
Normal: 0.3s (state changes)
Slow:   0.6s (spinners)
```

### Keyframes
```css
@keyframes spin {
  to { transform: rotate(360deg); }
}
```

### Transform Effects
```css
Hover Lift:     translateY(-2px)
Drag Scale:     scale(1.01)
Spinner:        border-top-color animation
Active Press:   translateY(0)
```

## ?? Responsive Breakpoints

```css
Max Width Apps:   1440px
Desktop:          > 900px
Tablet:           680px - 900px
Mobile:           < 680px
Small Mobile:     < 430px
```

### Spacing Adjustments by Breakpoint
```css
Desktop:      padding: 0 48px
Tablet:       padding: 0 32px
Mobile:       padding: 0 18px
Small:        padding: 0 16px
```

## ? Special Effects

### Glassmorphism (Topbar)
```css
background: rgba(255, 255, 255, 0.95);
backdrop-filter: blur(8px);
box-shadow: 0 2px 8px rgba(15, 20, 25, 0.03);
```

### Gradient Text (Potential)
```css
background: linear-gradient(135deg, #7c3aed 0%, #10b981 100%);
-webkit-background-clip: text;
-webkit-text-fill-color: transparent;
```

### Focus Ring (Accessibility)
```css
box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
```

## ?? Theme Variations

### Dark Mode (Future Implementation)
```css
--navy-dark:    #0f1419
--surface-dark: #1a2332
--bg-dark:      #0f1419
--text-dark:    #e5e7eb
```

### High Contrast (Accessibility)
```css
Borders: 2px instead of 1.5px
Shadows: Enhanced for better depth
Colors: More saturated for clarity
```

---

## ?? Usage Examples

### Button
```html
<button class="primary-button">Action</button>
<!-- Auto applies gradient, shadow, hover lift -->
```

### Metric Card
```html
<div class="metric-card">
  <span class="metric-icon green">??</span>
  <div>
    <span>Label</span>
    <strong>24</strong>
  </div>
</div>
<!-- Auto applies gradient background to icon -->
```

### Table Row
```html
<tr>
  <td>
    <div class="employee-cell">
      <span class="avatar">AB</span>
      <div><strong>Name</strong><span>Role</span></div>
    </div>
  </td>
</tr>
<!-- Avatar auto gets gradient background -->
```

---

## ?? Accessibility Notes

? **Color Contrast**: All text meets WCAG AA standards
? **Focus States**: Visible purple outline on all interactive elements
? **Hover Targets**: Min 44px height/width on touch targets
? **Motion**: No auto-playing animations that can cause seizures
? **Icons**: Always paired with text labels

