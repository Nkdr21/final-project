"""Create source-only handoff archives. No credentials, databases or build output."""
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
out = root / 'deliverables'
out.mkdir(exist_ok=True)
friend_files = [
    'src/Dwellio.Core/Services/NeighborhoodService.cs',
    'src/Dwellio.Web/Components/Pages/Complexes.razor',
    'src/Dwellio.Web/Components/Pages/ComplexPage.razor',
    'src/Dwellio.Web/Components/Shared/AmenityGroup.razor',
    'tests/Dwellio.Tests/NeighborhoodTests.cs',
    'docs/friend-branch-ru.md', 'docs/team-plan.md',
]
apply = '''# Surrare neighbourhood contribution


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
'''
with zipfile.ZipFile(out / 'Dwellio-Surrare-neighborhoods.zip', 'w', zipfile.ZIP_DEFLATED) as z:
    for name in friend_files: z.write(root / name, name)
    z.writestr('APPLY.md', apply)
exclude = {'.git', 'bin', 'obj', 'App_Data', 'uploads', 'TestResults', 'deliverables', '.DS_Store'}
with zipfile.ZipFile(out / 'Dwellio-source.zip', 'w', zipfile.ZIP_DEFLATED) as z:
    for p in sorted(root.rglob('*')):
        rel = p.relative_to(root)
        if not p.is_file() or any(x in exclude for x in rel.parts): continue
        if p.suffix in {'.db', '.zip', '.pfx', '.key'} or p.name.startswith('.env'): continue
        if p.name.endswith(('.db-shm', '.db-wal')): continue
        z.write(p, str(rel))
for name in ['Dwellio-Surrare-neighborhoods.zip', 'Dwellio-source.zip']:
    with zipfile.ZipFile(out / name) as z:
        assert z.testzip() is None
        print(name, len(z.namelist()), 'files; integrity OK')
