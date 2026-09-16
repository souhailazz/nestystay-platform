# Calendar Sync Final Acceptance

## Acceptance criteria

| Criterion | Result | Notes |
| --- | --- | --- |
| Connect an inbound ICS URL | PASS | Host-owned property route validates URL and persists feed |
| Reject private/link-local/metadata destinations | PASS | Existing safety tests retained |
| Reject embedded credentials and unsupported ports | PASS | Existing safety tests retained |
| Do not follow redirects | PASS | HTTP client disables redirects and controller rejects 3xx |
| Bound request duration and response size | PASS | 30-second named client timeout and 5 MB streaming limit |
| Persist sync attempts and failures | PASS | `MilestoneCalendarSyncEvent` history and feed error state |
| Retry a failed feed | PASS | Next attempt is scheduled five minutes later |
| Avoid overlapping worker pulls | PASS | Static per-feed running guard |
| Preserve imported UID identity across updates | PASS | UID-based update path |
| Remove cancelled or removed external events | PASS | Soft-delete preserves history while releasing availability |
| Include imported blocks in availability | PASS | Source is `ExternalCalendar` |
| Add/edit/release manual host blocks | PASS | API and Calendar page controls |
| Block booking quote and create on calendar holds | PASS | Quote/create checks external and manual blocks |
| Generate outbound ICS | PASS | Authenticated download and private token endpoint |
| Rotate and revoke private subscription | PASS | Active token is revoked before replacement |
| Keep outbound feed free of guest identity | PASS | Export regression asserts host ID is absent |
| Apply persisted rate overrides to quotes | PASS | Server-side quote regression |
| Apply persisted promotions to quotes | PASS | Discount appears in quote subtotal/breakdown |
| Protect pricing mutations by host ownership | PASS | API and persistence checks |
| PM dashboard without rental bookings | PASS | Regression test added; PM data is property/owner scoped |

## Commands

```text
dotnet build NestyStay.sln --no-restore
dotnet test tests/NestyStay.Api.Tests/NestyStay.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~CalendarEndpointTests|FullyQualifiedName~SpecCompletionEndpointTests.PublicContentExperiencesJournalAndAuthFlowsArePersistedAndValidated"
```

The targeted run completed with 12 passing tests and zero failures after the
calendar, pricing, and non-rental PM changes. A focused browser run covering
desktop, tablet, and mobile calendar operations also completed with 3 passing
tests and zero failures.

The complete backend suite completed with 183 passing tests and zero failures.
The full browser matrix reached all 191 scheduled tests; 99 passed before the
local frontend dev server stopped responding, 41 failed with connection
refused, 30 were intentional skips, and 21 did not run. Those 41 failures are
test-environment availability failures rather than application assertions and
must be rerun with supervised services before calling the browser matrix
green.

## External/staging acceptance still required

- Subscribe Airbnb, Booking.com, and VRBO test exports to the private NestyStay
  feed and verify provider refresh behavior.
- Verify staging nginx routing, TLS, database migration application, worker
  restart behavior, and external feed connectivity.
- Verify the exact provider credentials and rate limits chosen by the client.
