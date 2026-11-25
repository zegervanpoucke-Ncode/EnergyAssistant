using System.Threading;
using System.Threading.Tasks;

public interface IFluviusTokenProvider
{
	Task<string> GetAccessTokenAsync(
		CancellationToken cancellationToken = default);
}