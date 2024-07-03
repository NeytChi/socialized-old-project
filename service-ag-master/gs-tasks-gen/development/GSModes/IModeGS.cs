using database.context;
using Models.GettingSubscribes;

namespace ngettingsubscribers
{
    public interface IModeGS
    {
        bool HandleTask(Context context, ref TaskBranch branch); 
        bool CheckModeType(sbyte typeMode); 
        bool CheckOptions(Context context, ref TaskBranch branch);
    }
}