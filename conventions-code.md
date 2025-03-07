- **Naming Conventions**  
  -  Use *PascalCase* for class names, method names, properties, and namespaces (e.g., MyClass, ExecuteOrder).  
  -  Use *camelCase* for local variables and method parameters (e.g., orderCount, customerName).  
  -  Use *ALL_CAPS* for constants (e.g., MAX_BUFFER_SIZE).  
  -  Choose clear, descriptive, and meaningful names; avoid abbreviations except for universally accepted ones (e.g., i, j for loop counters).

- **Code Organization**  
  -  Organize code into logical sections with clear regions, using comments and XML documentation for public interfaces.  
  -  Group related classes into namespaces that reflect the business domain or functionality.  
  -  Maintain consistency in file naming: file names should match the public class or interface name.

- **Formatting and Styling**  
  -  Use four spaces for indentation and the Allman style for braces (opening braces on a new line).  
  -  Limit line lengths to enhance readability and break long expressions into multiple lines when needed.  
  -  Write one statement per line; ensure blank lines separate methods and property declarations to enhance clarity.[7][31]

- **Code Documentation**  
  -  Use XML comments to document public classes, methods, properties, and interfaces.  
  -  Code comments should explain "why" rather than "what" (the code should be self-explanatory using meaningful names).  
  -  Place inline comments above code blocks rather than at the end of lines.


- **Single Responsibility Principle (SRP)**  
  -  Each class and method should have one clearly defined purpose.  
  -  Refactor large methods into smaller, single-purpose methods.

- **Open-Closed Principle (OCP)**  
  -  Design classes to be open for extension but closed for modification.  
  -  Favor abstraction (e.g., interfaces or abstract classes) to enable extending behavior without altering existing code.

- **Liskov Substitution Principle (LSP)**  
  -  Derived classes must be substitutable for their base classes without altering the correctness of the program.  
  -  Ensure that overridden methods do not change the expected behavior defined by the base class.

- **Interface Segregation Principle (ISP)**  
  -  Create fine-grained interfaces that are client-specific.  
  -  Avoid forcing any class to implement methods it does not use.

- **Dependency Inversion Principle (DIP)**  
  -  Depend on abstractions rather than concrete implementations.  
  -  Use dependency injection to inject concrete implementations into consumers, facilitating testing and decoupling.[3][9]

## 4. Exception Handling & Code Robustness

- **Exception Handling**  
  -  Use try-catch blocks only where you can handle exceptions meaningfully.  
  -  Catch specific exception types rather than a general Exception.  
  -  Log errors and provide useful error messages to aid debugging, while not exposing sensitive details.[1][7]

- **Asynchronous Programming**  
  -  Use async/await for I/O-bound, long-running operations.  
  -  Utilize Task.ConfigureAwait(false) as appropriate to avoid deadlocks, especially in library code.

## 5. Dependency Injection and Testing

- **Dependency Injection (DI)**  
  -  Ensure dependencies are provided through constructors or proper DI containers instead of hardcoding them in classes.  
  -  Define and depend on interfaces to improve testability and modularity.

- **Unit Testing**  
  -  Write unit tests using frameworks like MSTest, NUnit, or xUnit.  
  -  Keep methods small and focused to facilitate easier test coverage.  
  -  Structure test cases to mirror the design of the application (e.g., testing the domain logic separately from infrastructure).