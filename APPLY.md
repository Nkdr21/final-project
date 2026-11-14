# Surrare neighbourhood contribution

This is a prepared AI-assisted contribution for review, learning and improvement.
It is an overlay for the Dwellio baseline, NOT a standalone application.

1. Accept the repository invite and wait for the owner to push the application baseline.
2. Clone/pull the baseline and create codex/surrare-neighborhoods.
3. Ensure git status is clean. Compare existing files with this archive before replacing anything.
4. Copy the src/, tests/ and docs/ files into matching paths only after reviewing differences.
5. Run dotnet build and dotnet test, then the app. Test complex name search, amenity AND filters,
   walking limits, inside/nearby grouping and review validation.
6. Read git diff. Commit actual reviewed changes with an accurate description and push your branch.
7. Open a PR to main and request the other student's review.

IMPORTANT: These changes are ALREADY integrated in the owner's local working directory.
If that complete version was pushed to main, applying this overlay will produce no changes.
Do not manufacture commits or claim original authorship. Make a real follow-up improvement
(e.g. additional edge-case tests or accessible filter summaries), document it and submit that PR.

Shared service review validation is in HousingService.cs in the baseline. Existing integration
coverage is in HousingServiceTests.cs. Do not overwrite those shared files blindly.
