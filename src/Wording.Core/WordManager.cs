namespace Wording.Core
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Repository;

    public class WordManager
    {
        private readonly IRepository _repository;

        public WordManager()
        {
            _repository = new WordRepository();
        }

        public IEnumerable<Word> GetWords()
        {
            return _repository.GetAll();
        }

        public List<Word> GetWordsData()
        {
            _repository.RefreshData();
            return _repository.GetAll().ToList();
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