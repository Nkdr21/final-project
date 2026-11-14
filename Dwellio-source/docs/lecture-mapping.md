# Functional C# lecture mapping

Reference supplied by the students: `L6_1_merged (1).pdf`, 1,537 pages. The following page numbers are physical PDF pages, not slide numbers.

| Topic | Lecture location | Dwellio evidence | Explanation |
|---|---|---|---|
| Pure functions | 3, 6 | RecommendationEngine, NeighborhoodService | Results depend on explicit arguments, without database calls |
| FP with OOP | 9 | Core functions plus HousingService | Functional calculations inside an ordinary C# application |
| Records / immutable updates | 38–47 | Preferences and UI `with` updates | New preference value rather than hidden mutation |
| Higher-order functions | 48–95 | FitsBudget returns Func; LINQ Where accepts it | Pass a rule as data |
| LINQ / declarative transformations | 53, 95 | Filter, Select, OrderBy, All, Any | Describe the selected result |
| Discriminated alternatives | 96 onward | RoutesFound / RoutesMissing / RoutesUnavailable | Different outcomes carry different information |
| Pattern matching | 41–42 | RouteResult and Crowding switches | Handle a specific case explicitly |
| Functional composition | 248 onward | Validate → filter → evaluate → sort | Small steps form a pipeline; no claim to implement a full monad library |
| Testing external boundaries | 1458–1460, 1528–1529 | Route interface; in-memory SQLite tests | Core is deterministic; effects are tested separately |

Not every advanced lecture concept is forced into the project. Currying libraries, recursion and memoization are not required for this small dataset. Explain why each used technique helps rather than reciting terminology.
