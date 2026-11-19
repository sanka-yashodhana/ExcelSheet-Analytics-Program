# ?? Sheetwise - Visual Design Guide

## UI Component Showcase

### **Buttons**

#### Primary Button
```
Background:  linear-gradient(135deg, #0f1419 0%, #1e2a3f 100%)
Color:       White
Height:      44px
Padding:     0 18px
Shadow:      0 4px 12px rgba(15,20,25,0.15)
Hover:       Transform: translateY(-2px)
             Shadow: 0 8px 20px rgba(15,20,25,0.2)
Border:      0
Radius:      8px
Font:        13px, weight 700
```

#### Secondary Button
```
Background:  #fff
Border:      1.5px solid #e5e7eb
Color:       #0f1419
Hover:       Border: #7c3aed
             Background: #f5f3ff
             Color: #7c3aed
Radius:      8px
Font:        13px, weight 700
```

#### Danger Button
```
Background:  #fff
Border:      1.5px solid #fecaca
Color:       #b91c1c
Hover:       Border: #ef4444
             Background: #fef2f2
             Color: #dc2626
```

---

### **Input Fields**

#### Text Input
```
Height:      40px
Padding:     0 12px 0 38px (with icon)
Border:      1.5px solid #e5e7eb
Border-Radius: 8px
Font:        12px
Focus:       Border-Color: #7c3aed
             Background: #f5f3ff
             Box-Shadow: 0 0 0 3px rgba(124,58,237,0.1)
Transition:  all 0.2s
```

---

### **Cards & Panels**

#### Default Panel
```
Background:  #fff
Border:      1px solid #e5e7eb
Border-Radius: 14px
Box-Shadow:  0 10px 25px rgba(15,20,25,.08),
             0 4px 12px rgba(15,20,25,.05)
Padding:     20-24px
Overflow:    hidden
```

#### Gradient Section
```
Background:  linear-gradient(135deg, #f0f9ff 0%, #eff6ff 100%)
Border:      1px solid #bfdbfe
```

---

### **Badges & Tags**

#### Department Tag
```
Padding:     5px 10px
Border-Radius: 20px
Background:  linear-gradient(135deg, #ede9fe 0%, #f3e8ff 100%)
Color:       #6d28d9
Font:        10px, weight 700
```

#### Code Tag
```
Padding:     5px 8px
Border-Radius: 6px
Background:  #f3f4f6
Color:       #4b5563
Font:        10px, weight 600 (monospace)
```

---

### **Avatars**

#### User Avatar
```
Width:       32-34px
Height:      32-34px
Border-Radius: 8px
Background:  linear-gradient(135deg, #ede9fe 0%, #e0e7ff 100%)
Color:       #5b21b6
Font:        10px, weight 700
Display:     Initials (2 letters)
```

#### File Badge
```
Width:       40px
Height:      44px
Border-Radius: 8px
Background:  linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%)
Color:       #065f46
Font:        9px, weight 800
Display:     "XLSX"
```

---

### **Metric Cards**

#### Purple Icon
```
Width/Height: 48px
Border-Radius: 12px
Background:  linear-gradient(135deg, #f3e8ff 0%, #ede9fe 100%)
Color:       #7c3aed
Font-Size:   20px
```

#### Green Icon
```
Width/Height: 48px
Border-Radius: 12px
Background:  linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%)
Color:       #059669
Font-Size:   20px
```

#### Blue Icon
```
Width/Height: 48px
Border-Radius: 12px
Background:  linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)
Color:       #0369a1
Font-Size:   20px
```

---

### **Alerts & Notices**

#### Success Notice
```
Background:  #f0fdf4
Border:      1px solid #22c55e
Border-Left: 4px solid #22c55e
Color:       #166534
Padding:     15px 16px
Border-Radius: 10px
Font:        13px
Line-Height: 1.6
```

#### Error Notice
```
Background:  #fef2f2
Border:      1px solid #ef4444
Border-Left: 4px solid #ef4444
Color:       #991b1b
Padding:     15px 16px
Border-Radius: 10px
Font:        13px
Line-Height: 1.6
```

---

### **Drop Zone**

#### Default State
```
Border:      2px dashed #c7d2e0
Border-Radius: 12px
Background:  linear-gradient(135deg, #f9fafb 0%, #f3f4f6 100%)
Min-Height:  220px
Padding:     40px 20px
Cursor:      pointer
Transition:  all 0.3s
```

#### Hover State
```
Border-Color: #7c3aed
Background:  linear-gradient(135deg, #f5f3ff 0%, #f0f9ff 100%)
```

#### Dragging State
```
Border-Color: #7c3aed
Background:  #f5f3ff
Transform:   scale(1.01)
```

---

### **Loading Spinner**

```
Width/Height: 18px
Border:      2.5px solid #e9d5ff
Border-Top-Color: #7c3aed
Border-Radius: 50%
Animation:   spin 0.6s linear infinite

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

---

### **Navigation Bar**

```
Height:      80px
Background:  rgba(255,255,255,0.95)
Backdrop-Filter: blur(8px)
Border-Bottom: 1px solid #e5e7eb
Box-Shadow:  0 2px 8px rgba(15,20,25,.03)
Gap:         48px
Padding:     0 48px

Active Link Indicator:
Height:      3px
Background:  linear-gradient(90deg, #7c3aed, #10b981)
Position:    absolute bottom -1px
Border-Radius: 2px
```

---

### **Empty State**

```
Padding:     64px 24px
Text-Align:  center
Color:       #6b7280 (muted)

Icon:
Width/Height: 56px
Border-Radius: 12px
Background:  linear-gradient(135deg, #f3e8ff 0%, #fef3c7 100%)
Color:       #7c3aed
Font-Size:   28px
Margin:      0 auto 20px

Heading:
Font:        18px, weight 700
Color:       #0f1419
Margin:      0 0 8px

Description:
Font:        14px
Color:       #6b7280
Margin:      0 0 20px
```

---

## ?? Animation Patterns

### Hover Lift (Buttons)
```css
.primary-button:hover {
  transform: translateY(-2px);
  transition: all 0.2s;
}
```

### Color Transition (Links)
```css
a {
  color: #7c3aed;
  transition: color 0.2s, text-decoration 0.2s;
}

a:hover {
  text-decoration: underline;
  color: #5b21b6;
}
```

### Drag Scale (Drop Zone)
```css
.drop-zone.dragging {
  transform: scale(1.01);
  transition: all 0.3s;
}
```

### Focus Ring (Accessibility)
```css
input:focus {
  border-color: #7c3aed;
  box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
}
```

---

## ?? Sizing Reference

### Common Heights
```
Extra Small:  32px
Small:        40px
Medium:       44px
Large:        48px
Extra Large:  56px
```

### Common Widths
```
Avatar:       32-34px
Icon:         48px
Button Badge: 40px
Metric Icon:  48px
File Badge:   40px
```

---

## ?? Color Usage Guidelines

| Color | Usage | Hex |
|-------|-------|-----|
| Navy | Primary text, dark backgrounds | #0f1419 |
| Purple | Actions, accents, links | #7c3aed |
| Green | Success, positive feedback | #10b981 |
| Gray | Secondary text, borders | #6b7280 |
| Light Gray | Subtle borders | #e5e7eb |
| Red | Errors, destructive actions | #ef4444 |
| Blue | Information, alternative actions | #0369a1 |

---

## ? Accessibility Checklist

? **Color Contrast**
- Text on background: 7:1+ (AAA standard)
- Button text: 7:1+
- Form labels: 4.5:1+

? **Focus States**
- All interactive elements: visible outline
- Color: Purple (#7c3aed)
- Not relying on color alone

? **Touch Targets**
- Minimum: 44px × 44px
- Buttons: 44px height
- Links: 40px height minimum

? **Motion**
- No auto-playing animations
- Respect `prefers-reduced-motion`
- Smooth transitions: 0.2-0.6s

---

## ?? Responsive Adjustments

### Desktop (> 900px)
- Full layout
- All features visible
- Padding: 48px

### Tablet (680-900px)
- 2-column layouts
- Optimized grid
- Padding: 32px

### Mobile (< 680px)
- Single column
- Stacked layout
- Touch-friendly buttons: full width
- Padding: 18px

### Small Mobile (< 430px)
- Ultra-compact
- Large touch targets
- Minimal spacing
- Padding: 16px

