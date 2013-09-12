namespace Wording.Core.Repository
{
    using System.Collections.Generic;

    public interface IRepository
    {
        IEnumerable<Word> GetAll();

        Word GetWordById(int id);

        void AddWord(Word word);

        void DeleteWord(int id);
        
        void EditWord(Word word);

        void RefreshData();
    }
}