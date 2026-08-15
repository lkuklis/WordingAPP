# Word packs

Every file here is a **word pack**: a list of words anyone can import into Wording from a
URL, under *Learning set → Import from a URL…*.

Copy the raw address of a file and paste it into the app:

```
https://raw.githubusercontent.com/lkuklis/WordingAPP/master/learning_data/spanish-travel.json
```

An imported pack becomes a **set of its own**, in its own file. It never merges into your
own words and never touches another set, so importing cannot disturb whatever you are
learning at the time. Importing the same pack again adds only the words your copy does not
have yet — your review progress on the rest is left alone.

## The format

```json
{
  "id": "spanish-travel",
  "name": "English → Spanish, travel",
  "description": "Words you need in an airport, a station and a hotel.",
  "words": [
    { "original": "airport", "translation": "aeropuerto" }
  ]
}
```

| Field | |
|---|---|
| `id` | Becomes the file name of the imported set. **Lower-case letters, digits and hyphens only**, up to 64 characters, no hyphen at either end. |
| `name` | What the app shows before the import is confirmed. Up to 80 characters. |
| `description` | Optional, up to 300 characters. |
| `kind` | Optional, `"vocabulary"` (the default) or `"concepts"`. Decides whether the app labels the two sides *Word / Translation* or *Term / Definition*. Anything unrecognised reads as vocabulary rather than failing the import. |
| `words` | Up to 5000 entries, each with a non-empty `original` and `translation` of at most 200 characters. |

The whole file must be under 2 MB and be served over **https**.

`id` is checked strictly because it decides which file gets written on someone else's
machine. An id that is not a plain slug is refused rather than cleaned up, and names
Windows reserves (`con`, `nul`, `com1`…) are rejected on every platform — the two apps
share this format, so a pack has to be storable on both.

## Contributing a pack

Add a `.json` file here and open a pull request. CI validates every pack in this directory
with the same parser the app uses, so a file that would be refused on import fails the
build instead of reaching anyone.

Keep one language pair per pack and give it a name that says which direction it goes.

A pack does not have to be vocabulary. The two sides are just short texts, so terms and
their definitions work as well — set `"kind": "concepts"` and the app labels them
accordingly. See [it-interview-concepts.json](it-interview-concepts.json).

**[PROMPT.md](PROMPT.md) has a ready prompt for generating one with an AI.** It carries the
limits above verbatim, so a model that follows it produces a file that imports cleanly;
a test fails if the prompt and the validator ever drift apart.
