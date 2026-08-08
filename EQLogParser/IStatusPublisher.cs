using System.Threading;
using System.Threading.Tasks;
using EQLogParser.Contracts;

namespace EQLogParser
{
    public interface IStatusPublisher
    {
        Task PublishAsync(ParserStatusUpdate status, CancellationToken cancellationToken);
    }
}
