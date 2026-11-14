# Data model

Identity data and housing data use separate SQLite files.

| Entity | Purpose and constraints |
|---|---|
| ApplicationUser / IdentityRole | Account identity, password hash and Student/Owner/Admin roles |
| ComplexEntity | Name, illustrative coordinates, description and amenity JSON |
| ListingEntity | Complex FK, owner ID, price, deposit, nullable utilities, availability, moderation flag and photo path |
| FavoriteEntity | Composite key `(UserId, ListingId)` and listing FK |
| ProfileEntity | One preference JSON document per user |
| ReviewEntity | Complex FK; unique `(UserId, ComplexId)`; rating 1–5 validated by service |

Identity user IDs are checked by the authenticated service. Cross-database user foreign keys are not implemented. Core records are projections of these storage entities: EF entities may mutate inside persistence operations; core inputs remain value-oriented records.
