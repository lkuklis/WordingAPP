# Generating a pack with an AI

Paste the prompt below into any AI assistant, filling in the five bracketed values at the
top. Save what it gives you as `<id>.json` in this directory and open a pull request — CI
checks every file here with the same parser the app uses, so a pack that would be refused
on import fails the build instead of reaching anyone.

The rules in the prompt are not style advice: they are the app's actual limits. A model
that follows them produces a file that imports cleanly on both Windows and macOS.

---

````text
You are generating a word pack for Wording, a desktop app that shows one word at a time in
a system notification for spaced-repetition practice. The word goes in the notification
title and its translation in the body.

Fill these in:
- From language: [English]
- To language: [Spanish]
- Topic: [everyday travel]
- Level: [beginner]
- Number of words: [40]

Produce exactly one JSON document in this shape, with these key names:

{
  "id": "spanish-travel",
  "name": "English → Spanish, travel",
  "description": "Words you need in an airport, a station and a hotel.",
  "words": [
    { "original": "airport", "translation": "aeropuerto" }
  ]
}

HARD RULES. A file that breaks any of these is rejected by the app, so check each one
before you answer.

1. "id": lower-case letters a-z, digits and hyphens only. 1 to 64 characters. No hyphen at
   the start or the end. Never "con", "prn", "aux", "nul", "com1".."com9" or "lpt1".."lpt9"
   — Windows reserves those as file names and the pack has to work on both platforms.
2. "name": not empty, at most 80 characters. Say the direction, for example
   "English → Spanish, travel".
3. "description": optional, at most 300 characters.
4. "words": between 1 and 5000 entries. Every "original" and "translation" must be
   non-empty and at most 200 characters after trimming.
5. No tabs and no line breaks inside any value.
6. No two entries may share the same "original", ignoring case. Duplicates fail the build.
7. UTF-8 with the real accented characters — "estación", not "estacion" and not "estación".

QUALITY RULES.

- Keep both sides short enough to read at a glance in a notification banner. Prefer single
  words and short phrases over sentences.
- One entry per concept. When a word has several senses, put them in one translation
  separated by commas rather than repeating the "original" in another entry.
- Choose words a learner will actually meet in the topic, not rare ones that merely fit it.
- If the target language marks gender, include the article in the translation where a
  learner would need it, for example "la estación".
- No pronunciation guides, no transliteration, no example sentences, no notes in
  parentheses explaining the choice.
- Order the entries so related words sit together; the app randomises what it shows, so the
  order is for whoever reads the file.

OUTPUT. The JSON document and nothing else — no commentary before or after, no markdown
code fence.
````

---

## Packs that are not vocabulary

Nothing in the format says the two sides have to be a word and its translation. A pack is a
pair of short texts, so the same file works for terms and their definitions — interview
preparation, a certification syllabus, anything you would otherwise put on flashcards. See
[it-interview-concepts.json](it-interview-concepts.json).

The app labels the columns *Word* and *Translation* either way, which reads a little oddly
for concepts. That is the only thing you give up.

Use the prompt above with these changes:

````text
Instead of a translation, "translation" holds a short answer or definition for the term in
"original".

Keep every answer under about 120 characters. This is not the technical limit - it is the
one that matters. The answer appears in a notification body, which truncates after two or
three lines, and short items are recalled far better than long ones. If a concept will not
fit, it is really two concepts: split it into separate entries rather than writing more.

Write the answer so it stands on its own, without referring to another entry.

Prefer the distinguishing fact over the textbook opening. "Doing it twice has the same
effect as doing it once" beats "Idempotency is a property of certain operations whereby...".

Do not put the term itself inside its own answer - it turns the card into a giveaway.
````

## Checking it before you open a pull request

Save the file as `<id>.json` — **the file name has to match the `id` inside it**, or the
set lands under a name nobody chose.

If you have the repository checked out, both test suites validate this directory:

```bash
dotnet test --filter FullyQualifiedName~PublishedPackTests
cd macos && swift test --filter PublishedPackTests
```

Otherwise open the pull request and let CI do it.
