namespace Wording.Core.Storage;

public interface IWordStore
{
    IReadOnlyList<Word> GetAll();

    Word? GetById(Guid id);

    Word Add(string original, string translation);

    bool Remove(Guid id);

    /// <summary>Zapisuje zmiany w slowku juz obecnym w magazynie (np. po ocenie powtorki).</summary>
    bool Update(Word word);

    /// <summary>Wczytuje ponownie z dysku, odrzucajac stan z pamieci.</summary>
    void Reload();
}
