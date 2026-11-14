# Ветка Surrare

Сначала прими приглашение в https://github.com/Nkdr21/final-project. Дождись, пока владелец отправит исходный код: локальная работа не появляется на GitHub автоматически.

```sh
git clone https://github.com/Nkdr21/final-project.git
cd final-project
git switch -c codex/surrare-neighborhoods
code .
```

Проверь `git config user.name` и `git config user.email`; укажи свои данные, если нужно.

ZIP содержит подготовленный с помощью AI код. Прочитай его, проверь и доработай, а не выдавай за независимо написанный с нуля. Основные темы: LINQ Where/All, разделение удобств по WalkMinutes, пустые результаты, проверки отзывов и тесты SQLite.

Перед копированием сделай `git status`: рабочая папка должна быть чистой. Прочитай APPLY.md в ZIP. Если эти изменения уже в main, не создавай фиктивный commit; выбери реальное улучшение, например сортировку ЖК, дополнительные граничные тесты или улучшение доступности формы отзыва.

```sh
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Dwellio.Web --launch-profile http
```

После проверки посмотри `git diff`, добавь только свои изменённые файлы и выполни:

```sh
git commit -m "Review and improve neighbourhood filtering"
git push -u origin codex/surrare-neighborhoods
```

На GitHub создай Pull Request: base `main`, compare `codex/surrare-neighborhoods`. Опиши конкретные изменения, тесты и использование подготовленного кода. Второй участник проверяет PR до merge. Force push не нужен.
