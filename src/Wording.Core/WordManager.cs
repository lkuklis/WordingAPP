using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Wording.Core
{
    public class WordManager
    {
        public WordsDataSet WordDataSet;

        public WordManager()
        {
            WordDataSet = new WordsDataSet();
        }

        public IEnumerable<Word> GetWords()
        {
            return (from DataRow item in WordDataSet.Word.Rows
                    select new Word
                        {
                            Id = Int32.Parse(item["Id"].ToString()), OrginalValue = item["Original"].ToString(), TranslationValue = item["Translated"].ToString()
                        }).ToList();
        }

        public DataTable GetWordsData()
        {
            var words = WordDataSet.Word;
            words.ReadXml("WordsData.xml");
            return words;
        }

        public void AddWord(string original, string translated)
        {
            var words = WordDataSet.Word;
            var newWordRow = words.NewRow();
            newWordRow["Original"] = original;
            newWordRow["Translated"] = translated;
            words.Rows.Add(newWordRow);
        }

        public void Save()
        {
           this.WordDataSet.Word.WriteXml("WordsData.xml", XmlWriteMode.IgnoreSchema);
        }
    }
}