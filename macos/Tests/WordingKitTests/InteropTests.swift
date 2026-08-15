import Foundation
import Testing

@testable import WordingKit

/// Zgodnosc formatu z powlokami .NET. To jest najwazniejszy test w tym pakiecie:
/// words.json jest jedynym kontraktem miedzy aplikacja macOS a wersjami .NET,
/// wiec kazda rozbieznosc w serializacji cicho rozjezdza dane uzytkownika.
@Suite struct InteropTests {

    /// Doslowny fragment pliku zapisanego przez System.Text.Json, razem ze
    /// stanem powtorek wygenerowanym przez klikniecia w powloce Avalonii.
    static let plikZDotNet = """
        {
          "version": 1,
          "words": [
            {
              "id": "2a1e11cb-7062-43bc-b68e-865fc3efea0e",
              "original": "scope",
              "translation": "zakres",
              "createdUtc": "2026-08-14T22:18:18.405614+00:00",
              "review": {
                "repetitions": 1,
                "intervalDays": 1,
                "easeFactor": 2.36,
                "dueUtc": "2026-08-15T22:18:43.812289+00:00",
                "lastReviewedUtc": "2026-08-14T22:18:43.812289+00:00",
                "lapses": 0
              }
            },
            {
              "id": "7fc14c29-429f-4217-86a0-0e9bc4f0d511",
              "original": "implicitly",
              "translation": "domyślnie, bezwzględnie, bez zastrzeżeń",
              "createdUtc": "2026-08-14T22:18:18.405614+00:00",
              "review": {
                "repetitions": 0,
                "intervalDays": 0,
                "easeFactor": 2.5,
                "dueUtc": "2026-08-14T22:18:18.405614+00:00",
                "lapses": 0
              }
            }
          ]
        }
        """

    @Test func odczytujePlikZapisanyPrzezDotNet() throws {
        let plik = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.plikZDotNet.utf8)
        )

        #expect(plik.version == 1)
        #expect(plik.words.count == 2)

        let scope = plik.words[0]
        #expect(scope.original == "scope")
        #expect(scope.translation == "zakres")
        #expect(scope.id == UUID(uuidString: "2a1e11cb-7062-43bc-b68e-865fc3efea0e"))
        #expect(scope.review.repetitions == 1)
        #expect(scope.review.easeFactor == 2.36)
        #expect(scope.review.lastReviewedUtc != nil)
    }

    @Test func obslugujeDateZSzescioCyframiUlamkaSekundy() throws {
        // .NET zapisuje mikrosekundy; standardowe .iso8601 w Swift tego nie przyjmuje.
        let data = WordingJSON.parseDate("2026-08-14T22:18:18.405614+00:00")

        #expect(data != nil)
    }

    @Test func brakLastReviewedUtcOznaczaSlowkoNigdyNiepowtarzane() throws {
        let plik = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.plikZDotNet.utf8)
        )

        // .NET pomija klucz zamiast zapisywac null (DefaultIgnoreCondition).
        #expect(plik.words[1].review.lastReviewedUtc == nil)
    }

    @Test func zapisPomijaLastReviewedUtcTakSamoJakDotNet() throws {
        let slowo = Word(
            original: "scope",
            translation: "zakres",
            createdUtc: Date(),
            review: .new(now: Date())
        )

        let json = String(
            data: try WordingJSON.encoder.encode(WordFile(version: 1, words: [slowo])),
            encoding: .utf8
        )!

        #expect(!json.contains("lastReviewedUtc"))
    }

    @Test func zapisujeGuidyMalymiLiterami() throws {
        // Swift domyslnie zapisuje UUID wielkimi literami; bez korekty kazdy
        // zapis z macOS przepisywalby wszystkie identyfikatory w pliku.
        let slowo = Word(
            id: UUID(uuidString: "2A1E11CB-7062-43BC-B68E-865FC3EFEA0E")!,
            original: "scope",
            translation: "zakres",
            createdUtc: Date(),
            review: .new(now: Date())
        )

        let json = String(
            data: try WordingJSON.encoder.encode(WordFile(version: 1, words: [slowo])),
            encoding: .utf8
        )!

        #expect(json.contains("2a1e11cb-7062-43bc-b68e-865fc3efea0e"))
        #expect(!json.contains("2A1E11CB"))
    }

    @Test func pelnaRundaTamIzPowrotemNieGubiDanych() throws {
        let oryginal = try WordingJSON.decoder.decode(
            WordFile.self,
            from: Data(Self.plikZDotNet.utf8)
        )

        let zapisany = try WordingJSON.encoder.encode(oryginal)
        let ponownie = try WordingJSON.decoder.decode(WordFile.self, from: zapisany)

        #expect(ponownie.words == oryginal.words)
        #expect(ponownie.words[1].translation == "domyślnie, bezwzględnie, bez zastrzeżeń")
    }

    @Test func katalogDanychJestTenSamCoWDotNet() {
        let sciezka = WordingPaths.dataFile().path(percentEncoded: false)

        #expect(sciezka.hasSuffix("Library/Application Support/Wording/words.json"))
    }
}
