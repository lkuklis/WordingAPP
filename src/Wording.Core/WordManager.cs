namespace Wording.Core
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Repository;

    public class WordManager
    {
        public WordsDataSet WordDataSet;

        private readonly IRepository _repository;

        public WordManager()
        {
            _repository = new WordRepository();
        }

        public IEnumerable<Word> GetWords()
        {
            return _repository.GetAll();
        }

        public DataTable GetWordsData()
        {
            _repository.RefreshData();
            var words = _repository.GetAll().ToList();
            return words.ToDataTable();
        }

        public void AddWord(string original, string translated)
        {
            _repository.AddWord(new Word
                                   {
                                       OriginalValue = original,
                                       TranslationValue = translated
                                   });
        }


        public void RemoveWord(int id)
        {
            _repository.DeleteWord(id);
        }
    }


}