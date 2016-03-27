using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Wording.Core.Repository
{
    public class WordRepository : IRepository
    {
        private List<Word> _words;

        private XDocument _wordsDocument;

        private readonly string _documentFileName = ConfigurationManager.AppSettings["defaultFileName"];

        public WordRepository()
        {
            this.RefreshData();
        }

        public void RefreshData()
        {
            this._words = new List<Word>();
            this._wordsDocument = XDocument.Load(_documentFileName);
            var wordList = (from words in this._wordsDocument.Descendants("Word")
                            select
                                new Word
                                    {
                                        Id = (int)words.Element("Id"),
                                        OriginalValue = words.Element("Original").Value,
                                        TranslationValue = words.Element("Translated").Value
                                    }).ToList();

            this._words.AddRange(wordList);
        }

        public IEnumerable<Word> GetAll()
        {
            return this._words;
        }

        public Word GetWordById(int id)
        {
            return this._words.Find(word => word.Id == id);
        }

        public void AddWord(Word word)
        {
            word.Id = (from b in this._wordsDocument.Descendants("Word")
                       orderby (int)b.Element("Id") descending
                       select (int)b.Element("Id")).FirstOrDefault() + 1;

            this._wordsDocument.Root.Add(
                new XElement(
                    "Word",
                    new XElement("Id", word.Id),
                    new XElement("Original", word.OriginalValue),
                    new XElement("Translated", word.TranslationValue)));

            this._wordsDocument.Save(_documentFileName);
        }

        public void DeleteWord(int id)
        {
            this._wordsDocument.Root.Elements("Word").Where(i => (int)i.Element("Id") == id).Remove();
            this._wordsDocument.Save(_documentFileName);
        }

        public void EditWord(Word word)
        {
            XElement node = this._wordsDocument.Root.Elements("Word").FirstOrDefault(i => (int)i.Element("Id") == word.Id);

            node.SetElementValue("Original", word.OriginalValue);
            node.SetElementValue("Translated", word.TranslationValue);

            this._wordsDocument.Save(_documentFileName);

        }
    }
}