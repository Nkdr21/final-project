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

## Sourced rental examples

Review date: 2026-11-15. Retrieved pages may be cached, from several days to a month old. This is not the listing update date. Prices are individual asking rents, not complex averages. Availability has not been confirmed.

| Complex | Address | Rooms / area | Rent, KZT/month | Deposit, KZT | Source |
|---|---|---|---|---|---|
| GreenLine.Silva | E-899 5 | 1 / 35 m² | 250,000 | 100,000 | [Listing](https://krisha.kz/a/show/1010117448) |
| GreenLine.Tokyo | Mukhamedkhanov Street 4B | 1 / 48 m² | 290,000 | 50,000 | [Listing](https://krisha.kz/a/show/672000122), [readable search results](https://krisha.kz/arenda/kvartiry/astana/?das%5Bmap.complex%5D=1152) |
| GreenLine.Headliner | Tole Bi Street 48/1 | 2 / 61 m² | 400,000 | Unknown | [Listing](https://krisha.kz/a/show/1008481040) |
| Expo New Life-2 | Mangilik El Avenue 51/2 | 2 / 77 m² | 330,000 | 200,000 | [Listing](https://krisha.kz/a/show/1002990387) |
| Promenade Expo | Mangilik El Avenue 52 | 2 / 74 m² | 300,000 (archived) | Unknown | [Archived listing](https://krisha.kz/a/show/1017128178) |
| Dream City | Mangilik El Avenue 47 | 2 / 70 m² | 350,000 headline / 400,000 description | 200,000 in description | [Listing](https://krisha.kz/a/show/1013748996) |

Tokyo was checked against the complex search results because its individual page could not be fetched; no photo URL has been verified for it. Other examples were read from their individual listing pages. Silva, Tokyo, Expo New Life-2 and Dream City charge utilities separately; the amount is unknown. Headliner's utility amount is unknown. Expo New Life-2 requests two months of rent in advance. Promenade Expo is archived and shown only as a historical price example. Dream City's conflicting rent values are explicitly shown in its card evidence.

[BI Group](https://bi.group/ru/bigville/greenline) describes infrastructure across the wider GreenLine development. This does not verify a gym, park or coworking space inside every building. Coordinates, walking times, transport routes and bus occupancy have not been established for these examples.

Implementation: `src/Dwellio.Web/Data/MarketReferences.cs`, displayed in the main `/catalog` through `MarketCatalog`. No user database is overwritten. Real examples are not assigned demo commute scores.

## Source photos

Five photo URLs were extracted from image links on the matching individual listing pages. They are loaded directly from `krisha-photos.kcdn.online`; no copies are stored in the repository. Each card links to the original listing and credits Krisha.kz as its source. A blocked or missing image produces an explicit fallback. Tokyo has no verified image URL and uses the fallback.

The images are advertiser-supplied listing photos, not independently authenticated pictures. Attribution does not establish a redistribution license. Reuse permissions have not been obtained; obtain appropriate permission before redistributing or publicly deploying these photos. No BI Group photographs, owner contacts or full listing descriptions are copied.
