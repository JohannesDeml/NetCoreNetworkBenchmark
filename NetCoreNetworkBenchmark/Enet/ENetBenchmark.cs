﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCoreNetworkBenchmark.Enet
{
	internal class ENetBenchmark: INetworkBenchmark
	{
		private BenchmarkConfiguration config;
		private EchoServer echoServer;
		private List<EchoClient> echoClients;


		public void Initialize(BenchmarkConfiguration config)
		{
			this.config = config;
			ENet.Library.Initialize();
			echoServer = new EchoServer(config);
			echoClients = new List<EchoClient>();
		}

		public Task StartServer()
		{
			return echoServer.StartServerThread();
		}

		public Task StartClients()
		{
			for (int i = 0; i < config.NumClients; i++)
			{
				echoClients.Add(new EchoClient(i, config));
			}

			return Task.CompletedTask;
		}

		public Task ConnectClients()
		{
			for (int i = 0; i < config.NumClients; i++)
			{
				echoClients[i].Start();
			}

			var clientsConnected = Task.Run(() =>
			{
				for (int i = 0; i < config.NumClients; i++)
				{
					while (!echoClients[i].IsConnected)
					{
						Task.Delay(10);
					}
				}
			});
			return clientsConnected;
		}

		public void StartBenchmark()
		{
			for (int i = 0; i < echoClients.Count; i++)
			{
				echoClients[i].StartSendingMessages();
			}
		}

		public void StopBenchmark()
		{
		}

		public Task DisconnectClients()
		{
			for (int i = 0; i < echoClients.Count; i++)
			{
				echoClients[i].Disconnect();
			}

			return Task.CompletedTask;
		}

		public Task StopServer()
		{
			return echoServer.StopServerThread();
		}

		public Task StopClients()
		{
			return Task.CompletedTask;
		}

		public Task DisposeClients()
		{
			for (int i = 0; i < echoClients.Count; i++)
			{
				echoClients[i].Dispose();
			}

			var allDisposed = Task.Run(() =>
			{
				for (int i = 0; i < echoClients.Count; i++)
				{
					while (!echoClients[i].IsDisposed)
					{
						Task.Delay(10);
					}
				}
			});
			return allDisposed;
		}

		public Task DisposeServer()
		{
			echoServer.Dispose();

			return Task.CompletedTask;
		}
	}
}