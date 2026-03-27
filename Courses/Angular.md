# EPIC: Frontend / Angular Training

This epic focuses on enabling new hires to become productive members of our frontend development teams by providing a structured, hands-on training in **Angular and its ecosystem**. Most participants will be new to professional frontend development, though some may have prior academic or project-level exposure.

Through this training, participants will:

-   Gain a solid foundation in **TypeScript**, the language underlying Angular.
    
-   Learn the **core concepts of Angular**, including components, templates, data binding, services, dependency injection, routing, and module organization.
    
-   Develop an understanding of **reactive programming with RxJS**, learning how to handle asynchronous data streams in Angular applications.
    
-   Explore **state management using NgRx**, understanding best practices for managing complex application state.
    
-   Cover a set of **miscellaneous Angular topics** such as testing, performance optimization, styling, and coding standards.
    

The training is designed to be **practical and hands-on**, with short guided lectures followed by coding exercises and mini-projects. By the end of this epic, participants should be able to:

-   Contribute to real sprint tasks in Angular projects.
    
-   Follow and understand the team’s coding standards and architectural patterns.
    
-   Debug and reason about frontend issues using modern tooling.
    
-   Collaborate effectively with other team members in a professional frontend workflow.
    

**Expected Outcome**  
At the end of this epic, participants will be confident in reading, writing, and debugging Angular codebases and will be ready to take on beginner-to-intermediate sprint stories in active projects.

# 1- TypeScript

This story introduces participants to **TypeScript**, the language used to build Angular applications. Trainees will learn the fundamentals of TypeScript’s type system, how it enhances JavaScript with type safety, and how to write maintainable, scalable code using object-oriented and generic programming techniques. Advanced concepts like utility types, namespaces, and modules will be optionally covered based on time and trainee progress.

**Acceptance Criteria / Learning Objectives**

1.  **TypeScript Types & Compatibility**
    
    -   Trainee can declare and use basic types (string, number, boolean, null/undefined, any, unknown).
        
    -   Trainee can explain and demonstrate **type inference** in TypeScript.
        
    -   Trainee can combine types using **union**, **intersection**, and **type aliases**.
        
    -   Trainee can explain **structural typing** and TypeScript’s type compatibility rules.
        
2.  **Object-Oriented TypeScript**
    
    -   Trainee can create and use **interfaces** and **classes**.
        
    -   Trainee can demonstrate **inheritance, abstract classes, and interfaces**.
        
    -   Trainee can use **access modifiers** (public, private, protected, readonly).
        
    -   Trainee can apply **encapsulation** and **polymorphism** concepts in TypeScript.
        
3.  **Generic / Hybrid / Utility / Advanced Types (Optional)**
    
    -   Trainee can write and use **generic functions and classes**.
        
    -   Trainee understands and can apply **utility types** (e.g., Partial, Pick, Readonly, Record).
        
    -   Trainee can describe advanced type features like **mapped types** and **conditional types**.
        
4.  **Namespaces, Modules & Ecosystem (Optional)**
    
    -   Trainee can explain the difference between **namespaces** and **modules**.
        
    -   Trainee can import/export functions, classes, and interfaces across files.
        
    -   Trainee understands the **TypeScript compiler (**tsc**)**, configuration (tsconfig.json), and integration with build tools.
        

**Completion Criteria**

-   Trainee has completed small coding exercises demonstrating types, classes, and generics.
    
-   Trainee can explain concepts verbally and apply them in short examples.
    
-   Instructor reviews exercises and confirms understanding through Q&A.

## Hands-on Exercises

### 1\. Types & Compatibility

-   **Task:** Write a function formatUser that takes a user object { name: string, age: number } and returns a string. Try it first with explicit types, then remove them and see how **type inference** works.
    
-   **Task:** Define a printId function that accepts either a string or a number (union type). Show how TypeScript enforces safe access.
    
-   **Task:** Demonstrate **structural typing** by creating two different objects with the same shape and passing them to a typed function.
    

### 2\. Object-Oriented TypeScript

-   **Task:** Create a base Shape class with a method getArea(). Extend it into Rectangle and Circle classes. Show polymorphism by storing them in a single array and iterating over them.
    
-   **Task:** Add **access modifiers** to properties (e.g., make radius private in Circle) and show how it changes accessibility.
    

### 3\. Generics & Utility Types (Optional)

-   **Task:** Write a generic function wrapInArray<T>(value: T): T\[\] that takes any type and returns it inside an array.
    
-   **Task:** Create a generic class KeyValuePair<K, V> and demonstrate storing different typed pairs.
    
-   **Task:** Use **utility types**: create a User type and then make Readonly<User> and Partial<User>. Show how they change allowed operations.
    

### 4\. Namespaces, Modules & Ecosystem

-   **Task:** Split the Shape classes into separate files using **modules** (export/import).
    
-   **Task:** Configure a tsconfig.json file with strict mode enabled, then intentionally violate a type rule to see the compiler catch it.

# 2- Angular

This story introduces participants to **Angular** as a frontend framework and prepares them to work effectively within its opinionated ecosystem. Trainees will learn how to set up and run Angular projects, understand Angular’s application structure, and build core building blocks such as components, templates, and directives. They will also be introduced to **dependency injection**, a foundational but potentially new concept for many.

Beyond fundamentals, trainees will practice using Angular’s **routing, forms, HTTP client, and built-in testing framework**. They will also gain hands-on experience with the **Angular CLI**, learn its conventions, and understand how Angular applications are run and debugged locally—including within a **Dockerized environment**, as used in our teams.

**Acceptance Criteria / Learning Objectives**

1.  **Environment Setup & Angular CLI**
    
    -   Trainee can install Node.js, Angular CLI, and spin up a new Angular project.
        
    -   Trainee understands the Angular project structure (modules, components, assets, environment files).
        
    -   Trainee can generate components, services, and modules using Angular CLI commands.
        
2.  **Components, Templates & Directives**
    
    -   Trainee can create a new component and bind it into the application.
        
    -   Trainee understands and can use **interpolation, property binding, event binding, and two-way binding**.
        
    -   Trainee can apply **structural directives** (\*ngIf, \*ngFor) and **attribute directives** (ngClass, ngStyle).
        
3.  **Dependency Injection**
    
    -   Trainee understands the concept of dependency injection and its role in Angular.
        
    -   Trainee can create and inject services into components.
        
    -   Trainee can explain provider scopes (root, module, component-level).
        
4.  **Routing & Navigation**
    
    -   Trainee can configure routes and navigate between pages.
        
    -   Trainee understands route parameters and query parameters.
        
    -   Trainee can create a simple multi-page Angular app with navigation.
        
5.  **Forms & User Input**
    
    -   Trainee can build a form using **template-driven** and **reactive forms**.
        
    -   Trainee can add validation (required, min/max length, custom).
        
    -   Trainee understands form state and error handling.
        
6.  **HTTP & Data Handling**
    
    -   Trainee can use Angular’s HttpClient to make GET/POST requests.
        
    -   Trainee can handle responses and errors.
        
    -   Trainee can display data in a component using async patterns.
        
7.  **Testing & Debugging**
    
    -   Trainee can run and understand Angular’s default test setup (Jasmine/Karma).
        
    -   Trainee can write a basic component test.
        
    -   Trainee can use Angular DevTools in the browser for debugging.
        
8.  **Running Angular in Docker**
    
    -   Trainee can run an Angular project locally in Docker.
        
    -   Trainee understands the workflow of building an image and serving Angular.
        
    -   Trainee can explain the benefits of containerized frontend development.
        

**Completion Criteria**

-   Trainee has built and run a simple Angular application locally and in Docker.
    
-   Trainee has implemented navigation, a form with validation, and a component consuming data via HTTP.
    
-   Trainee has written and executed at least one unit test for a component or service.
    
-   Instructor has reviewed the application and validated functionality through Q&A and demos.
