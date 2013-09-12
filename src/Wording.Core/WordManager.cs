namespace Wording.Core
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Wording.Core.Repository;

    public class WordManager
    {
        public WordsDataSet WordDataSet;

        private IRepository repository;

        public WordManager()
        {
            repository = new WordRepository();
        }

        public IEnumerable<Word> GetWords()
        {
            return repository.GetAll();
        }

        public DataTable GetWordsData()
        {
            repository.RefreshData();
            var words = repository.GetAll().ToList();
            return words.ToDataTable();
        }

        public void AddWord(string original, string translated)
        {
            repository.AddWord(new Word
                                   {
                                       OriginalValue = original,
                                       TranslationValue = translated
                                   });
        }
       
    }


}