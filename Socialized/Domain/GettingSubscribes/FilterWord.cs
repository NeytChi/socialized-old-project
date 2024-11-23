namespace Domain.GettingSubscribes
{
    public partial class FilterWord
    {
        public FilterWord()
        {
            
        }
        public FilterWord(string wordValue, bool wordUse)
        {
            this.wordValue = wordValue;
            this.wordUse = wordUse;
        }
        public long wordId { get; set; }
        public long filterId { get; set; }
        public string wordValue { get; set; }
        public bool wordUse { get; set; }
        public virtual TaskFilter Filter { get; set; }
    }
}