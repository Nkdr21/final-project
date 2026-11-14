# Recommendation rules

1. Validate the preference input.
2. Exclude unavailable, over-budget, too-small or unfurnished homes when furniture is required.
3. Require every mandatory amenity within the walking limit.
4. If route data exists, select a valid outward and return trip independently. Exclude the home if either direction has no route under the time/transfer limits.
5. If route data is absent/unavailable, retain the otherwise eligible home with an explicit warning and zero transport score. Its commute suitability is unknown, not confirmed.
6. Compute a weighted score, round it and sort descending. Catalog uses rent as the secondary order.

```text
price = clamp(1 - rent / budget, 0, 1)
lifestyle = matching desired categories / desired categories
travel = clamp(1 - (outboundMinutes + returnMinutes) / (2 * maxMinutes), 0, 1)
score = 100 * (price*wPrice + lifestyle*wLifestyle + travel*wTravel) / sum(weights)
```

No desired categories gives lifestyle zero, not an invented perfect score. All-zero or negative weights are rejected. No learning model is involved. The score is an explicit preference heuristic, not a probability or objective property quality.

The comfort route heuristic adds penalties for transfers and known/demo loads; an unknown load receives a conservative penalty and retains its unknown label. This formula illustrates tradeoffs rather than predicting actual comfort.

Monthly known subtotal = rent + known utilities + user transport estimate + applicable gym estimate. Deposit is separate. Missing utilities remain flagged; their omission does not turn them into confirmed zero costs.
