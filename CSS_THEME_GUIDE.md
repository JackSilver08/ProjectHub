# PROJECTHUB CSS Theme Guide

## Overview

This document outlines the professional design system used throughout the PROJECTHUB application. All styles follow a consistent, modern design methodology to ensure a cohesive user experience.

## Theme Files

### Core Theme
- **site.css** - Global styles, navbar, sidebar, modal templates, utilities
- **login.css** - Login and authentication pages
- **profile.css** - User profile pages
- **projects.css** - Project management pages
- **tasks.css** - Task management and board pages
- **dashboard.css** - Dashboard and notifications

## Color Palette

All colors are defined as CSS variables in the `:root` selector:

```css
--primary-color: #01373e          /* Dark teal - main brand color */
--primary-light: #015965          /* Light teal variant */
--primary-dark: #00252a           /* Darkest teal */
--secondary-color: #e6f4f1        /* Soft mint - hover backgrounds */
--accent-color: #00b894           /* Bright green - highlights & CTAs */
--text-dark: #333333              /* Dark text */
--text-light: #666666             /* Light gray text */
--border-color: #e0e0e0           /* Light borders */
--hover-light: #f8f9fa            /* Hover state background */
```

### Using Colors
Always reference CSS variables instead of hardcoding colors:

```css
/* ✓ Good */
color: var(--primary-color);
background: var(--secondary-color);

/* ✗ Avoid */
color: #01373e;
background: #e6f4f1;
```

## Typography

### Font Family
```css
font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
```

### Font Weights
- **700 (Bold)** - Page titles, card headers
- **600 (Semibold)** - Section headers, button labels, table headers
- **500 (Medium)** - Labels, smaller headings
- **400 (Regular)** - Body text, descriptions

### Size Scale
- **2rem (32px)** - Page titles
- **1.75rem (28px)** - Modal titles
- **1.5rem (24px)** - Section headers
- **1.1rem (18px)** - Subsection headers
- **1rem (16px)** - Regular text
- **0.95rem (15px)** - Labels
- **0.9rem (14px)** - Small text
- **0.85rem (13px)** - Extra small text

## Spacing System

### Values (using CSS variables)
```css
--radius-sm: 6px;                 /* Small components */
--radius-md: 10px;                /* Medium components */
--radius-lg: 16px;                /* Large components, cards */
```

### Padding/Margin Scale
- **2rem** - Large container padding
- **1.5rem** - Standard padding inside cards
- **1rem** - Medium spacing
- **0.75rem** - Small spacing
- **0.5rem** - Minimal spacing

## Shadow System

```css
--shadow-sm: 0 2px 8px rgba(1, 55, 62, 0.08);     /* Subtle shadows */
--shadow-md: 0 4px 16px rgba(1, 55, 62, 0.12);    /* Medium shadows */
--shadow-lg: 0 8px 32px rgba(1, 55, 62, 0.16);    /* Large shadows */
```

### Shadow Usage
- **shadow-sm**: Cards at rest, subtle hover states
- **shadow-md**: Cards on hover, moderate elevation
- **shadow-lg**: Modals, dropdowns, lifted components

## Transition & Animation

### Timing Function
```css
--transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
```

Use this for all state changes:
```css
/* ✓ Good */
button {
    transition: var(--transition);
}

button:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-lg);
}

/* ✗ Avoid */
button {
    transition: all 0.1s linear;
}
```

## Component Styles

### Buttons

#### Primary Button
```html
<button class="btn btn-primary">
    Label
</button>
```

```css
.btn-primary {
    background: linear-gradient(135deg, var(--primary-color), var(--primary-light));
    border: none;
    color: white;
    padding: 0.875rem 1.5rem;
    border-radius: var(--radius-sm);
    font-weight: 600;
    box-shadow: 0 4px 15px rgba(1, 55, 62, 0.2);
    transition: var(--transition);
}

.btn-primary:hover {
    background: linear-gradient(135deg, var(--primary-dark), var(--primary-color));
    transform: translateY(-2px);
    box-shadow: 0 6px 25px rgba(1, 55, 62, 0.3);
}
```

#### Secondary Button
```html
<button class="btn btn-secondary">
    Label
</button>
```

```css
.btn-secondary {
    background: white;
    border: 2px solid var(--border-color);
    color: var(--text-dark);
    padding: 0.875rem 1.5rem;
    border-radius: var(--radius-sm);
    font-weight: 600;
    transition: var(--transition);
}

.btn-secondary:hover {
    background: var(--secondary-color);
    border-color: var(--primary-color);
    color: var(--primary-color);
}
```

### Form Controls

#### Input & Select
```css
.form-control,
.form-select {
    border: 2px solid var(--border-color);
    border-radius: var(--radius-sm);
    padding: 0.875rem 1rem;
    transition: var(--transition);
    font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}

.form-control:focus,
.form-select:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 3px rgba(1, 55, 62, 0.1);
    outline: none;
}

.form-label {
    font-weight: 600;
    color: var(--primary-color);
    margin-bottom: 0.5rem;
    font-size: 0.95rem;
}
```

### Cards

```css
.card {
    background: white;
    border: 1px solid var(--border-color);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-sm);
    transition: var(--transition);
}

.card:hover {
    box-shadow: var(--shadow-md);
    border-color: var(--primary-light);
}
```

### Badges & Badges

```css
.badge {
    display: inline-flex;
    align-items: center;
    padding: 0.35rem 0.75rem;
    border-radius: 20px;
    font-size: 0.8rem;
    font-weight: 600;
}

.badge.bg-success {
    background: #d1fae5 !important;
    color: #047857;
}

.badge.bg-danger {
    background: #fee2e2 !important;
    color: #991b1b;
}
```

## Gradient Patterns

### Primary Gradient
```css
background: linear-gradient(135deg, var(--primary-color), var(--primary-light));
```

### Secondary Gradient
```css
background: linear-gradient(135deg, var(--secondary-color), transparent);
```

### Text Gradient
```css
.text-gradient {
    background: linear-gradient(135deg, var(--primary-color), var(--accent-color));
    -webkit-background-clip: text;
    background-clip: text;
    color: transparent;
}
```

## Animations

### Fade In
```css
@keyframes fadeIn {
    from {
        opacity: 0;
    }
    to {
        opacity: 1;
    }
}

.element {
    animation: fadeIn 0.4s ease forwards;
}
```

### Slide Up
```css
@keyframes slideInUp {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.element {
    animation: slideInUp 0.6s ease forwards;
}
```

## Responsive Design

### Breakpoints

```css
/* Large screens (1200px+) - Default */

/* Medium screens (768px - 1199px) */
@media (max-width: 992px) {
    /* Adjust layouts, reduce font sizes */
}

/* Small screens (576px - 767px) */
@media (max-width: 768px) {
    /* Adjust layouts, reduce padding */
}

/* Extra small screens (< 576px) */
@media (max-width: 576px) {
    /* Stack layouts, reduce sizes further */
}
```

## Best Practices

### 1. Use CSS Variables
Always use the defined CSS variables for colors, shadows, and spacing:

```css
/* ✓ Good */
padding: 1.5rem;
box-shadow: var(--shadow-md);
color: var(--text-light);

/* ✗ Avoid */
padding: 24px;
box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
color: #999999;
```

### 2. Consistent Transitions
Use the standard transition timing:

```css
/* ✓ Good */
transition: var(--transition);

/* ✗ Avoid */
transition: all 0.5s ease;
transition: opacity 0.2s linear;
```

### 3. Maintain Hover States
All interactive elements should have clear hover states:

```css
/* ✓ Good */
.btn {
    transition: var(--transition);
}

.btn:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-lg);
}

/* ✗ Avoid */
.btn {
    /* No hover state */
}
```

### 4. Mobile-First Approach
Design for mobile first, then add complexity for larger screens:

```css
/* Mobile styles by default */
.grid {
    grid-template-columns: 1fr;
}

/* Tablet and up */
@media (min-width: 768px) {
    .grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

/* Desktop and up */
@media (min-width: 1200px) {
    .grid {
        grid-template-columns: repeat(3, 1fr);
    }
}
```

### 5. Order of Properties
Follow this property order:
1. Display and positioning
2. Dimensions (width, height)
3. Margins and padding
4. Fonts and text
5. Colors and backgrounds
6. Borders and shadows
7. Transforms and transitions

```css
.card {
    /* Display & Position */
    display: flex;
    position: relative;
    
    /* Dimensions */
    width: 100%;
    min-height: 200px;
    
    /* Spacing */
    padding: 1.5rem;
    margin-bottom: 1rem;
    
    /* Fonts & Text */
    font-size: 1rem;
    font-weight: 600;
    line-height: 1.6;
    
    /* Colors & Backgrounds */
    background: white;
    color: var(--text-dark);
    
    /* Borders & Shadows */
    border: 1px solid var(--border-color);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-sm);
    
    /* Transforms & Transitions */
    transition: var(--transition);
}
```

## Adding New Pages

When creating a new page:

1. Create a new CSS file in `wwwroot/css/` named `[page-name].css`
2. Import the theme variables at the top
3. Use the defined color variables and spacing scale
4. Follow the naming conventions and patterns
5. Add the CSS file reference to the HTML page using `@section Styles`

Example:

```razor
@page
@model MyModel

@{
    ViewData["Title"] = "My Page";
}

@section Styles {
    <link rel="stylesheet" href="~/css/mypage.css" asp-append-version="true" />
}
```

```css
/* mypage.css */

:root {
    --primary-color: #01373e;
    --primary-light: #015965;
    --primary-dark: #00252a;
    --secondary-color: #e6f4f1;
    --accent-color: #00b894;
    --text-dark: #333333;
    --text-light: #666666;
    --border-color: #e0e0e0;
    --shadow-sm: 0 2px 8px rgba(1, 55, 62, 0.08);
    --shadow-md: 0 4px 16px rgba(1, 55, 62, 0.12);
    --radius-sm: 6px;
    --radius-md: 10px;
    --radius-lg: 16px;
    --transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.mypage-container {
    animation: fadeIn 0.4s ease forwards;
}

/* ... rest of styles ... */
```

## Common Patterns

### Hover Lift Effect
```css
.hover-lift {
    transition: var(--transition);
}

.hover-lift:hover {
    transform: translateY(-3px);
    box-shadow: var(--shadow-lg);
}
```

### Text Truncation
```css
.truncate {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.truncate-multi {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
```

### Gradient Overlay
```css
.gradient-overlay {
    position: relative;
}

.gradient-overlay::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(135deg, rgba(1, 55, 62, 0.1), transparent);
    border-radius: inherit;
}
```

## Maintenance

- Review CSS files regularly for unused styles
- Keep the color palette consistent across all files
- Test responsive design on real devices
- Update documentation when adding new design patterns
- Use the theme variables consistently to enable easy theme updates in the future

## Support

For questions about the design system or CSS implementation, refer to:
- site.css for core patterns
- Specific page CSS files for component examples
- This guide for detailed guidelines
