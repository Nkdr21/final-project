# Sources and limitations

- Eight named complexes and 32 seeded apartments: fictional classroom data in `SeedData.cs`, not scraped listings.
- Coordinates: illustrative points in Astana, not verified locations of named complexes.
- Amenities/walking times: manually authored teaching examples, not measured routes.
- Bus times, routes and crowding: synthetic deterministic scenarios. No actual bus route number, occupancy feed or live forecast is claimed.
- University labels: destination labels for scenarios, not a verified map/routing integration to the campuses.
- Illustrations: original repository SVG artwork, not property photographs.
- User images/contact data: author supplied, not verified by Dwellio.
- Reviews: user opinions; residency unverified, counts visible and absent reviews never generate a rating.
- OpenStreetMap: external embedded map, visible attribution and link. It requires internet. No bulk/offline tile download.
- Fonts: local system fonts; no external font request.

Official references consulted:
- https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0
- https://operations.osmfoundation.org/policies/tiles/

A future real-route adapter needs verified coverage, cost, caching rules and attribution. Missing live crowding data must remain unknown.
