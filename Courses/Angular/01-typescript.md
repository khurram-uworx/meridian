---
title: "TypeScript Foundations for Angular"
order: 1
story_points: 2
---

This section introduces TypeScript fundamentals used in Angular applications.

Learning objectives:

1. TypeScript types and compatibility
- Declare and use basic types (string, number, boolean, null/undefined, any, unknown).
- Explain and demonstrate type inference.
- Combine types using union, intersection, and type aliases.
- Explain structural typing and type compatibility rules.

2. Object-oriented TypeScript
- Create and use interfaces and classes.
- Demonstrate inheritance, abstract classes, and interfaces.
- Use access modifiers (public, private, protected, readonly).
- Apply encapsulation and polymorphism.

3. Generic and advanced types (optional)
- Write and use generic functions and classes.
- Apply utility types (Partial, Pick, Readonly, Record).
- Describe mapped and conditional types.

4. Namespaces, modules, and ecosystem (optional)
- Explain namespaces vs modules.
- Import/export functions, classes, and interfaces across files.
- Understand tsc, tsconfig.json, and build tool integration.

Hands-on exercises:

- Write formatUser for a typed user object; compare explicit typing vs type inference.
- Build printId that accepts string | number and demonstrate safe access patterns.
- Show structural typing with different objects sharing the same shape.
- Implement Shape, Rectangle, and Circle classes and demonstrate polymorphism.
- Apply access modifiers and show accessibility changes.
- Create wrapInArray<T>(value: T): T[].
- Build a generic KeyValuePair<K, V> class.
- Use Readonly<User> and Partial<User> and compare behavior.
- Split Shape classes into modules using export/import.
- Configure strict tsconfig.json and verify compiler type checks.

Completion criteria:

- Trainee completes coding exercises on types, classes, and generics.
- Trainee can explain concepts and apply them in short examples.
- Instructor validates understanding via exercise review and Q&A.
