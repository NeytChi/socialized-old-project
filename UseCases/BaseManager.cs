using Serilog;

namespace UseCases
{
    public class BaseManager
    {
        public ILogger Logger;

        public BaseManager(ILogger logger)
        {
            Logger = logger;
        }
    }
}
