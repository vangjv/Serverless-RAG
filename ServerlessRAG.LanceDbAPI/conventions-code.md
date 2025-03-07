## General Principles

- Write code that is easy to read, maintain, and test.  
- Adhere to PEP 8 for consistency in indentation, line length, and naming conventions.
- Favor clarity over cleverness and ensure that every function, class, or module has a clear purpose.

## Formatting and Naming Conventions

- **Indentation and Spacing:**  
  -  Use 4 spaces per indentation level.  
  -  Limit lines to 79–120 characters depending on team preferences.  

- **Naming:**  
  -  Use lowercase with underscores (snake_case) for function names, variables, and methods.  
  -  Use CamelCase for class names.  
  -  Write constant names in all uppercase with underscores.  
  -  Follow descriptive naming that reflects the item’s usage rather than its implementation details.

- **Comments and Docstrings:**  
  -  Use inline comments sparingly and include meaningful module- and function-level docstrings.  
  -  Adopt a consistent docstring style (such as Google or reStructuredText) that clearly describes parameters, return types, and exceptions.

## Strong Typing and Type Hints

- **Mandatory Annotations:**  
  -  Every function must include explicit type annotations for all parameters and return types.  
  -  Use the `typing` module (e.g., `List`, `Dict`, `Optional`, `Union`) to annotate complex types.  

- **Example:**  
  ```python
  from typing import List, Dict, Optional

  def fetch_user_data(user_id: int) -> Optional[Dict[str, str]]:
      """
      Retrieve user data based on user id.
  
      Args:
          user_id (int): The unique identifier of the user.
  
      Returns:
          Optional[Dict[str, str]]: A dictionary with user details if found; otherwise, None.
      """
      # Implementation logic here
      return None
  ```
  This example demonstrates clear type annotations and comprehensive documentation for API functions.

- **Static Type Checking:**  
  -  Integrate tools like mypy or pyright in the development workflow to enforce type correctness continuously.

## API Development Specifics

- **Framework Selection:**  
  -  For REST API development, leverage FastAPI to structure endpoints and manage HTTP requests and responses.

- **RESTful Best Practices:**  
  -  Design endpoints to represent clear and consistent resources; use noun-based URLs and HTTP verbs (GET, POST, PUT, DELETE) appropriately.  
  -  Implement proper versioning in URLs or headers to maintain backward compatibility.  
  -  Secure endpoints with methods such as token-based or OAuth 2.0 authentication, and enforce rate limiting and input validations to mitigate potential security risks.

- **Error Handling and Responses:**  
  -  Standardize error responses with clear error messages, relevant HTTP status codes, and consistent formatting (e.g., JSON error payloads).  
  -  Log errors and exceptions in a manner that facilitates debugging and monitoring.

## Code Organization and Modularization

- **Project Structure:**  
  -  Organize code into clear, separated modules and packages (e.g., controllers, models, services) to keep the application modular and testable.  
  -  Use Routers (FastAPI) to group related endpoints logically.
  
- **Environment and Dependency Management:**  
  -  Maintain a virtual environment and use dependency management tools (such as pipenv or Poetry) to track project dependencies.  
  -  Keep configuration separate from code, using environment variables or dedicated configuration files.

- **Testing:**  
  -  Write unit and integration tests for all significant code paths, including test cases for API endpoints, input validations, and error handling.  
  -  Incorporate static analysis and code quality tools (e.g., linters and formatters) in the continuous integration workflow.

## Documentation and Collaboration

- **Code Documentation:**  
  -  Generate interactive API documentation automatically when possible (as enabled by frameworks like FastAPI).  
  -  Document design decisions and public APIs clearly in README files or dedicated developer documentation.
  
- **Collaboration Standards:**  
  -  Encourage code reviews and enforce coding standards through automated checks.  
  -  Write clear commit messages and maintain a changelog to keep the development process transparent for team members.
