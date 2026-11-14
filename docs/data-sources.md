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

## Реальные предложения: исследовательская выборка

Дата просмотра доступных страниц: 15.11.2026. Полученные страницы могут быть кэшированными (возраст снимков — от нескольких дней до месяца); это не дата обновления объявления. Цены — запрошенная месячная аренда конкретных квартир, не средняя цена ЖК. Доступность не подтверждена.

| ЖК | Адрес из объявления | Комнаты / площадь | Аренда | Депозит | Источник |
|---|---|---|---|---|---|
| GreenLine.Silva | Е-899 5 | 1 / 35 м² | 250 000 ₸ | 100 000 ₸ | [Объявление](https://krisha.kz/a/show/1010117448) |
| GreenLine.Tokyo | Мухамедханова 4б | 1 / 48 м² | 290 000 ₸ | 50 000 ₸ | [Объявление](https://krisha.kz/a/show/672000122), [прочитанная выдача](https://krisha.kz/arenda/kvartiry/astana/?das%5Bmap.complex%5D=1152) |
| GreenLine.Headliner | Толе би 48/1 | 2 / 61 м² | 400 000 ₸ | Не указан | [Объявление](https://krisha.kz/a/show/1008481040) |

Silva и Headliner проверены по тексту индивидуальных страниц. Tokyo — по выдаче ЖК: индивидуальная страница не загрузилась. Для Silva и Tokyo коммунальные услуги оплачиваются отдельно, сумма неизвестна. Для Headliner стоимость коммунальных услуг не указана.

[BI Group](https://bi.group/ru/bigville/greenline) описывает инфраструктуру всего GreenLine. Эти сведения не доказывают наличие спортзала, парка или коворкинга в каждом отдельном корпусе. Проверенные координаты, маршруты, время пешком и загруженность автобусов для этих примеров отсутствуют.

Код: `src/Dwellio.Web/Data/MarketReferences.cs`; интерфейс `/market`. Реальные примеры отделены от синтетических объектов подбора и не записываются поверх пользовательской SQLite-базы. Фотографии доступны на исходных страницах; права на повторную публикацию не получены, поэтому фотографии не копируются. Контакты владельцев и полные тексты описаний не импортируются.

### Мәңгілік Ел: дополнение 15.11.2026

| ЖК | Адрес | Комнаты / площадь | Аренда в источнике | Депозит | Источник и статус |
|---|---|---|---|---|---|
| Expo New Life-2 | Мәңгілік Ел 51/2 | 2 / 77 м² | 330 000 ₸ | 200 000 ₸ | [Объявление](https://krisha.kz/a/show/1002990387); актуальная доступность не подтверждена |
| Promenade Expo | Мәңгілік Ел 52 | 2 / 74 м² | 300 000 ₸ | Не подтверждён | [Архивное объявление](https://krisha.kz/a/show/1017128178); только исторический пример цены |
| Dream City | Мәңгілік Ел 47 | 2 / 70 м² | 350 000 ₸ | Не подтверждён | [Объявление](https://krisha.kz/a/show/1013748996); актуальная доступность не подтверждена |

Адреса и характеристики сопоставлены с индивидуальными страницами и поисковой выдачей Krisha.kz. Для Expo New Life-2 указаны отдельная оплата коммунальных услуг и предоплата аренды за два месяца. Это условие отражено в карточке. В интерфейсе добавлен фильтр `Along Mangilik El avenue`; архивный Promenade Expo явно отмечен. Все шесть примеров остаются исследовательской выборкой, не текущей базой свободных квартир.
