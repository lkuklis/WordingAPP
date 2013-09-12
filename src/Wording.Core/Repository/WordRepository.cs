namespace Wording.Core.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public class WordRepository : IRepository
    {
        private List<Word> words;

        private XDocument wordsDocument;

        public WordRepository()
        {
            this.RefreshData();
        }

        public void RefreshData()
        {
            this.words = new List<Word>();
            this.wordsDocument = XDocument.Load("WordsData.xml");
            var wordList = (from words in this.wordsDocument.Descendants("Word")
                            select
                                new Word
                                    {
                                        Id = (int)words.Element("Id"),
                                        OriginalValue = words.Element("Original").Value,
                                        TranslationValue = words.Element("Translated").Value
                                    }).ToList();

            this.words.AddRange(wordList);
        }

        public IEnumerable<Word> GetAll()
        {
            return this.words;
        }

        public Word GetWordById(int id)
        {
            return this.words.Find(word => word.Id == id);
        }

        public void AddWord(Word word)
        {
            word.Id = (from b in this.wordsDocument.Descendants("Word") 
                       orderby (int)b.Element("Id") descending 
                       select (int)b.Element("Id")).FirstOrDefault() + 1;

            this.wordsDocument.Root.Add(
                new XElement(
                    "Word",
                    new XElement("Id", word.Id),
                    new XElement("Original", word.OriginalValue),
                    new XElement("Translated", word.TranslationValue)));

            this.wordsDocument.Save("WordsData.xml");
        }

        public void DeleteWord(int id)
        {
            this.wordsDocument.Root.Elements("Word").Where(i => (int)i.Element("Id") == id).Remove();
            this.wordsDocument.Save("WordsData.xml");
        }

        public void EditWord(Word word)
        {
            XElement node = this.wordsDocument.Root.Elements("Word").Where(i => (int)i.Element("Id") == word.Id).FirstOrDefault();

            node.SetElementValue("Original", word.OriginalValue);
            node.SetElementValue("Translated", word.TranslationValue);

            this.wordsDocument.Save("WordsData.xml");
            
        }
    }
}