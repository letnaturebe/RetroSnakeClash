using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
	public class CConnector
	{
		private delegate void ConnectedHandler(CUserToken token);

		private ConnectedHandler? ConnectedCallback { get; set; }

		private Socket? _client;

		private readonly CNetworkService _networkService;

		public CConnector(CNetworkService networkService)
		{
			_networkService = networkService;
			ConnectedCallback = null;
		}

		public void Connect(IPEndPoint remoteEndpoint)
		{
			_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			SocketAsyncEventArgs eventArg = new SocketAsyncEventArgs();
			
			eventArg.Completed += OnConnectCompleted;
			eventArg.RemoteEndPoint = remoteEndpoint;
			bool pending = _client.ConnectAsync(eventArg);
			if (!pending)
			{
				OnConnectCompleted(null, eventArg);
			}
		}

		private void OnConnectCompleted(object? sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				CUserToken token = new CUserToken();

				_networkService.OnConnectCompleted(_client!, token);
				if (ConnectedCallback != null)
				{
					ConnectedCallback(token);
				}
			}
			else
			{
				Console.WriteLine($"Failed to connect. {e.SocketError}");
			}
		}
	}
}
