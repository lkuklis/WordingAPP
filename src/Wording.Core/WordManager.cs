using System.Collections.Generic;
using System.Linq;

using Wording.Core.Repository;

namespace Wording.Core
{
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