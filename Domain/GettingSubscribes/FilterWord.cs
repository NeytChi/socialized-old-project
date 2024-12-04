namespace Domain.GettingSubscribes
{
    public partial class FilterWord : BaseEntity
    {
        public long FilterId { get; set; }
        public string Value { get; set; }
        public bool Use { get; set; }
        public virtual TaskFilter Filter { get; set; }
    }
}