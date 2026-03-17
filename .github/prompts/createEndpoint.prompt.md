---
name: createEndpoint
description: Create a REST API endpoint based on the request
---

Create a new REST API endpoint in the application. Follow the structure and conventions used by existing endpoints in the project.

## Namespace

Create the correct namespace according to the structure used by other files in the `Features` folder. Name must be a plural. For example, if the endpoint is related to "User", the namespace should be `Features.Users.Create`.

## Endpoint structure

Each endpoint must consist of the following components (in this order):

1. **Command record**
   - Contains data coming from the JSON request body.
   - If the request body is empty, use the predefined `EmptyCommand`.

2. **Parameters record**
   - Contains data coming from query parameters.
   - If there are no query parameters, use predefined `EmptyParameters`.
   - If the only parameter is `Id`, use predefined `IdParameters`.

3. **Result record**
   - Represents the JSON response.
   - If the endpoint returns no data, use predefined `EmptyResult`.

4. **Handler class**
   - Derive the handler from:

     `GenericHandler<TCommand, TParameters, TResult>`

   - Implement the asynchronous method:

     `Task<TResult> HandleAsync(...)`

    - For **database**:
      - Use async database calls where appropriate.
      - If database access is required, inject `AppDbContext` via the primary constructor as:
    `AppDbContext dbContext`
      - If you need to get an entity from db by it ID and the entity is not found, return `EntityNotFoundException`, like
    `throw new EntityNotFoundException(typeof(Exam), parameters.Id);`
      - If you are returning entity into some `Record`, try map the entity directly in the `Select` command into the record using the constructor, like
    `.Select(e => new ExamRecord(e.Id, e.Title))`
      - If the previous is not possible, use
      `.AsNoTracking()` where possible.
      - If building database query using several methods invocation, add ` //` at the end of the first line, e.g.:

    ```
        var exam = await dbContext.Exams //
        .AsNoTracking()
        .Include(e => e.Stations)
        .FirstOrDefaultAsync(e => e.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Exam), parameters.Id);
    ```

5. **Endpoint class**
   - Represents the Minimal API endpoint.
   - Inherit from:
     - `GenericOkEndpoint<Command, Parameters, Handler, Result>` if the endpoint returns **HTTP 200 OK** with any body
     - `GenericCreatedEndpoint<Command, Parameters, Handler, Result>` if the endpoint returns **HTTP 201 Created**
     - `GenericEndpoint<Command, Parameters, Handler>` otherwise, or if you need a specific behavior in the handling method. In this case, implement the behavior in the overridden `ProcessRequestAsync(...)` method. Also, in the method use the invocation of the `Handler` method serving the request.

   - If using `GenericCreatedEndpoint`, return the created entity ID using `IdResult`.

   - Choose the correct HTTP method:

     | Operation | Method |
     | --------- | ------ |
     | Read      | GET    |
     | Create    | POST   |
     | Update    | PATCH  |
     | Delete    | DELETE |

   - Suggest appropriate values for:
     - `BaseRoute`
     - `EndpointRoute`

   (If uncertain, provide a reasonable guess and add a TODO remark to review it later.)
   - Leave **RequiredRoles empty** unless specified otherwise.

6. **Endpoint summary**
   - Add the attribute:

     `[EndpointSummary(...)]`

   describing the endpoint.

## Validation rules

If a **custom `Command` or `Parameters` record** is created:

- Use validation attributes on properties:

  `[property: ...]`

- Only use validators defined in:

  `Exceptions.Validation`

- **Do NOT use standard .NET validation attributes.**

If a required validator does not exist, mention it in a comment.

## Uncertain behavior

If the endpoint behavior is unclear:

Add a `TODO` comment inside the `Handler` describing the expected logic so it can be implemented later.

## Important

Match the **coding style, naming conventions, and structure** used by existing endpoints in the project.
