---
title: "Angular Components Deep Dive"
order: 3
story_points: 2
depends_on: 02-angular
quiz: angular-components-q1
quiz_questions:
  - text: "Which decorator defines an Angular component?"
    options: ["@Module", "@Injectable", "@Component", "@Directive"]
    correct_index: 2
  - text: "Which binding syntax is used for an event in Angular templates?"
    options: ["[click]", "(click)", "{{click}}", "*click"]
    correct_index: 1
  - text: "What is the primary purpose of ngOnInit?"
    options: ["Destroy component state", "Run logic after input-bound properties are initialized", "Register routes", "Compile templates at runtime"]
    correct_index: 1
---

This section deepens understanding of Angular components and component communication patterns.

Learning objectives:

1. Component inputs and outputs
- Use `@Input()` to pass data from parent to child components.
- Use `@Output()` and `EventEmitter` to raise events from child to parent.
- Apply one-way data flow for predictable component behavior.

2. Component lifecycle
- Explain common lifecycle hooks (`ngOnInit`, `ngOnChanges`, `ngOnDestroy`).
- Use lifecycle hooks for initialization and cleanup.

3. Reusable UI composition
- Design reusable presentational components.
- Separate container vs presentational responsibilities.
- Keep components focused and testable.

Hands-on exercises:

- Build a parent and child component pair using input/output binding.
- Add lifecycle hook logs to observe render and update behavior.
- Refactor a large component into smaller reusable components.

Completion criteria:

- Trainee demonstrates parent-child communication using input/output.
- Trainee explains when to use `ngOnInit` and `ngOnDestroy`.
- Trainee completes the section quiz and submits results for instructor review in Jira.
